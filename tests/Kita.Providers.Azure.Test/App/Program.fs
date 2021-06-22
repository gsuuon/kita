module AzureApp.Program

open Kita.Core
open Kita.Domains
open Kita.Providers.Azure

module Operation =
    open AzureApp.App
    open Kita.Providers.Azure.RunContext
    open Kita.Providers.Azure.Compile

    let provider = AzureProvider("myaznativeapp", "eastus")
    let attachedApp = app |> Operation.attach provider

    [<AzureRunModuleFor("myaznativeapp")>]
    type AzureRunner() =
        interface AzureRunModule<AppState> with
            member _.Provider = provider

            member _.RunRouteState withDomain =
                attachedApp |> Routes.Operation.runRoutes routesDomain withDomain

            member _.RunAuthedRouteState withDomain =
                attachedApp |> Routes.Operation.runRoutes authedRoutesDomain withDomain

    // Does it make more sense to run / launch, and _then_ do work on domains?
    // In the proxy project, if I run routes then run logs, I'd need to remember
    // in the RunApp if I've run or not. But if I've already run, there's no way to
    // access all the blocks again without running again. That means run needs to be
    // idempotent, which it currently is but is not guaranteed to stay that way.

    let launchRouteState withDomain =
        attachedApp |> Routes.Operation.launchRoutes routesDomain withDomain

[<EntryPoint>]
let main argv =
    // NOTE this needs to launch (provision + deploy)
    printfn "Deploying"

    Operation.launchRouteState (fun routes -> printfn "\n\nApp launched routes: %A" routes)

    0 // return an integer exit code
