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
