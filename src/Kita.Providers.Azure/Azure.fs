namespace Kita.Providers.Azure

open System.IO
open System.Threading.Tasks
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
open Kita.Providers.Azure.Resources

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

        let! envVariables =
            provisionRequests
            |> List.map (fun req -> req rgName saName)
            |> Task.WhenAll<(string * string) option>

        return
            envVariables
            |> Array.choose id
            |> Array.toSeq

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
            provision
                appName
                location
                cloudTasks
                this.ExecuteProvisionRequests
            |> Async.AwaitTask

        member this.Activate () =
            let conString = System.Environment.GetEnvironmentVariable "Kita_AzureNative_ConnectionString"

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

    interface Definition.AzureWebPubSubProvider with
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

            awps :> Definition.IAzureWebPubSub
        
