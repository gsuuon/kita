namespace Kita.Blocks.Routed

open Kita.Core
open Kita.Domains
open Kita.Domains.Routes
open Kita.Domains.Procs

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

    type RoutesBlockState = {
        routeState : RouteState
    }

    let routes =
        RoutesBlock<RoutesBlockState>
            { new UserDomain<_,_> with 
                member _.get s = s.routeState
                member _.set s rs = { s with routeState = rs }
            }

    let routesBlock name =
        Block<_, RoutesBlockState> name


    let noop = fun () -> ()
    let blockA =
        routesBlock "A" {
            do! routes {
                route "delete" "hello" noop
                post "hi" noop
                post "hey" noop
            }

            return ()
        }

module RoutesAndProcScenario =
    open ScenarioCommon
    open JustRoutesScenario

    type RouteAndProcBlockState = {
        routeState : RouteState
        procState : ProcState
    }

    let routeAndProcBlock name =
        Block<_, RouteAndProcBlockState>(name)

    let routes =
        RoutesBlock<RouteAndProcBlockState>
            { new UserDomain<_, _> with 
                member _.get s = s.routeState
                member _.set s rs = { s with routeState = rs }
            }

    let procs = 
        ProcsBlock<RouteAndProcBlockState>
            { new UserDomain<_, _> with 
                member _.get s = s.procState
                member _.set s d = { s with procState = d }
            }

    let blockA =
        routeAndProcBlock "A" {
            do! routes {
                route "get" "hey" noop
            }

            do! procs {
                proc noop
            }

            return ()
        }
