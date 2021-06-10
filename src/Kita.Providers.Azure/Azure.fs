namespace Kita.Providers.Azure

open System.IO
open FSharp.Control.Tasks

open Kita.Core
open Kita.Utility
open Kita.Providers.Azure
open Kita.Providers.Azure.Client
open Kita.Providers.Azure.AzurePreviousApi
open Kita.Providers.Azure.AzureNextApi

open Kita.Resources
open Kita.Resources.Collections

type InjectableLogger =
    abstract SetLogger : Logger -> unit

type AzureProvider(appName, location) =
    let defaultLocation = "eastus"

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

    member _.Deploy (conString, functionApp, generatedZip: byte[]) = task {
        let blobs = Blobs(conString)

        let! blobContainerClient =
            blobs.BlobContainerClient "deploy-zips-azure"

        let blobClient = blobContainerClient.GetBlobClient("latest-deploy.zip")

        use mem = new System.IO.MemoryStream(generatedZip)
        let! _info = blobClient.UploadAsync(mem, true) |> rValue

        printfn "Uploaded blob"

        let! blobUri =
            Blobs.BlobGenerateSas
                BlobPermission.Read
                1.0
                blobClient

        printfn "Blob sas uri:\r\n%s" blobUri.AbsoluteUri

        let! deployment =
            AppService.deployFunctionApp
                conString
                blobUri.AbsoluteUri
                functionApp

        return deployment

        }

    member _.Attach (conString: string) =
        connectionString.Set conString
        
    member _.ProvisionGroup (appName, location) = task {
        printfn "Provisioning %s for %s" appName location
        let! rg = Resources.createResourceGroup appName location
        let! sa = Storage.createStorageAccount appName location

        let rgName = rg.Name
        let saName = sa.Name

        let! key = Storage.getFirstKey rgName saName

        return Storage.formatKeyToConnectionString key saName, rgName, saName

        }

    member _.Provision (appName, conString, rgName, saName) = task {
        for provision in provisionRequests do
            do! provision rgName saName

        let! appPlan = AppService.createAppServicePlan appName rgName

        let! functionApp =
            AppService.createFunctionApp
                appName
                appPlan
                rgName

        printfn "Using function app: %s" functionApp.Name

        return conString, functionApp

        }

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
                let work = task {
                    let! (conString, rgName, saName) = this.ProvisionGroup(appName, location)
                    let provisionWork = this.Provision(appName, conString, rgName, saName)
                    let zipProjectWork = this.Generate(conString)
                        // TODO contextualize the logs of each process
                        // logging channels

                    let! (conString, functionApp) = provisionWork
                    let! zipProject = zipProjectWork
                    printfn "Generated zip project"
                        // FIXME zip can fail if reference dlls are in use? (eg by an lsp server)
                        // but we're only trying to copy
                        // is there some way around this?

                    let! deployment = this.Deploy(conString, functionApp, zipProject)
                    do! functionApp.SyncTriggersAsync()

                    let appUri = sprintf "https://%s" functionApp.DefaultHostName
                    printfn "Deployed app -- %s" appUri

                    let! funKey = functionApp.AddFunctionKeyAsync("Proxy", "proxyKey", null)
                    let appAccessKey = funKey.Value

                    printfn "Key -- %s | %s" funKey.Name funKey.Value
                    printfn "\n\nAccess endpoints:"
                    printfn "%s/api/<endpoint>?code=%s" appUri appAccessKey
                }

                work.Wait()

        member this.Run () =
            let conString = System.Environment.GetEnvironmentVariable "Kita_AzureNative_ConnectionString"

            if conString <> null then
                this.Attach conString
            else
                failwith "Connection string environment variable is missing, it needs to be set to run the Azure provider."

    interface CloudQueueProvider with
        member _.Provide (name) =
            Resources.AzureCloudQueue
                ( name
                , connectionString
                , (fun () -> requestProvision <| Storage.createQueue name)
                ) :> ICloudQueue<_>

    interface CloudMapProvider with
        member _.Provide<'K, 'V> name =
            Resources.AzureCloudMap
                ( name
                , connectionString
                , fun () -> requestProvision <| Storage.createMap name
                ) :> ICloudMap<'K, 'V>

    interface InjectableLogger with
        member _.SetLogger lg = logger <- lg
