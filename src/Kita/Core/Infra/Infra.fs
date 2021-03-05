namespace Kita.Core

open Kita.Providers
open Kita.Resources

[<AutoOpen>]
module Helper =
    let inline print (m: Managed<'a>) label item =
        printfn "%s| %s: %A"
        <| match Managed.getName m with
           | "" -> "anon"
           | x -> x
        <| label
        <| item

type Infra< ^Provider when ^Provider :> Provider and ^Provider: (new : unit -> ^Provider)>
    (
        name: string
    ) =
    inherit Named(name)

    member inline _.Bind
        (
            resource: ^R when ^R: (member Deploy : ^Provider -> unit),
            f
        ) =
        State
        <| fun (s: Managed< ^Provider >) ->

            print s "Resource" resource

            let (State m) = f resource
            Ops.deploy (resource, s.provider)

            s |> addResource resource |> m

    member inline _.Bind(State mA, f) =
        State
        <| fun (stateA: Managed< ^Provider >) ->

            let (x, stateA) = mA stateA
            print stateA "Combined bind" x
            print stateA "Combined stateA" stateA.provider

            let (State mB) = f x

            let stateAsB = convert stateA
            print stateA "Combined stateB" stateAsB.provider

            let (x, stateAsB') = mB stateAsB
            print stateA "Combined stateB ran" stateAsB'.provider

            let stateAsBAsFinal : Managed< ^Provider > = convert stateAsB'

            print stateA "Combined final" stateAsBAsFinal.provider

            x, stateAsBAsFinal

    member inline _.Bind(State m, f) =
        State
        <| fun s' ->

            let (x, s) = m s'
            print s' "Value" x

            let (State m) = f x

            m s

    member inline _.Bind(nested, f) =
        State
        <| fun s ->
            let s' = nested s

            print s "inner" <| Managed.getName s'

            let (State m) = f ()

            m s'

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

    member inline x.Run(State m) : Managed<'a> -> Managed< ^Provider > =
        fun s ->

            print s "run" ""

            s |> addName x.Name |> m |> snd

    member inline _.Combine(State mA, State mB) =
        State
        <| fun stateA ->

            let ((), stateA) = mA stateA
            let (y, stateB) = mB (convert stateA)

            y, (convert stateB)

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind = true)>]
    member inline _.Route
        (
            State m,
            [<ProjectionParameter>] pathWith,
            [<ProjectionParameter>] handlersWith
        ) =
        State
        <| fun s ->

            let (ctx, s) = m s
            let path = pathWith ctx
            let handlers = handlersWith ctx

            print s "Route" path

            handlers
            |> List.iter
                (fun handler ->
                    print s "Handler" (handler.GetType().FullName))

            ctx,
            s
            |> addRoutes (List.map (fun x -> path, x) handlers)

    [<CustomOperation("proc", MaintainsVariableSpaceUsingBind = true)>]
    member inline _.Proc
        // NOTE in this form, it's basically some sugar around a do!
        // with constraints on the constructor argument type
        (
            State m,
            [<ProjectionParameter>] getCreator,
            [<ProjectionParameter>] getResourceDef: _ -> Async<unit>
        ) =
        State
        <| fun s ->
            print s "Task" ""

            let (ctx, s) = m s

            let resourceDef = getResourceDef ctx
            let creator = getCreator ctx

            let resource = creator resourceDef


            ctx, s |> addResource resource

and Named(name: string) =
    // Gets around issues with inline accessing private data and SRTP:
    // error FS0670: This code is not sufficiently generic. The type variable  ^Provider when  ^Provider :> Config and  ^Provider : (new : unit ->  ^Provider) could not be generalized because it would escape its scope
    // error FS1113: The value 'Run' was marked inline but its implementation makes use of an internal or private function which is not sufficiently accessible
    member val Name = name

module Infra =
    let inline infra'< ^Provider
                            when ^Provider :> Provider
                            and ^Provider: (new : unit -> ^Provider)>
        name
        =
        Infra< ^Provider>(name)

    let gated cond block = if cond then block else id
