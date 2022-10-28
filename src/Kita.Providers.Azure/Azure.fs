namespace Kita.Providers.Azure

open System.IO
open System.Threading.Tasks
open FSharp.Control.Tasks

open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Diagnostics
open Microsoft.Azure.Management.AppService.Fluent

open Kita.Core
open Kita.Utility
open Kita.Resources
open Kita.Resources.Collections

open Kita.Providers.Azure
open Kita.Providers.Azure.Client
open Kita.Providers.Azure.Activation
open Kita.Providers.Azure.AzurePreviousApi
open Kita.Providers.Azure.AzureNextApi
open Kita.Providers.Azure.Operations
open Kita.Providers.Azure.Resources
open Kita.Providers.Azure.Resources.Definition
open Kita.Providers.Azure.Resources.Operation
open Kita.Providers.Azure.Utility.LocalLog


type InjectableLogger =
    abstract SetLogger : Logger -> unit

type AzureProvider(appName, location) =
    let mutable cloudTasks = []
    let mutable provisionRequests = []
    let mutable provisionRequestsWithApp = []

    /// rgName -> saName -> unit task
    let requestProvision provision =
        provisionRequests <- provision :: provisionRequests

    /// IFunctionApp -> unit task
    let requestProvisionAfterApp provision =
        provisionRequestsWithApp <- provision :: provisionRequestsWithApp

    let connectionString = Waiter<string>()

    let mutable logger =
        { new Logger with
            member _.Info x = printfn "INFO: %s" x
            member _.Warn x = printfn "WARN: %s" x
            member _.Error x = printfn "ERROR: %s" x
        }

    let executeRequests executor requests = task {
        let! envVariables =
            requests
            |> List.map executor
            |> Task.WhenAll<(string * string) option>

        return
            envVariables
            |> Array.choose id
            |> Array.toSeq
        }

    member _.Generate(conString) =
        // TODO find the RootBlock attribute which contains this provider
        GenerateProject.generateFunctionsAppZip
            (Path.Join(__SOURCE_DIRECTORY__, "ProxyFunctionApp"))
            conString
            appName
            cloudTasks

    member _.Attach (conString: string) =
        connectionString.Set conString
        
    member _.ExecuteProvisionRequests (rgName, saName) =
        executeRequests
        <| fun req -> req rgName saName
        <| provisionRequests

    member _.ExecuteProvisionRequestsAfterApp (app: IFunctionApp) =
        executeRequests
        <| fun req -> req app
        <| provisionRequestsWithApp

    member _.CloudTasks = cloudTasks

    interface Provider with
        // FIXME enforce lowercase only
        // Same with queue names
        // azure most names must be lowercase i guess?

        // TODO generate app-namespaced connection string env variable
        // Use SetParameters in IFunctionApp.Deploy()..
        // Directly set into environment for local deploy
        // Or use local.settings.json? Would make it straightforward to use
        // local hosting to debug
        // -- I think I'd need both, if the process isnt started using azure func
        // -- e.g the server is Local / Giraffe, the env variable still needs to be there

        // I can use command-line configuration provider to inject keys
        // https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#command-line-configuration-provider
        // Not sure how I would use this with zipdeploy?
        // Could be useful for local provider

        member this.Launch () =
            provision
                appName
                location
                cloudTasks
                this.ExecuteProvisionRequests
                this.ExecuteProvisionRequestsAfterApp
            |> Async.AwaitTask

        member this.Activate () =
            let conString =
                System.Environment.GetEnvironmentVariable 
                    AzureConnectionStringVarName

            if conString <> null then
                this.Attach conString
            else
                failwith "Connection string environment variable is missing, it needs to be set to run the Azure provider."

    interface InjectableLogger with
        member _.SetLogger lg = logger <- lg

    interface CloudQueueProvider with
        member _.Provide (name: string) =
            Resources.Provision.AzureCloudQueue
                (name, requestProvision) :> ICloudQueue<_>

    interface CloudMapProvider with
        member _.Provide<'K, 'V> (name: string) =
            Resources.Provision.AzureCloudMap
                (name, requestProvision) :> ICloudMap<'K, 'V>

    interface CloudLogProvider with
        // TODO
        // Wrap in thread-local object or restructure entirely
        member _.Provide () =
            { new ICloudLog with
                member _.Info x = logger.Info x
                member _.Warn x = logger.Warn x
                member _.Error x = logger.Error x
            }

    interface CloudTaskProvider with
        member _.Provide (chronExpr, work) =
            let cloudTask =
                { new ICloudTask with
                    member _.Chron = chronExpr
                    member _.Work = work }

            cloudTasks <- cloudTask :: cloudTasks

            cloudTask

    interface AzureWebPubSubProvider with
        member _.Provide (name, config) =
            let awps = Provision.AzureWebPubSub(name, appName)

            requestProvision 
            <| awps.ProvisionRequest
                { location = location
                  name = appName
                  tier = config.tier
                  skuName =
                      match config.tier with
                      | "free" ->
                          "Free_F1"
                      | tier ->
                          failwithf
                              "Don't know the skuName for tier %s"
                              tier
                  capacity = 1
                }

            awps :> IAzureWebPubSub
        
    interface CloudCacheProvider with
        member _.Provide name =
            Provision.AzureCloudCache
                ( name
                , requestProvision
                , location
                ) :> ICloudMap<_,_>


    interface AzureDatabaseSQLProvider with
        member _.Provide<'T when 'T :> DbContext>
            (
                serverName,
                createCtx
            ) =
            let connectionStringEnvVarName =
                $"Kita_Azure_DbSQL_{serverName}"
                |> canonEnvVarName

            let createOptions (conString: string) =
                (new DbContextOptionsBuilder())
                    .UseSqlServer(conString)
                    .AddInterceptors(AzureConnectionInterceptor())
                    .Options

            requestProvisionAfterApp
            <| fun app -> task {
                report "Creating sql server.."
                let rgName = app.ResourceGroupName
                
                let adGroupName = $"kita_{app.Name}_sql_{serverName}"
                (* let waitAdGroup = task { *)
                (*     let! adGroup = *)
                (*         ActiveDirectory.createADGroup adGroupName *)
                (*         (1* 403 forbidden *1) *)
                (*         (1* subscription id not ad admin *1) *)
                (*         (1* should i just require an ad tenant for the app? *1) *)
                (*         (1* or app has admin rights to a tenant, and advise users *1) *)
                (*         (1* create a new tenant just for kita apps? *1) *)

                (*     let! _updated = *)
                (*         app.SystemAssignedManagedServiceIdentityPrincipalId *)
                (*         |> ActiveDirectory.addMemberToADGroup adGroup *)

                (*     return () *)
                (* } *)

                let! sqlServer =
                    SqlServer.createSqlServerRngUser
                        serverName
                        location
                        rgName
                        []
                
                // Assume that the app managed identity name is app name
                
                let dbName =
                    (typeof<'T>).Name
                        .Replace("Context","")
                        .Replace("Db","")

                report "Provisioning db: %s"dbName
                let! db =
                    SqlServer.createSqlDatabase
                        dbName
                        sqlServer

                let connectionString =
                    $"Server=tcp:{sqlServer.FullyQualifiedDomainName},1433;Initial Catalog={dbName};Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;"

                let dbCtx = connectionString |> createOptions |> createCtx

                report "Checking database migrations for %s" serverName

                let migrations = dbCtx.Database.GetMigrations() |> Seq.toList
                report "Found %i migrations" migrations.Length
                // NOTE
                // I think migrations aren't found because this dbctx type is actually different than
                // the one which generated the migrations
                // not sure how to get around this

                let! _pendingMigrations = dbCtx.Database.GetPendingMigrationsAsync()

                let pendingMigrations = Seq.toList _pendingMigrations

                report "Found %i pending migrations. Applying.." pendingMigrations.Length

                do! dbCtx.Database.MigrateAsync()
                report "Migrated database %s" serverName
                
                (* do! waitAdGroup *)

                // TODO-next add create user sql stuff here

                return Some (connectionStringEnvVarName, connectionString)
            }

            let conString =
                defaultArg
                <| getActivationData connectionStringEnvVarName
                <| ""

            { new IAzureDatabaseSQL<'T> with
                member _.GetContext () = conString |> createOptions |> createCtx }
