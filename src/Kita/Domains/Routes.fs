namespace Kita.Domains.Routes

open Kita.Core
open Kita.Domains
open Kita.Domains.Routes.Http

type RouteAddress =
    { path : string
      method : string }

type RouteHandler = RawHandler
type RouteState =
    { routes : Map<RouteAddress, RawHandler> }
    static member Empty =
        { routes = Map.empty }

module private Helper =
    let canonMethod (httpMethodString: string) =
        httpMethodString.ToLower()

module RouteState =
    let addRoute routeAddress handler (routeState: RouteState) =
        { routeState with
            routes = Map.add routeAddress handler routeState.routes }

type RoutesBlock<'U>(userDomain)
    =
    inherit DomainBuilder<'U, RouteState>(userDomain)

    member this.AddRoute routeAddress routeHandler s =
        s |> UserDomain.update<'P, 'U, RouteState>
            this.UserDomain
            (RouteState.addRoute routeAddress routeHandler)

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
                  { path = getRoute ctx
                    method = getMethod ctx }
                    (getHandler ctx)

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
                  { path = getRoute ctx
                    method = "post" }
                    (getHandler ctx)
