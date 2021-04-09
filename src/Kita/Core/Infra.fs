namespace rec Kita.Core

open Kita.Core.Http

type Provider =
    abstract member Launch : string * string * AttachedBlock -> unit
    
type AttachedBlock =
    { name : string
      state : Managed
      nested : Map<string, AttachedBlock> }

type Block<'T when 'T :> Provider> =
    abstract member Name : string
    abstract member Attach : Managed -> AttachedBlock
    abstract member Attach : Managed * 'T -> AttachedBlock

module AttachedBlock =
    let getNestedByPath pathsAll (root: AttachedBlock) =
        let rec getNested pathsLeft (current: AttachedBlock) =
            match pathsLeft with
            | [] -> current
            | head::rest ->
                match current.nested.TryGetValue head with
                | true, attachedBlock ->
                    getNested rest attachedBlock
                | false, _ ->
                    let pathsTraveled =
                        pathsAll
                        |> List.take (pathsAll.Length - pathsLeft.Length)

                    failwithf
                        "No block found\nLost at: %s\nLooking for: %s"
                            (pathsTraveled |> String.concat ".")
                            (pathsAll |> String.concat ".")

        getNested pathsAll root

type Managed =
    { resources: CloudResource list
      handlers: MethodHandler list
      nested : Map<string, Block<Provider>>
      path : string list }

type State<'a, 'b, 'c> = State of (Managed -> 'c * Managed)

module Managed =
    let empty () =
        { resources = []
          handlers = []
          nested = Map.empty
          path = List.empty }

type Resource<'T when 'T :> CloudResource> = Resource of 'T

module Ops =
    let inline attach< ^C, ^R when ^R: (member Attach : ^C -> unit) and ^C :> Provider>
        (
            resource: ^R,
            config: ^C
        )
        =
        (^R: (member Attach : ^C -> unit) (resource, config))

type CloudResource = interface end

