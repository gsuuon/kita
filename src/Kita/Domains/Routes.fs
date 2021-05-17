namespace Kita.Domains.Routes

open Kita.Core
open Kita.Domains

type RouteHandler = unit -> unit
type RouteState = { routes : Map<string, RouteHandler> }

module RouteState =
    let addRoute route handler (routeState: RouteState) =
        { routeState with
            routes = Map.add route handler routeState.routes }
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
                (RouteState.addRoute route handler)
    
