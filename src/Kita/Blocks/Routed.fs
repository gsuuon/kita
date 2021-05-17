namespace Kita.Blocks.Routed

open Kita.Core

type RouteHandler = unit -> unit
type RouteState = { routes : Map<string, RouteHandler> }

module RoutedBlockBuilder =
    let addRoute route handler (routeState: RouteState) =
        { routeState with
            routes = Map.add route handler routeState.routes }

type UserDomain<'U, 'D> =
    abstract get : 'U -> 'D
    abstract set : 'U -> 'D -> 'U

module UserDomain =
    let inline update<'P, 'U, 'D when 'P :> Provider>
        (userDomain: UserDomain<'U, _>)
        (updater: 'D -> 'D)
        (state: BlockBindState<'P, 'U>)
        =
        let domain = userDomain.get state.user

        let updated = updater domain
        let resultUser = userDomain.set state.user updated

        { state with user = resultUser }

type DomainBuilder<'P, 'U, 'D when 'P :> Provider>() =
    member _.Return x = x
    member _.Bind (m, f) = f m
    member _.Delay f = f
    member _.Run f = f()

type RoutedBlockBuilder<'P, 'U when 'P :> Provider>(userDomain)
    =
    inherit DomainBuilder<'P, 'U, RouteState>()

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind=true)>]
    member _.Route
        (
            ctx,
                [<ProjectionParameter>]
            getRoute,
                [<ProjectionParameter>]
            getHandler
        ) =
        fun s ->
            let route = getRoute ctx
            let handler = getHandler ctx

            s
            |> UserDomain.update<'P, 'U, RouteState>
                userDomain
                (RoutedBlockBuilder.addRoute route handler)
    
module ScenarioCommon = 
    type AProvider() =
        interface Provider with
            member _.Launch () = ()
            member _.Run () = ()

    type AResource() =
        interface CloudResource

    type AResourceBuilder() =
        interface ResourceBuilder<AProvider, AResource> with
            member _.Build (_p) = 
                AResource()

module JustRoutesScenario =
    open ScenarioCommon

    type RoutedBlockState = {
        routeState : RouteState
    }

    let routes<'P when 'P :> Provider> =
        RoutedBlockBuilder<'P, RoutedBlockState>
            { new UserDomain<_,_> with 
                member _.get s = s.routeState
                member _.set s rs = { s with routeState = rs }
            }

    let routedBlock provider name =
        BlockBuilder<_, RoutedBlockState>(name, provider)

    let blockA (provider: AProvider) =
        routedBlock provider "A" {
            let! x = AResourceBuilder()

            do! routes {
                route "hey" (fun () -> ())
            }

            return ()
        }

type ProcState = { procs : List<unit -> unit> }

module ProcState =
    let addProc proc (procState: ProcState) =
        { procState with procs = proc :: procState.procs }

type ProcBlockBuilder<'P, 'U when 'P :> Provider>(userDomain)
    =
    inherit DomainBuilder<'P, 'U, ProcState>()

    [<CustomOperation("proc", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Proc
        (
            ctx,
                [<ProjectionParameter>]
            getProc
        ) =
        fun s ->
            let proc = getProc ctx

            s
            |> UserDomain.update<'P, 'U, ProcState>
                userDomain
                (ProcState.addProc proc)

module RoutesAndProcScenario =
    open ScenarioCommon
    open JustRoutesScenario

    type RouteAndProcBlockState = {
        routeState : RouteState
        procState : ProcState
    }

    let routeAndProcBlock provider name =
        BlockBuilder<_, RouteAndProcBlockState>(name, provider)

    let routes =
        RoutedBlockBuilder<_, RouteAndProcBlockState>
            { new UserDomain<_, _> with 
                member _.get s = s.routeState
                member _.set s rs = { s with routeState = rs }
            }

    let procs = 
        ProcBlockBuilder<_, RouteAndProcBlockState>
            { new UserDomain<_, _> with 
                member _.get s = s.procState
                member _.set s d = { s with procState = d }
            }

    let blockA (provider: AProvider) =
        routeAndProcBlock provider "A" {
            let! x = AResourceBuilder()

            do! routes {
                route "hey" (fun () -> ())
            }

            do! procs {
                proc (fun () -> ())
            }

            return ()
        }
