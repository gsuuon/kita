namespace Kita.Core

open Kita.Core.Providers
open Kita.Core.Resources

type Named(name: string) =
    // Gets around issues with inline + SRTP:
    // error FS0670: This code is not sufficiently generic. The type variable  ^Config when  ^Config :> Config and  ^Config : (new : unit ->  ^Config) could not be generalized because it would escape its scope
    // error FS1113: The value 'Run' was marked inline but its implementation makes use of an internal or private function which is not sufficiently accessible
    member val Name = name

[<AutoOpen>]
module Helper =
    let inline print (m: Managed<'a>) label item =
        printfn "%s| %s: %A"
        <| match Managed.getName m with
           | "" -> "anon"
           | x -> x
        <| label
        <| item

type Infra< ^Config
    when ^Config :> Config
    and ^Config : (new : unit -> ^Config)
    >(name: string) =
    inherit Named(name)

    member inline _.Bind (resource: #CloudResource, f)
        =
        State <| fun (s: Managed< ^Config>) ->

        print s "Resource" resource
        let (State runner) = f resource
        Ops.deploy (resource, s.config)

        runner s

    member inline _.Bind (State m, f) =
        State <| fun s' ->

        let (x, s) = m s'
        print s' "Value" x

        let (State m) = f x

        m s

    member inline _.Bind (nested, f) =
        State <| fun s' ->

        let s = nested s'

        print s' "inner" <| Managed.getName s

        let (State m) = f ()

        m s
          
    member inline _.Zero () =
        State <| fun s ->

        print s "zero" ""

        (), Managed.empty< ^Config>()

    member inline _.Return x = ret x
    member inline _.Yield x = ret x
    member inline _.Delay f =
        State <| fun s ->

        let (State m) = f()
        m s

    member inline x.Run (State runner) =
        fun s ->

        print s "run" ""

        s
        |> addName x.Name
        |> runner
        |> snd

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Route (State runner,
        [<ProjectionParameter>]pathWith,
        [<ProjectionParameter>]handlersWith)
        =
        State <| fun s ->

        let (ctx, s) = runner s
        let path = pathWith ctx
        let handlers = handlersWith ctx
    
        print s "Route" path

        ctx
        , s |> addRoutes
                (List.map (fun x -> path, x) handlers)

    [<CustomOperation("proc", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Proc (State runner,
        [<ProjectionParameter>]task: _ -> Async<unit>)
        =
        State <| fun s ->
        print s "Task" ""

        let (ctx, s) = runner s
        let task = task ctx
        let cloudTask = CloudTask task


        ctx, s |> addResource cloudTask
