namespace Kita.Core

open Kita.Core.Providers
open Kita.Core.Resources

type Infra (name: string, config: #Config) =
    member inline _.Bind (resource: #CloudResource, f)
        // I may be running into this bug:
        // https://github.com/dotnet/fsharp/issues/9449
        // Regression: inline data for method (related to witness passing PR) #9449
        =
        State <| fun s ->

        let (State runner) = f resource

        Ops.deploy (resource, config)
        printfn "Bind resource: %A" resource

        runner s

    member inline _.Bind (State m, f) =
        State <| fun s ->

        let (x, s) = m s
        let (State m) = f x

        printfn "Bind value: %A" x

        m s

    member inline _.Bind (nested: Managed -> Managed, f) =
        State <| fun s ->
            let s = nested s
            let (State m) = f ()

            match List.tryLast s.names with
            | Some innerName ->
                printfn "Bind inner infra: %s" innerName
            | None ->
                printfn "Bind inner anonymous"

            m s

    member inline _.Combine(stateA, stateB) =
      { resources = stateA.resources @ stateB.resources
        handlers = stateA.handlers @ stateB.handlers
        names = stateA.names @ stateB.names }
          
    member inline _.Zero () =
        State <| fun _ ->

        (), Managed.Empty

    member inline _.Return x = ret x
    member inline _.Yield x = ret x
    member inline _.Delay f =
        State <| fun s ->

        let (State m) = f()
        m s

    member inline _.Run (State m) =
        fun s ->

        let (_x, s) = m s
        s
        (* |> addName name *)

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Route (State runner,
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
    member inline _.Proc (State runner,
        [<ProjectionParameter>]task: _ -> Async<unit>)
        =
        State <| fun s ->

        let (ctx, s) = runner s
        let task = task ctx
        let cloudTask = CloudTask task

        printfn "Cloud task: %A" task

        ctx, s |> addResource cloudTask

module Infra =
    let inline infra (config: #Config) name =
        Infra(name, config)
