module Kita.Providers.Azure.Operations

open FSharp.Control.Tasks
open System.Threading.Tasks

open Kita.Providers.Azure.Client
open Kita.Providers.Azure.AzurePreviousApi
open Kita.Providers.Azure.AzureNextApi

let provisionCloudGroup appName location = task {
    printfn "Provisioning %s for %s" appName location
    let! rg = Resources.createResourceGroup appName location
    let! sa = Storage.createStorageAccount appName location

    let rgName = rg.Name
    let saName = sa.Name

    let! key = Storage.getFirstKey rgName saName

    return Storage.formatKeyToConnectionString key saName, rgName, saName
}

let uploadZipGetBlobSas conString (generatedZip: byte[]) = task {
    printfn "Uploading proxy project archive"

    let blobs = Blobs(conString)

    let! blobContainerClient =
        blobs.BlobContainerClient "deploy-zips-azure"

    let blobClient = blobContainerClient.GetBlobClient("latest-deploy.zip")

    use mem = new System.IO.MemoryStream(generatedZip)
    let! _info = blobClient.UploadAsync(mem, true) |> rValue

    printfn "Uploaded archive"

    let! blobUri =
        Blobs.BlobGenerateSas
            BlobPermission.Read
            1.0
            blobClient

    return blobUri.AbsoluteUri
}

let provision
    appName
    location
    cloudTasks
    (executeProvisionRequests:
        string * string -> Task<unit>)
    = task {

    let! (conString, rgName, saName) =
        provisionCloudGroup appName location

    let execProvisionRequestsWork =
        executeProvisionRequests(rgName, saName)

    let! zipProject = 
        GenerateProject.generateFunctionsAppZip
            (System.IO.Path.Join(__SOURCE_DIRECTORY__, "ProxyFunctionApp"))
            conString
            appName
            cloudTasks

    printfn "Generated zip project"
        // FIXME zip can fail if reference dlls are in use? (eg by an lsp server)
        // but we're only trying to copy
        // is there some way around this?

    execProvisionRequestsWork.Wait()
    printfn "Finished provisioning resources"

    let blobUriWork = uploadZipGetBlobSas conString zipProject
    let! appPlan = AppService.createAppServicePlan appName location rgName
    let! blobUri = blobUriWork
    let! functionApp = AppService.createFunctionApp appName appPlan rgName saName
    let! deployment = AppService.deployFunctionApp conString blobUri functionApp
    let! updatedFunctionApp =
        [ "Kita_AzureNative_ConnectionString", conString ]
        |> dict
        |> AppService.updateFunctionAppSettings functionApp

    printfn "Deployed app: %s" functionApp.Name

    printfn "Syncing triggers"
    do! functionApp.SyncTriggersAsync()

    do! AppService.listAllFunctions functionApp

    let proxyName = "Proxy"
    printfn "Adding key for proxy trigger: %s" proxyName
    let! funKey = functionApp.AddFunctionKeyAsync(proxyName, "devKey", null)

    printfn "Key -- %s | %s" funKey.Name funKey.Value

    printfn "\n\nAccess endpoints at\n"
    let appUri = sprintf "https://%s" functionApp.DefaultHostName
    printfn "%s/api/<endpoint>?code=%s" appUri funKey.Value

    printfn "\n\nAll done :3"
}
