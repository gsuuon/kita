module AzureApp.Program

open System.Reflection

open Kita.Core
open Kita.Domains
open Kita.Providers.Azure

open FSharp.Control.Tasks
open Kita.Providers.Azure.AzurePreviousApi
open Kita.Providers.Azure.Utility.LocalLog

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
        attachedApp.launch() |> Async.RunSynchronously
        attachedApp
        |> Routes.Operation.runRoutes routesDomain withDomain

[<AutoOpen>]
module Scratch =
    open Microsoft.EntityFrameworkCore
    open AzureApp.DbModel
    open System.Threading.Tasks

    let run (t: Task<'T>) = t.Wait()

    [<AutoOpen>]
    module Operations =
        let getApp appName location = task {
            report "Getting app"

            let saName = appName
            let rgName = appName

            let! appPlan = AppService.createAppServicePlan appName location rgName
            let! functionApp = AppService.createFunctionApp appName appPlan rgName saName

            return functionApp
        }

        let getDbCtx serverName dbName =
            let options =
                DbContextOptionsBuilder()
                    .UseSqlServer($"Server=tcp:{serverName}.database.windows.net,1433;Initial Catalog={dbName};Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;")
                    .AddInterceptors(Resources.Operation.AzureConnectionInterceptor())
                    .Options

            new ApplicationDbContext(options)

    module Procedures =
        let getManagedServiceId () = task {
            let! app = getApp "myaznativeapp" "useast"
            printfn "App system managed id: %A" app.SystemAssignedManagedServiceIdentityPrincipalId
        }

        let addUserDbContext () = task {
            let! app = getApp "myaznativeapp" "useast"
            let createStatement =
                ActiveDirectory.buildCreateUserSqlWorkaround
                    "E"
                    "myaznativeapp"
                    app.SystemAssignedManagedServiceIdentityPrincipalId

            printfn "Create statement sql:\n%s" createStatement

            (* let sqlCommand = *)
            (*     $""" *)
(* if not exists(select * from sys.database_principals where name = '{userName}') *)
    (* create user {userName} from external PROVIDER; *)
    (* alter role db_datareader add member {userName}; *)
    (* alter role db_datawriter add member {userName}; *)
(* """ *)
            (* printfn "Sql:\n%s" sqlCommand *)

            let sqlCommand = createStatement

            let dbContext = getDbCtx "kita-test-db" "Application"
            let! rows = dbContext.Database.ExecuteSqlRawAsync sqlCommand

            return rows
        }



[<EntryPoint>]
let main _argv =
    // NOTE this needs to launch (provision + deploy)

    (* Procedures.addUserDbContext().Wait() *)
    (* printfn "Done" *)

    (* printfn "Deploying" *)
    (* Operation.launchRouteState (fun routes -> printfn "\n\nApp launched routes: %A" routes) *)

    System.Reflection
        .Assembly
        .GetExecutingAssembly()
        .GetName()
        .Version
    |> printfn "Version: %A"


    0 // return an integer exit code
