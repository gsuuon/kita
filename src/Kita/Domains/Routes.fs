namespace Kita.Domains.Routes

open Kita.Core
open Kita.Utility
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

[<AutoOpen>]
module private RoutesBlock =
    let addRoute userDomain routeAddress handler bindState =
        let updater = RouteState.addRoute routeAddress handler
        let updateUserDomain =
            UserDomain.update<'P, 'U, RouteState> userDomain updater

        bindState |> updateUserDomain

    let addHandlerMethod<'P, 'U, 'a when 'P :> Provider>
        userDomain
        (DomainRunner runCtx)
        getMethod
        getPath
        getHandler
        : DomainRunner<'P,'U,'a>
        =
        DomainRunner <| fun s ->
            let (ctx, s) = runCtx s

            let routeAddress =
                { path = getPath ctx
                  method = getMethod ctx }
            let handler = getHandler ctx

            ctx, s |> addRoute userDomain routeAddress handler
        
type RoutesBlock<'U>(userDomain)
    =
    inherit DomainBuilder<'U, RouteState>(userDomain)

    let addHandler x = addHandlerMethod userDomain x

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind=true)>]
    member _.Route
        (
            rCtx,
            [<ProjectionParameter>] getMethod,
            [<ProjectionParameter>] getPath,
            [<ProjectionParameter>] getHandler
        ) =
        addHandler rCtx getMethod getPath getHandler

    [<CustomOperation("post", MaintainsVariableSpaceUsingBind=true)>]
    member this.Post
        (
            rCtx,
            [<ProjectionParameter>] getPath,
            [<ProjectionParameter>] getHandler
        ) =
        addHandler rCtx (konst "post") getPath getHandler

    [<CustomOperation("get", MaintainsVariableSpaceUsingBind=true)>]
    member this.Get
        (
            rCtx,
            [<ProjectionParameter>] getPath,
            [<ProjectionParameter>] getHandler
        ) =
        addHandler rCtx (konst "get") getPath getHandler

module Operation =
    type RoutesCollector() =
        let mutable routes = Map.empty
        let addRoute routeAddress handler =
            routes <- Map.add routeAddress handler routes

        member _.Collect (routeState: RouteState) =
                routeState.routes |> Map.iter addRoute

        member _.RouteState = { routes = routes }

    type RoutesLauncher<'U, 'a>(app, routesDomain: UserDomain<'U, RouteState>) =
        member _.Launch(withRouteState: RouteState -> 'a) =
            let routesCollector = RoutesCollector()

            app.launch (routesDomain.get >> routesCollector.Collect)

            withRouteState routesCollector.RouteState

    let launchRoutes routesDomain withRoutes app =
        RoutesLauncher(app, routesDomain).Launch withRoutes
