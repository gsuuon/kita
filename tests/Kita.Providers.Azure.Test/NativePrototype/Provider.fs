namespace AzureNativePrototype

open System

open FSharp.Control.Tasks

open Kita.Core
open Kita.Providers
open Kita.Utility

open AzureNextApi
open AzurePreviousApi

type AzureNative() =
    inherit Provider("Azure.Native")
    let defaultLocation = "eastus"

    let mutable provisionRequests = []
    let requestProvision provision =
        provisionRequests <- provision :: provisionRequests

    let connectionString = Waiter<string>()
    member _.WaitConnectionString = connectionString
    member _.OnConnection = connectionString.OnSet
    member _.Deploy
        appName
        location
        (managed: Managed<AzureNative>)
        = task {

        let! rg = Resources.createResourceGroup appName location
        let! sa = Storage.createStorageAccount appName location

        let rgName = rg.Name
        let saName = sa.Name

        let! key = Storage.getFirstKey rgName saName
        let conString = Storage.formatKeyToConnectionString key saName

        for provision in provisionRequests do
            do! provision rgName saName

        connectionString.Set conString

        let! appPlan = AppService.createAppServicePlan appName rgName

        let! functionApp =
            AppService.createFunctionApp
                appName
                appPlan
                rgName

        let! data = GenerateProject.generateFunctionsAppZip managed

        let! blobContainerClient =
            Blobs.blobContainerClient "deploy-zips-azure"

        let blobClient = blobContainerClient.GetBlobClient("latest-deploy")

        let! info =
            use mem = new System.IO.MemoryStream(data)
            blobClient.UploadAsync(mem) |> rValue

        let! blobUri =
            Blobs.blobGenerateSas
                Blobs.Permission.Read
                1.0
                blobClient


        (* let! deployment = *)
        (*     functionApp *)
        (*         .Deploy() *)
        (*         .WithPackageUri(blobUri.AbsoluteUri) *)
        (*         .WithExistingDeploymentsDeleted(false) *)
        (*         .ExecuteAsync() *)

        printfn "Blob sas uri: %s" blobUri.AbsoluteUri

        return ()

        }
        
    member _.RequestQueue (qName) =
        requestProvision <| Storage.createQueue qName
