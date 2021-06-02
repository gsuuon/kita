module GiraffePrototype.Program

open System.IO
open Kita.Core
open Kita.Domains
open Kita.Domains.Routes
open Kita.Domains.Routes.Http
open Kita.Domains.Routes.Http.Helpers

module Server =
    open System.Threading.Tasks
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Hosting
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Hosting
    open Microsoft.Extensions.DependencyInjection
    open System.Buffers
    open FSharp.Control.Tasks

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
        
    let routeStateToApp (routeState: RouteState) =
        let canonPath (path: string) =
            if not (path.StartsWith "/") then
                "/" + path
            else
                path

        let routeAndWrap routeAddress handler =
            let verb =
                match routeAddress.method with
                | "post" -> POST
                | "get" -> GET
                | x -> failwithf "Unknown method: %s" x

            routeCi (canonPath routeAddress.path)
            >=> wrapHandler handler
            >=> verb

        routeState.routes
        |> Map.map routeAndWrap
        |> Map.toList
        |> List.map snd
        |> choose

    let configureApp webApp (app: IApplicationBuilder) =
        app.UseGiraffe webApp

    let configureServices (services: IServiceCollection) =
        services.AddGiraffe() |> ignore

    let start routeState =
        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(
                fun webHostBuilder ->
                    webHostBuilder
                        .Configure(routeState |> routeStateToApp |> configureApp)
                        .ConfigureServices(configureServices)
                        |> ignore
                )
            .Build()
            .Run()

module App =
    open Kita.Core
    open Kita.Utility
    open Kita.Providers

    type AppState =
        { routeState : RouteState }
        static member Empty = { routeState = RouteState.Empty }

    [<AutoOpen>]
    module private AppState =
        let ``.routeState`` s = s.routeState
        let ``|.routeState`` rs s = { s with routeState = rs }

    let local name = Block<_, AppState> name
    let routes =
        RoutesBlock<AppState>
            { new UserDomain<_,_> with
                member _.get s = s.routeState
                member _.set s rs = { s with routeState = rs } }

    let retOk = Kita.Domains.Routes.Http.Helpers.returnOk
    let localBlock =
        local "testApp" {
            do! routes {
                get "hello" (ok "Hows it going" |> asyncReturn |> konst)
                get "/hi" (ok "Hi there" |> asyncReturn |> konst)
            }

            return ()
        }

    let localApp = localBlock |> Operation.attach (Local())
        // Provider doesn't really do anything in this example

    let launch withRouteState =
        let routesCollector = Routes.Operation.RoutesCollector()

        localApp.run (``.routeState`` >> routesCollector.Collect)

        withRouteState routesCollector.RouteState

[<EntryPoint>]
let main _argv =
    let x = App.localApp

    App.launch Server.start

    0
