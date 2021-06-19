namespace Kita.Providers.Azure

open System.IO
open FSharp.Control.Tasks

open Kita.Core
open Kita.Utility
open Kita.Resources
open Kita.Resources.Collections

open Kita.Providers.Azure
open Kita.Providers.Azure.Client
open Kita.Providers.Azure.AzurePreviousApi
open Kita.Providers.Azure.AzureNextApi
open Kita.Providers.Azure.Operations

type InjectableLogger =
    abstract SetLogger : Logger -> unit

type AzureProvider(appName, location) =
    let mutable cloudTasks = []
    let mutable provisionRequests = []

    let requestProvision provision =
        provisionRequests <- provision :: provisionRequests

    let connectionString = Waiter<string>()

    let mutable logger =
        { new Logger with
            member _.Info x = printfn "INFO: %s" x
            member _.Warn x = printfn "WARN: %s" x
            member _.Error x = printfn "ERROR: %s" x
        }

    let mutable launched = false
        // FIXME rework launch/run so this isn't necessary

    member val WaitConnectionString = connectionString
    member val OnConnection = connectionString.OnSet

    member _.Generate(conString) =
        // TODO find the RootBlock attribute which contains this provider
        GenerateProject.generateFunctionsAppZip
            (Path.Join(__SOURCE_DIRECTORY__, "ProxyFunctionApp"))
            conString
            appName
            cloudTasks

    member _.Attach (conString: string) =
        connectionString.Set conString
        
    member _.ExecuteProvisionRequests (rgName, saName) = task {
        printfn "Provisioning resources"
        for provision in provisionRequests do
            do! provision rgName saName
        }

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
            if not launched then
                launched <- true

                let work =
                    provision
                        appName
                        location
                        cloudTasks
                        this.ExecuteProvisionRequests

                work.Wait()

        member this.Run () =
            let conString = System.Environment.GetEnvironmentVariable "Kita_AzureNative_ConnectionString"

            if conString <> null then
                this.Attach conString
            else
                failwith "Connection string environment variable is missing, it needs to be set to run the Azure provider."

    interface InjectableLogger with
        member _.SetLogger lg = logger <- lg

    interface CloudQueueProvider with
        member _.Provide (name) =
            Resources.Provision.AzureCloudQueue
                ( name
                , connectionString
                , (fun () -> requestProvision <| Storage.createQueue name)
                ) :> ICloudQueue<_>

    interface CloudMapProvider with
        member _.Provide<'K, 'V> name =
            Resources.Provision.AzureCloudMap
                ( name
                , connectionString
                , fun () -> requestProvision <| Storage.createMap name
                ) :> ICloudMap<'K, 'V>

    interface CloudLogProvider with
        // NOTE
        // The logger will be the last set logger
        // Meaning if it's used in a call which doesn't set the logger
        // It will attribute the log to the previous request
        // Could cause problems?
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
