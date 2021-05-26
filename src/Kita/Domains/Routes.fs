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

    let addHandlerMethod
        userDomain
        (DomainRunner runCtx)
        getMethod
        getPath
        getHandler
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

    let addHandler = addHandlerMethod userDomain

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
