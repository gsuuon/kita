namespace Kita.Core

open Kita.Core.Http
open Kita.Core.Providers
open Kita.Core.Resources

type State =
  { resources : CloudResource list
    handlers : (string * MethodHandler) list
    names : string list }
    static member Empty =
      { resources = []
        handlers = []
        names = []}

type State<'a> = | State of (State -> 'a * State)
type Resource<'T when 'T :> CloudResource> = | Resource of 'T

[<AutoOpen>]
module State =
    let addResource (resource: #CloudResource) state =
        { state with
            resources = resource :> CloudResource :: state.resources }
    let getResources = State (fun s -> s.resources, s)

    let addRoutes pathHandlers state =
        { state with handlers = state.handlers @ pathHandlers }

    let addName name state =
        { state with names = name :: state.names }

    let ret x = State (fun s -> x, s)

type Infra (name: string, config: Config) =
    new (config: Config) = Infra("anon", config)

    member _.Bind (resource: #CloudResource, f)
        =
        State <| fun s ->

        let (State runner) = f resource
        resource.BeginActivation config
        
        printfn "Bind resource: %A" resource

        runner s

    member _.Bind (State m, f) =
        State <| fun s ->

        let (x, s) = m s
        let (State m) = f x

        printfn "Bind value: %A" x

        m s

    member _.Bind (nested: State -> State, f) =
        State <| fun s ->
            let s = nested s
            let (State m) = f ()
            printfn "Bind inner infra: %s" <| nameof nested
            m s

    member _.Combine(stateA, stateB) =
      { resources = stateA.resources @ stateB.resources
        handlers = stateA.handlers @ stateB.handlers
        names = stateA.names @ stateB.names }
          
    member _.Zero () =
        State <| fun _ ->

        (), State.Empty

    member _.Return x = ret x
    member _.Yield x = ret x
    member _.Delay f =
        State <| fun s ->

        let (State m) = f()
        m s

    member _.Run (State m) =
        fun s ->

        let (_x, s) = m s
        s
        |> addName name

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind=true)>]
    member _.Route (State runner,
        [<ProjectionParameter>]pathWith,
        [<ProjectionParameter>]handlersWith)
        =
        State <| fun s ->

        let (ctx, s) = runner s
        let path = pathWith ctx
        let handlers = handlersWith ctx
    
        printfn "Routing: %s" path

        ctx
        , s |> addRoutes
                (List.map (fun x -> path, x) handlers)

    [<CustomOperation("proc", MaintainsVariableSpaceUsingBind=true)>]
    member _.Proc (State runner,
        [<ProjectionParameter>]task: _ -> Async<unit>)
        =
        State <| fun s ->

        let (ctx, s) = runner s
        let task = task ctx
        let cloudTask = CloudTask task

        printfn "Cloud task: %A" task

        ctx, s |> addResource cloudTask
