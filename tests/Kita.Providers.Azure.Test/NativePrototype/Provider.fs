namespace AzureNativePrototype

open System
open FSharp.Control.Tasks

open Kita.Providers
open Kita.Utility

open AzureNativePrototype.Client
open AzureNextApi
open AzurePreviousApi
open Kita.Core

open System.IO

type AzureNative() =
    let defaultLocation = "eastus"

    let mutable provisionRequests = []
    let requestProvision provision =
        provisionRequests <- provision :: provisionRequests

    let connectionString = Waiter<string>()

    interface Provider with
        member _.Name = "Azure.Native"
        member _.Initialize () = printfn "Initializing"

    member val WaitConnectionString = connectionString
    member val OnConnection = connectionString.OnSet

    member _.Generate(app, conString) =
        GenerateProject.generateFunctionsAppZip
            (Path.Join(__SOURCE_DIRECTORY__, "ProxyFunctionApp"))
            app
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
        
    static member Run
        (
            appName,
            location,
            app: Managed<AzureNative> -> Managed<AzureNative>
        ) =
        AzureNative.Run(appName, location, Managed.empty(), app)

    static member Run
        (
            appName,
            location,
            start,
            app: Managed<AzureNative> -> Managed<AzureNative>
        ) = task {
        // FIXME how do I hide the type of location?
        // typed location makes it hard to have 1 line change to switch
        // vendor platform

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

        let managed = start |> app
        let provider = managed.provider

        let! (conString, rgName, saName) = provider.ProvisionGroup(appName, location)
        let provisionWork = provider.Provision(appName, conString, rgName, saName)
        let zipProjectWork = provider.Generate(app, conString)
            // TODO contextualize the logs of each process
            // logging channels

        let! (conString, functionApp) = provisionWork
        let! zipProject = zipProjectWork
        printfn "Generated zip project"
            // FIXME zip can fail if reference dlls are in use? (eg by an lsp server)
            // but we're only trying to copy
            // is there some way around this?
        let! deployment = provider.Deploy(conString, functionApp, zipProject)
        do! functionApp.SyncTriggersAsync()

        provider.Attach conString

        printfn "Deployed app -- https://%s" functionApp.DefaultHostName
        let! funKey = functionApp.AddFunctionKeyAsync("Proxy", "proxyKey", null)
        printfn "Key -- %s | %s" funKey.Name funKey.Value

        return managed
    }

    member _.RequestQueue (qName) =
        requestProvision <| Storage.createQueue qName
