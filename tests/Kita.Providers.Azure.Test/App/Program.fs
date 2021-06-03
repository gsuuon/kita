open System
open FSharp.Control.Tasks

open Kita.Core
open Kita.Domains
open Kita.Domains.Routes
open Kita.Providers.Azure

module AppSpec =
    type AppState =
        { routeState : RouteState }
        static member Empty = { routeState = RouteState.Empty }

    let routesDomain = 
        { new UserDomain<_,_> with
            member _.get s = s.routeState
            member _.set s rs = { s with routeState = rs } }

    let routes = RoutesBlock<AppState> routesDomain

module App = 
    open AppSpec
    open Kita.Domains.Routes.Http
    open Kita.Domains.Routes.Http.Helpers
    open Kita.Compile.Reflect

    let app =
        Block<AzureProvider, AppState> "myaznativeapp" {
        // TODO name is thrown away
        // This should be the app name
        // app names must be between 2-60 characters alphanumeric + non-leading hyphen
        let! q = Resource.Queue<string>("myaznatq")

        do! routes {
            get "hi" (fun _ -> async {
                let! xs = q.Dequeue 20
                return
                    sprintf "Got %A" xs
                    |> ok
            })

            post "hi" (fun req -> async {
                let text =
                    req.body
                    |> Seq.toArray
                    |> Text.Encoding.UTF8.GetString
                do! q.Enqueue text
                return ok "Ok sent"
            })
        }
    }

module AppOp =
    open AppSpec
    open App
    open Kita.Compile.Domains.Routes

    let attachedApp = 
        let provider = AzureProvider("myaznativeapp", "eastus")

        app
        |> Operation.attach provider

    [<RoutesEntrypoint("main")>]
    let runRouteState withDomain =
        attachedApp |> Routes.Operation.runRoutes routesDomain withDomain

    let launchRouteState withDomain =
        attachedApp |> Routes.Operation.launchRoutes routesDomain withDomain

[<EntryPoint>]
let main argv =
    // NOTE this needs to launch (provision + deploy)
    printfn "Deploying"

    AppOp.launchRouteState (fun routes -> printfn "App launched routes: %A" routes)

    0 // return an integer exit code
