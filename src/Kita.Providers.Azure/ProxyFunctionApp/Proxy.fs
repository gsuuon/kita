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

    let connectionString =
        Environment.GetEnvironmentVariable "Kita_ConnectionString"
        // TODO this needs to be a generated name

    let handleRoute log =
        let notFoundHandler req : Async<RawResponse> =
            async { return { body = "Not found :("; status = NOTFOUND } }

        let rootHandler =
            ProxyApp.AutoReplacedReference.appLauncher
            <| fun routeState ->
                fun routeAddress ->
                match routeState.routes.TryFind routeAddress with
                | Some handler ->
                    handler
                | None ->
                    log <| sprintf "Unknown route: %A" routeAddress
                    notFoundHandler

        (* ProxyApp.AutoReplacedReference.app.Run() *)
        // TODO Do I need to call run?

        rootHandler

    [<Function("Proxy")>]
    let run
        ([<HttpTrigger
            (AuthorizationLevel.Function,
                "get",
                "post",
                Route = "{**route}")>]req: HttpRequestData,
                route: string,
                context: FunctionContext)
        = 
        let log =
            let l = context.GetLogger()
            l.LogInformation

        log (sprintf "Proxy handling route: %s" route)

        let routeAddress =
            { path = route
              method = Helpers.canonMethod req.Method }

        let handler = handleRoute log routeAddress

        task {
            let! rawRequest = ProxyApp.Adapt.inRequest req
            let! rawResponse = handler rawRequest |> Async.StartAsTask
            let response = ProxyApp.Adapt.outResponse req rawResponse

            return response
        }