[<AutoOpen>]
module State =
    let addResource (resource: #CloudResource) state =
        { state with
              resources = resource :> CloudResource :: state.resources }

    let getResources = State(fun s -> s.resources, s)

    let addRoutes pathHandlers state =
        { state with
              handlers = state.handlers @ pathHandlers }

    let ret x = State(fun s -> x, s)

[<AutoOpen>]
module Helper =
    let inline print (m: Managed) label item =
        printfn "%s| %s: %A"
        <| (m.path |> String.concat ".")
        <| label
        <| item

type NoBlock<'T when 'T :> Provider>() =
    interface Block<'T> with
        member _.Attach (_, _) =
            { launch = fun (_, _) -> ()
              name = "No name"
              state = Managed.empty()
              nested = Map.empty }

        member b.Attach (x: Managed) =
            (b :> Block<'T>).Attach(x, Unchecked.defaultof<_>())

        member _.Name = "No block"

    static member Instance = NoBlock() :> Block<'T>

type Infra< ^Provider when ^Provider :> Provider>
    (
        name: string
    ) =
    inherit Named(name)

    member inline x.Run(State runner) =
        { new Block< ^Provider> with
            member _.Name = x.Name
            member block.Attach (initState) =
                block.Attach(initState, System.Activator.CreateInstance< ^Provider>())
                    // FIXME will runtime crash if ^Provider is interface type or no null constructor
                    // all this really saves me is the `when ^Provider : new` constraint
                    // but srtp is annoying and i need to copy/paste that constraint in any
                    // other type where i want to use ^Provider TODO add the new constraint
                
            member block.Attach (initState: Managed, provider: ^Provider) =
                print initState "run" ""

                let path = initState.path @ [block.Name]

                let ranState =
                    { initState with path = path }
                        // Add name to state path
                    |> runner
                    |> snd
                
                let nestedAttached =
                    ranState.nested
                    |> Map.map (fun k v ->
                        printfn "Attaching child: %s" k

                        { Managed.empty() with path = path }
                        |> v.Attach

                        )

                let attachedBlock =
                    {
                        name = block.Name
                        state = ranState
                        nested = nestedAttached
                    }

                // TODO I don't actually need launch here
                // it just uses ranState to do stuff
                let launch (provider: #Provider) =
                    fun (name, location) ->
                        printfn "Nested providers: %i"
                            ranState.nested.Count

                        printfn "FIXME does the nested block actually work?"

                        nestedAttached
                        |> Map.iter (fun _k v ->
                            v.launch(name, location)
                            )

                        provider.Launch(name, location, _temp)

                attachedBlock
        }

    member inline _.Bind
        (
            resource: ^R when ^R: (member Attach : ^Provider -> unit),
            f
        ) =
        State
        <| fun (s: Managed) ->

            print s "Resource" resource

            let (State m) = f resource

            // TODO Call attach
            printfn "TODO Actually attach resource to provider"

            (* Ops.attach (resource, s.provider) *)
            (* let doAttach = fun (provider: ^Provider) -> Ops.attach (resource, provider) *)

            s |> addResource resource |> m

    member inline _.Bind(State m, f) =
        State
        <| fun s' ->

            let (x, s) = m s'
            print s' "Value" x

            let (State m) = f x

            m s

    member inline _.Zero() =
        State
        <| fun s ->
            print s "zero" ""

            (), Managed.empty ()

    member inline _.Return x = ret x
    member inline _.Yield x = ret x

    member inline _.Delay f =
        State
        <| fun state ->

            let (State runner) = f ()

            state |> runner

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind = true)>]
    member inline _.Route
        (
            State m,
            [<ProjectionParameter>] getPath,
            [<ProjectionParameter>] getHandlers
        ) =
        State
        <| fun s ->

            let (ctx, s) = m s
            let path = getPath ctx
            let handlers = getHandlers ctx

            print s "Route" path

            let pathedHandlers = handlers |> List.map (fun h -> h path)

            pathedHandlers
            |> List.iter (fun mh -> print s "Handler" mh.handler)

            ctx, s |> addRoutes pathedHandlers

    [<CustomOperation("proc", MaintainsVariableSpaceUsingBind = true)>]
    member inline _.Proc
        // NOTE in this form, it's basically some sugar around a do!
        // with ability to put constraints on the creator argument type
        // trying to use do! was kind of busted because overload resolution
        // doesn't differentiate null type, so I'd have to create
        // a wrapper type for null-like resources and wrap everything
        // custom operation makes this easier
        (
            State m,
            [<ProjectionParameter>] getCreator,
            [<ProjectionParameter>] getResourceDef
        ) =
        State
        <| fun s ->
            print s "Task" ""

            let (ctx, s) = m s

            let resourceDef = getResourceDef ctx
            let creator = getCreator ctx

            let resource = creator resourceDef

            ctx, s |> addResource resource

    [<CustomOperation("nest", MaintainsVariableSpaceUsingBind = true)>]
    member inline _.Nest
        (
            State m,
            [<ProjectionParameter>] getNested
        ) =
        State
        <| fun state ->
            let (ctx, s) = m state

            let innerBlock : Block<'T> = getNested ctx

            if innerBlock :? NoBlock<'T> then
                printfn "Not adding child, noop"
                ctx, s
            else
                printfn "Adding child: %s" innerBlock.Name

                let nextNested =
                    let nestedBlock =
                        { new Block<Provider> with
                            member _.Name = innerBlock.Name
                            member _.Attach (x, _) =
                                // NOTE nested blocks always create a new instance of the provider
                                innerBlock.Attach (x,System.Activator.CreateInstance<_>())
                            member b.Attach (x) =
                                b.Attach(x, Unchecked.defaultof<_>)
                        }

                    match s.nested.TryGetValue innerBlock.Name with
                    | true, _block -> // ??
                        printfn "Nested block name collision: %s" innerBlock.Name
                            // TODO what to do in this case?
                        s.nested.Add (innerBlock.Name, nestedBlock) 
                    | false, _ ->
                        s.nested.Add (innerBlock.Name, nestedBlock) 

                ctx, { s with nested = nextNested }

and Named(name: string) =
    // Gets around issues with inline accessing private data and SRTP:
    // error FS0670: This code is not sufficiently generic. The type variable  ^Provider when  ^Provider :> Config and  ^Provider : (new : unit ->  ^Provider) could not be generalized because it would escape its scope
    // error FS1113: The value 'Run' was marked inline but its implementation makes use of an internal or private function which is not sufficiently accessible
    member val Name = name


module Infra =
    let inline infra'< ^Provider when ^Provider :> Provider>
        name
        =
        Infra< ^Provider>(name)

    let inline gated cond (block: Block<'T>) =
        if cond then block else NoBlock.Instance
