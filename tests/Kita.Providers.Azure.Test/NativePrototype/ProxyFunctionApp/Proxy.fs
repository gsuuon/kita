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
    open Kita.Core.Http
    open Kita.Core.Http.Helpers

    let connectionString =
        Environment.GetEnvironmentVariable "Kita_ConnectionString"
        // TODO this needs to be a generated name

    let app =
        let routes =
            Managed.empty()
            |> ProxyApp.AutoReplacedReference.app.Attach
            |> fun managed ->
                managed.provider.Attach connectionString
                // TODO I feel like this project should depend on AzureNative, and I can make the type explicit here
                    // This depends on the replaced Kita_AssemblyReference

                managed
            |> fun managed ->
                managed.handlers
                |> Seq.fold
                    (fun (routes: Dictionary<_,Dictionary<_,_>>) mh ->
                        match routes.TryGetValue mh.route with
                        | true, routeHandlers ->
                            routeHandlers.[mh.method] <- mh.handler

                        | false, _ ->
                            let routeHandlers = Dictionary()
                            routeHandlers.[mh.method] <- mh.handler

                            routes.[mh.route] <- routeHandlers

                        routes
                    )
                    (Dictionary())
                :> IReadOnlyDictionary<_,_>

        let notFoundHandler req : Async<RawResponse> =
            async { return { body = "Not found :("; status = NOTFOUND } }

        let is (a: string) (b: string) =
            a.ToLower() = b.ToLower()

        fun route methd log ->
            log (sprintf "Matching route: %s & method: %s" route methd)

            match routes.TryGetValue route with
            | true, handlers ->
                match handlers.TryGetValue (canonMethod methd) with
                | true, handler -> handler
                | false, _ ->
                    log "Route doesn't handle method"
                    notFoundHandler

            | false, _ ->
                log "No such route"
                notFoundHandler

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

        let handler = app route req.Method log

        task {
            let! rawRequest = ProxyApp.Adapt.inRequest req
            let! rawResponse = handler rawRequest |> Async.StartAsTask
            let response = ProxyApp.Adapt.outResponse req rawResponse

            return response
        }
