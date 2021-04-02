module GiraffePrototype.Program

open System.IO
open Kita.Core

module Server =
    open System.Threading.Tasks
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Hosting
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Hosting
    open Microsoft.Extensions.DependencyInjection
    open System.Buffers
    open FSharp.Control.Tasks
    open Kita.Core.Http
    open Giraffe

    let wrapHandler (handler: RawHandler) = // RawHandler to Giraffe handler
        let readBody (br: Pipelines.PipeReader) = task {
            let! result = br.ReadAsync().AsTask()
            return result.Buffer }

        let transformRequest (req: HttpRequest) : Task<RawRequest>
            = task {

            let! body = readBody req.BodyReader

            let kvSeqToDict transformValue aSeq =
                aSeq
                |> Seq.map
                    (fun (KeyValue(k, v)) ->
                        k, transformValue v )
                |> dict

            return
                { body =
                    body.ToArray()
                  queries =
                    req.Query
                    |> kvSeqToDict (fun v -> v.ToArray() |> Array.toList)
                  headers =
                    req.Headers
                    |> kvSeqToDict (fun v -> v.ToArray() |> Array.toList)
                  cookies =
                    req.Cookies
                    |> kvSeqToDict id } }

        let writeBody
            (bw: Pipelines.PipeWriter)
            (res: RawResponse)
            = task {

            let! _flushResult =
                let vtask = 
                    res.body
                    |> System.Text.Encoding.UTF8.GetBytes
                    |> System.ReadOnlyMemory
                    |> bw.WriteAsync

                vtask.AsTask()

            return () }

        let transformResponse
            (res: RawResponse)
            (ctx: HttpContext)
            = task {

            match res.status with
            | OK ->
                ctx.Response.StatusCode <- 200
            | NOTFOUND ->
                ctx.Response.StatusCode <- 404

            do! writeBody ctx.Response.BodyWriter res

            }
            
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                printfn "Handling.."
                let! request = transformRequest ctx.Request
                let! response = handler request
                do! transformResponse response ctx
                return! next ctx
            }
        
    let handlersToApp (routeHandlers: MethodHandler list) =
        let canonPath (path: string) =
            if not (path.StartsWith "/") then
                "/" + path
            else
                path

        let routeAndWrap path handler =
            routeCi (canonPath path)
            >=> wrapHandler handler

        routeHandlers
        |> List.map
            (fun mh ->
                let verb =
                    match mh.method with
                    | "post" -> POST
                    | "get" -> GET
                    | x -> failwithf "Unknown method: %s" x

                verb >=> routeAndWrap mh.route mh.handler
            )
        |> choose

    let configureApp webApp (app: IApplicationBuilder) =
        app.UseGiraffe webApp

    let configureServices (services: IServiceCollection) =
        services.AddGiraffe() |> ignore

    let start handlers =
        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(
                fun webHostBuilder ->
                    webHostBuilder
                        .Configure(handlers |> handlersToApp |> configureApp)
                        .ConfigureServices(configureServices)
                        |> ignore
                )
            .Build()
            .Run()

module App =
    open Kita.Core.Infra
    open Kita.Core.Http
    open Kita.Core.Http.Helpers
    open Kita.Providers

    let local = infra'<Local>

    let localApp =
        local "testApp" {
            route "hello" [
                ok "Hows it going" |> asyncReturn |> konst |> get
            ]
            route "/hi" [
                ok "Hi there" |> asyncReturn |> konst |> get
            ]
        }
        <| Managed.empty()

open Kita.Core.Managed

[<EntryPoint>]
let main _argv =
    Server.start App.localApp.handlers

    // managed contains route handlers with bound references
    // resources

    0
