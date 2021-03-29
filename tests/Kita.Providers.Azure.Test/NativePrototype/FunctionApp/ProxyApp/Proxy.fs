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

    let connectionString =
        Environment.GetEnvironmentVariable "Kita_ConnectionString"

    let app =
        let routes =
            Managed.empty()
            |> Program.App.app
                // TODO user app
            |> fun managed ->
                managed.provider.Attach connectionString; managed
            |> fun managed ->
                managed.handlers
                |> Seq.fold
                    (fun (routes: Dictionary<_,Dictionary<_,_>>)
                         (route, handler) ->

                        match routes.TryGetValue route with
                        | true, routeHandlers ->
                            routeHandlers.[handler.MethodString()]
                                <- handler.Handler()

                        | false, _ ->
                            let routeHandlers = Dictionary()
                            routeHandlers.[handler.MethodString()]
                                <- handler.Handler()
                                
                            routes.[route] <- routeHandlers

                        routes
                    )
                    (Dictionary())
                :> IReadOnlyDictionary<_,_>

        let notFoundHandler req : Async<RawResponse> =
            async { return { body = "Not found :("; status = NOTFOUND } }

        let is (a: string) (b: string) =
            a.ToLower() = b.ToLower()

        fun route methd log ->
            log (sprintf "Matching route: %s %s" route methd)

            match routes.TryGetValue route with
            | true, handlers ->
                match handlers.TryGetValue methd with
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
