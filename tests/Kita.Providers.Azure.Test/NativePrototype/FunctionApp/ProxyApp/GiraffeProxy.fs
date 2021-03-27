namespace Company.Function

open Giraffe
open FSharp.Control.Tasks

module GiraffeProxy =
    open System
    open System.Net
    open System.Text.Json
    open Microsoft.Azure.Functions.Worker
    open Microsoft.Azure.Functions.Worker.Http
    open Microsoft.Extensions.Logging
    open Microsoft.Azure.Functions.Worker.Pipeline

    open Kita.Core

    let connectionString =
        Environment.GetEnvironmentVariable "Kita_ConnectionString"

    let app =
        Managed.empty()
        |> Program.App.app
        |> fun managed ->
            managed.provider.Attach connectionString; managed
    
    [<Function("GiraffeProxy")>]
    let run
        ([<HttpTrigger
            (AuthorizationLevel.Function,
                "get",
                "post",
                Route = "{**route}")>]req: HttpRequestData,
                route: string,
                context: FunctionContext,
                log: ILogger)
        =

        let response = req.CreateResponse(HttpStatusCode.OK)
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString(sprintf "Welcome to Azure Functions! %s" route);

        response
