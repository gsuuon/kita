namespace Kita.Domains.Routes

open Kita.Core
open Kita.Domains

type RouteHandler = unit -> unit
type RouteState =
    { routes : Map<string * string, RouteHandler> }
    static member Empty =
        { routes = Map.empty }
    // key is method * route

module private Helper =
    let canonMethod (httpMethodString: string) =
        httpMethodString.ToLower()

module RouteState =
    let addRoute route handler (routeState: RouteState) =
        { routeState with
            routes = Map.add route handler routeState.routes }

type NewRoute =
    { route : string
      methd : string
      handler : RouteHandler }

type RoutesBlock<'U>(userDomain)
    =
    inherit DomainBuilder<'U, RouteState>(userDomain)

    member this.AddRoute newRoute s =
        s |> UserDomain.update<'P, 'U, RouteState>
            this.UserDomain
            (RouteState.addRoute
                (newRoute.methd, newRoute.route)
                newRoute.handler)

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind=true)>]
    member this.Route
        (
            DomainRunner runCtx,
            [<ProjectionParameter>] getMethod,
            [<ProjectionParameter>] getRoute,
            [<ProjectionParameter>] getHandler
        ) =
        DomainRunner <| fun s ->
            let (ctx, s) = runCtx s

            ctx,
            s |> this.AddRoute
                  { route = getRoute ctx
                    methd = getMethod ctx
                    handler = getHandler ctx }

    [<CustomOperation("post", MaintainsVariableSpaceUsingBind=true)>]
    member this.Post
        (
            DomainRunner runCtx,
            [<ProjectionParameter>] getRoute,
            [<ProjectionParameter>] getHandler
        ) =
        DomainRunner <| fun s ->
            let (ctx, s) = runCtx s

            ctx,
            s |> this.AddRoute
              { route = getRoute ctx
                methd = "post"
                handler = getHandler ctx }
