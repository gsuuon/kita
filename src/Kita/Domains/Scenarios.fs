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

    type RoutesBlockState =
        { routeState : RouteState }
        static member Empty =
            { routeState = RouteState.Empty }

    let routes =
        RoutesBlock<RoutesBlockState>
            { new UserDomain<_,_> with 
                member _.get s = s.routeState
                member _.set s rs = { s with routeState = rs }
            }

    let inline routesBlock< ^a when 'a :> Provider> name =
        Block<'a, RoutesBlockState> name

    let noop = fun () -> ()
    let ok = Http.Helpers.returnOk

    let blockA =
        routesBlock "A" {
            do! routes {
                route "delete" "hello" ok
                get "hey" ok
            }

            return ()
        }

    let launched = blockA |> Operation.attach (AProvider())

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
                route "get" "hey" ok
            }

            do! procs {
                proc noop
            }

            return ()
        }
