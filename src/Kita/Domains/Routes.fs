namespace Kita.Domains.Routes

open Kita.Core
open Kita.Domains

type RouteHandler = unit -> unit
type RouteState = { routes : Map<string, RouteHandler> }

module RouteState =
    let addRoute route handler (routeState: RouteState) =
        { routeState with
            routes = Map.add route handler routeState.routes }
type RoutedBlock<'U>(userDomain)
    =
    inherit DomainBuilder<'U, RouteState>(userDomain)

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind=true)>]
    member this.Route
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
                this.UserDomain
                (RouteState.addRoute route handler)
    
