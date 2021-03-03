namespace Kita.Core

open Kita.Core.Providers
open Kita.Core.Resources

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
                and ^Config : (new : unit -> ^Config)>
    (name: string)
    =
    inherit Named(name)

    member inline _.Bind (resource: ^R when ^R : (member Deploy : ^Config -> unit), f)
        =
        State <| fun (s: Managed< ^Config>) ->

        print s "Resource" resource

        let (State runner) = f resource
        Ops.deploy (resource, s.config)

        s
        |> addResource resource
        |> runner

    member inline _.Bind (State runnerA, f) =
        State <| fun (stateA: Managed< ^Config>) ->

        let (x, stateA) = runnerA stateA
        print stateA "Combined bind" x
        print stateA "Combined stateA" stateA.config

        let (State runnerB) = f x

        let stateAsB = convert stateA
        print stateA "Combined stateB" stateAsB.config

        let (x, stateAsB') = runnerB stateAsB
        print stateA "Combined stateB ran" stateAsB'.config

        let stateAsBAsFinal : Managed< ^Config> =
            convert stateAsB'

        print stateA "Combined final" stateAsBAsFinal.config

        x, stateAsBAsFinal

    member inline _.Bind (State m, f) =
        State <| fun s' ->

        let (x, s) = m s'
        print s' "Value" x

        let (State m) = f x

        m s

    member inline _.Bind (nested, f) = State <| fun s ->
        let s' = nested s

        print s "inner" <| Managed.getName s'

        let (State m) = f()

        m s'
          
    member inline _.Zero () = State <| fun s ->
        print s "zero" ""

        (), Managed.empty< ^Config>()

    member inline _.Return x = ret x
    member inline _.Yield x = ret x
    member inline _.Delay f = State <| fun s ->
        let (State runner) = f()

        s |> runner

    member inline x.Run (State runner)
        : Managed<'a> -> Managed< ^Config>
        =
        fun s ->

        print s "run" ""

        s
        |> addName x.Name
        |> runner
        |> snd

    member inline _.Combine (State runnerA, State runnerB) =
        State <| fun stateA ->
            let ((), stateA) = runnerA stateA
            let (y, stateB) = runnerB (convert stateA)
            y, (convert stateB)

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
and Named(name: string) =
    // Gets around issues with inline accessing private data and SRTP:
    // error FS0670: This code is not sufficiently generic. The type variable  ^Config when  ^Config :> Config and  ^Config : (new : unit ->  ^Config) could not be generalized because it would escape its scope
    // error FS1113: The value 'Run' was marked inline but its implementation makes use of an internal or private function which is not sufficiently accessible
    member val Name = name

module Infra =
    let inline infra'< ^Config
        when ^Config :> Config
        and ^Config : (new : unit -> ^Config)> name = Infra< ^Config>(name)


    let gated cond block =
        if cond then block else id
