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

    type RoutedBlockState = {
        routeState : RouteState
    }

    let routes =
        RoutedBlock<RoutedBlockState>
            { new UserDomain<_,_> with 
                member _.get s = s.routeState
                member _.set s rs = { s with routeState = rs }
            }

    let routedBlock name =
        Block<_, RoutedBlockState> name

    let blockA =
        routedBlock "A" {
            let! _x = AResourceBuilder()

            do! routes {
                route "hey" (fun () -> ())
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
        RoutedBlock<RouteAndProcBlockState>
            { new UserDomain<_, _> with 
                member _.get s = s.routeState
                member _.set s rs = { s with routeState = rs }
            }

    let procs = 
        ProcBlock<RouteAndProcBlockState>
            { new UserDomain<_, _> with 
                member _.get s = s.procState
                member _.set s d = { s with procState = d }
            }

    let blockA =
        routeAndProcBlock "A" {
            let! _x = AResourceBuilder()

            do! routes {
                route "hey" (fun () -> ())
            }

            do! procs {
                proc (fun () -> ())
            }

            return ()
        }
