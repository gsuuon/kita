namespace Company.Function

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open Microsoft.Extensions.Logging

open Giraffe
open FSharp.Control.Tasks

module GiraffeProxy =
    let app = 
        choose [
            GET >=> route "/api/hi" >=> htmlString "hi there"
            GET >=> route "/api/hello" >=> htmlString "hello there"
        ]

    [<FunctionName("GiraffeProxy")>]
    let run
        ([<HttpTrigger
            (AuthorizationLevel.Function,
                "get",
                "post",
                Route = "{*any}")>]req: HttpRequest,
                context: ExecutionContext,
                log: ILogger)
        =
        req.HttpContext.GetHostingEnvironment().ContentRootPath <- context.FunctionAppDirectory

        let ret x = x |> Some |> Task.FromResult

        { new Microsoft.AspNetCore.Mvc.IActionResult with
            member _.ExecuteResultAsync(ctx) = task {
                try
                    return! app ret ctx.HttpContext
                with exn ->
                    log.LogError
                    <| sprintf "Giraffe proxy errored: %A" exn
                    let handler =
                        clearResponse
                        >=> ServerErrors.INTERNAL_ERROR exn.Message

                    return! handler ret req.HttpContext } :> Task }
