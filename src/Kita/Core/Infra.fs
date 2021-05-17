namespace rec Kita.Core

open Kita.Core.Http

type Provider =
        // Needs to be idempotent if many-to-one Block / Provider relationship
    abstract member Launch : unit -> unit
    abstract member Run : unit -> unit

type AttachedBlock =
    { name : string
      state : Managed
      launch : unit -> unit
      run : unit -> unit
      nested : Map<string, AttachedBlock> }

type Block<'T when 'T :> Provider> =
    abstract member Name : string
    abstract member Attach : Managed -> AttachedBlock

type RootBlockAttribute(name: string) =
    inherit System.Attribute()
    new () = RootBlockAttribute("")

module AttachedBlock =
    let getNestedByPath (root: AttachedBlock) pathsAll =
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
    { resources : CloudResource list
      handlers: MethodHandler list
      nested : Map<string, Managed>
      path : string list }
      static member Empty =
        { resources = []
          handlers = []
          nested = Map.empty
          path = List.empty }

type State<'result> =
    State of (Managed -> 'result * Managed)

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

    let noop = fun () -> ()

type NoBlock<'T when 'T :> Provider>() =
    interface Block<'T> with
        member _.Attach (_) =
            { launch = Helper.noop
              run = Helper.noop
              name = "No name"
              state = Managed.Empty
              nested = Map.empty
              }

        member _.Name = "No block"

    static member Instance = NoBlock() :> Block<'T>

type PublicInfra<'P>(name: string, provider: 'P) =
    // Gets around issues with inline accessing private data and SRTP:
    // error FS0670: This code is not sufficiently generic. The type variable  ^Provider when  ^Provider :> Config and  ^Provider : (new : unit ->  ^Provider) could not be generalized because it would escape its scope
    // error FS1113: The value 'Run' was marked inline but its implementation makes use of an internal or private function which is not sufficiently accessible
    member _.Name = name
    member _.Provider = provider

type Infra< ^Provider when ^Provider :> Provider>
    (
        name: string,
        provider: ^Provider
    ) =
    inherit PublicInfra<'Provider>(name, provider)

    member inline this.Run(State runner) =
        { new Block< ^Provider> with
            member _.Name = this.Name
            member block.Attach (initState) =
                print initState "run" ""

                // if initstate is [] then this is the root
                let path = initState.path @ [block.Name]

                let ranState =
                    { initState with path = path }
                        // Add name to state path
                    |> runner provider
                    |> snd
                
                let nestedAttached =
                    ranState.nested
                    |> Map.map (fun k v ->
                        printfn "Attaching child: %s" k

                        { Managed.Empty with path = path }
                        |> v.Attach

                        )

                {
                    name = block.Name
                    state = ranState
                    nested = nestedAttached
                    launch = fun name location ->
                        printfn "Nested providers: %i"
                            ranState.nested.Count

                        printfn "FIXME does the nested block actually work?"

                        nestedAttached
                        |> Map.iter (fun _k v ->
                            v.launch name location
                            )

                        this.Provider.Launch(name, location, ranState.path)
                }
        }

    member inline this.Bind
        (
            resource: ^R when ^R: (member Attach : ^Provider -> unit),
            f
        ) =
        State
        <| fun (s: Managed) ->

            print s "Resource" resource

            let (State m) = f resource

            Ops.attach (resource, this.Provider)

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

            (), Managed.Empty

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
                            // This is cast of Block<#Provider> to Block<Provider>
                            member _.Name = innerBlock.Name
                            member b.Attach (x) = innerBlock.Attach(x)
                        }

                    match s.nested.TryGetValue innerBlock.Name with
                    | true, _block -> // ??
                        printfn "Nested block name collision: %s" innerBlock.Name
                            // TODO what to do in this case?
                        s.nested.Add (innerBlock.Name, nestedBlock) 
                    | false, _ ->
                        s.nested.Add (innerBlock.Name, nestedBlock) 

                ctx, { s with nested = nextNested }

module Infra =
    let inline infra< ^Provider when ^Provider :> Provider>
        provider
        name
        =
        Infra< ^Provider>(name, provider)

    let inline gated cond (block: Block<'T>) =
        if cond then block else NoBlock.Instance

    let launch
        (appName: string)
        (location: string)
        (block: AttachedBlock)
        =
        block.launch appName location

    let launchNested
        (appName: string)
        (location: string)
        nestedPath
        (rootBlock: AttachedBlock)
        =
        nestedPath
        |> AttachedBlock.getNestedByPath rootBlock
        |> launch appName location
