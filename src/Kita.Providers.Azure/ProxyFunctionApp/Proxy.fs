namespace ProxyApp

open FSharp.Control.Tasks

module Proxy =
    open System
    open System.Net
    open System.Text.Json
    open System.Collections.Generic

    open Microsoft.Azure.Functions.Worker
    open Microsoft.Azure.Functions.Worker.Http
    open Microsoft.Extensions.Logging
    open Microsoft.Azure.Functions.Worker.Pipeline

    open Kita.Core
    open Kita.Domains
    open Kita.Domains.Routes
    open Kita.Domains.Routes.Http
    open Kita.Providers.Azure
    open Kita.Providers.Azure.RunContext

    let connectionString =
        Environment.GetEnvironmentVariable
            "Kita_AzureNative_ConnectionString"
        // TODO this needs to be a generated name

    let runModule = ProxyApp.AutoReplacedReference.runModule :> AzureRunModule<_>

    let notFoundHandler _req : Async<RawResponse> =
        async { return { body = "Not found :("; status = NOTFOUND } }

    let injectLog (lg: ILogger) =
        let logInjecter = runModule.Provider :> InjectableLogger

        logInjecter.SetLogger
            { new Kita.Resources.Logger with
                member _.Info x = lg.LogInformation x
                member _.Warn x = lg.LogWarning x
                member _.Error x = lg.LogError x
            }

    let handleRoutes (routeState: RouteState) =
        let rootHandler (lg: ILogger) routeAddress =
            match routeState.routes.TryFind routeAddress with
            | Some handler ->
                injectLog lg

                handler
            | None ->
                lg.LogInformation <| sprintf "Unknown route: %A" routeAddress
                notFoundHandler

        rootHandler

    let handleRoute =
        id
        |> runModule.RunRouteState 
        |> handleRoutes

    let handleAuthedRoutes =
        id
        |> runModule.RunAuthedRouteState
        |> handleRoutes
        
    [<Function("Proxy")>]
    let run
        ([<HttpTrigger
            (AuthorizationLevel.Anonymous,
                "get",
                "post",
                Route = "{**route}")>] req: HttpRequestData,
            route: string,
            context: FunctionContext
        ) = 
        let lg = context.GetLogger() :> ILogger

        lg.LogInformation (sprintf "Proxy handling route: %s" route)

        let routeAddress =
            { path = route
              method = Helpers.canonMethod req.Method }

        let handler = handleRoute lg routeAddress

        task {
            let! rawRequest = ProxyApp.Adapt.inRequest req
            let! rawResponse = handler rawRequest |> Async.StartAsTask
            let response = ProxyApp.Adapt.outResponse req rawResponse

            return response
        }

    [<Function("AuthedProxy")>]
    let runAuthed
        ([<HttpTrigger
            (AuthorizationLevel.Function,
                "get",
                "post",
                Route = "authed/{**route}")>] req: HttpRequestData,
            route: string,
            context: FunctionContext
        ) = 
        let lg = context.GetLogger() :> ILogger

        lg.LogInformation (sprintf "Proxy handling authed route: %s" route)

        let routeAddress =
            { path = route
              method = Helpers.canonMethod req.Method }

        let handler = handleAuthedRoutes lg routeAddress

        task {
            let! rawRequest = ProxyApp.Adapt.inRequest req
            let! rawResponse = handler rawRequest |> Async.StartAsTask
            let response = ProxyApp.Adapt.outResponse req rawResponse

            return response
        }
