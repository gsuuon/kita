namespace rec Kita.Core

open Kita.Core.Http

type Provider =
    abstract member Name : string
    abstract member Launch : string * string -> unit

module Ops =
    let inline attach< ^C, ^R when ^R: (member Attach : ^C -> unit) and ^C :> Provider>
        (
            resource: ^R,
            config: ^C
        )
        =
        (^R: (member Attach : ^C -> unit) (resource, config))

type CloudResource = class end

type Managed<'Provider> =
    { resources: CloudResource list
      handlers: MethodHandler list
      name: string
      provider: 'Provider
      nested : Map<string, unit -> Provider> }

module Managed =
    let inline empty<'Provider when 'Provider :> Provider> ()
        =
        { resources = []
          handlers = []
          name = ""
          provider = System.Activator.CreateInstance<'Provider>()
          nested = Map.empty }

type State<'a, 'b, 'c> = State of (Managed<'b> -> 'a * Managed<'c>)
type Resource<'T when 'T :> CloudResource> = Resource of 'T

[<AutoOpen>]
module State =
    let addResource (resource: #CloudResource) state =
        { state with
              resources = resource :> CloudResource :: state.resources }

    let getResources = State(fun s -> s.resources, s)

    let addRoutes pathHandlers state =
        { state with
              handlers = state.handlers @ pathHandlers }

    let setName name (managed: Managed<_>) =
        { managed with name = name }

    let ret x = State(fun s -> x, s)

[<AutoOpen>]
module Helper =
    let inline print (m: Managed<'a>) label item =
        printfn "%s| %s: %A"
        <| match m.name with
           | "" -> "anon"
           | x -> x
        <| label
        <| item

type IBlock<'P> =
    abstract member Name : string
    abstract member Attach : Managed<'P> -> Provider

type Infra< ^Provider when ^Provider :> Provider>
    (
        name: string
    ) =
    inherit Named(name)

    member inline _.Bind
        (
            resource: ^R when ^R: (member Attach : ^Provider -> unit),
            f
        ) =
        State
        <| fun (s: Managed< ^Provider >) ->

            print s "Resource" resource

            let (State m) = f resource
            Ops.attach (resource, s.provider)

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

            (), Managed.empty< ^Provider> ()

    member inline _.Return x = ret x
    member inline _.Yield x = ret x

    member inline _.Delay f =
        State
        <| fun s ->

            let (State m) = f ()

            s |> m

    member inline x.Run(State runner) =
        { new IBlock< ^Provider> with
            member _.Name = x.Name
            member this.Attach initState =
                print initState "run" ""

                let ranState =
                    initState
                    |> setName x.Name
                    |> runner
                    |> snd

                let nestedProviders =
                    ranState.nested
                    |> Map.map (fun k v ->
                        let provider = v()
                        printfn "Got child provider: %s" provider.Name
                        provider
                        )

                { new Provider with
                    member _.Name = x.Name
                    member _.Launch (a, b) =
                        let provider : 'Provider = ranState.provider

                        printfn "Nested providers: %i" nestedProviders.Count

                        nestedProviders
                        |> Map.iter (fun k v ->
                            printfn "Launching child: %s" v.Name
                            v.Launch(a, b)
                            )

                        provider.Launch (a,b)
                }
        }

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
        // TODO revisit this, Managed<'T> should just live in a collection and not all be jammed into one record
        State
        <| fun state ->
            let (ctx, s) = m state

            let childApp : IBlock< 'T> = getNested ctx

            printfn "Adding child: %s" childApp.Name

            ctx,
            { s with
                nested =
                    s.nested.Add
                        ( childApp.Name
                        , fun () ->
                            Managed.empty<'T>()
                            |> childApp.Attach
                            ) }

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

    let gated cond block = if cond then block else id
