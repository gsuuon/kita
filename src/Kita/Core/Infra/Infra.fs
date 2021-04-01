namespace Kita.Core

open Kita.Providers
open Kita.Resources
open Kita.Core.Http

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

            ctx,
            s |> addRoutes (List.map (fun x -> path, x) pathedHandlers)

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

            let joinNested = getNested ctx

            ctx, 
            s
            |> convert
                // inner state may be a different Provider, ie different specialization of Managed
            |> joinNested
            |> combine
                // Convert back to original provider type
                { provider = s.provider
                  handlers = []
                  resources = []
                  names = []
                  }


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
