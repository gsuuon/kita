open System
open FSharp.Control.Tasks

open AzureNativePrototype
open Kita.Core
open Kita.Domains
open Kita.Domains.Routes

module App = 
    open Kita.Domains.Routes.Http
    open Kita.Domains.Routes.Http.Helpers
    open Kita.Compile

    type AppState =
        { routeState : RouteState }
        static member Empty = { routeState = RouteState.Empty }

    let routesDomain = 
        { new UserDomain<_,_> with
            member _.get s = s.routeState
            member _.set s rs = { s with routeState = rs } }

    let routes = RoutesBlock<AppState> routesDomain

    let app =
        Block<AzureNative, AppState> "myaznativeapp" {
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

    let deploy () =
        let provider = AzureNative("myaznativeapp", "eastus")

        app |> Operation.attach provider

    [<RootBlock("main")>]
    type AppLauncher<'T>() =
        interface DomainLauncher<RouteState, 'T> with
            member _.Launch withDomain =
                let provider = AzureNative("myaznativeapp", "eastus")

                app
                |> Operation.attach provider
                |> Routes.Operation.launchRoutes routesDomain withDomain

[<EntryPoint>]
let main argv =
    let managed =
        printfn "Deploying"

        App.deploy()

    0 // return an integer exit code
