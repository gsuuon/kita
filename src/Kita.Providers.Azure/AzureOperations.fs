module Kita.Providers.Azure.Operations

open FSharp.Control.Tasks
open System.Threading.Tasks

open Kita.Providers.Azure.Client
open Kita.Providers.Azure.AzurePreviousApi
open Kita.Providers.Azure.AzureNextApi
open Kita.Providers.Azure.Activation
open Kita.Providers.Azure.Utility.LocalLog

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
        string * string -> Task<(string * string) seq>)
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

    let! environmentVariablesFromResourceProvisions = execProvisionRequestsWork
    printfn "Finished provisioning resources"

    let blobUriWork = uploadZipGetBlobSas conString zipProject
    let! appPlan = AppService.createAppServicePlan appName location rgName
    let! blobUri = blobUriWork
    let! functionApp = AppService.createFunctionApp appName appPlan rgName saName

    let! updatedFunctionApp =
        seq {
            yield! environmentVariablesFromResourceProvisions
            yield AzureConnectionStringVarName, conString
            yield "WEBSITE_RUN_FROM_PACKAGE", blobUri 
        }
        |> dict
        |> AppService.updateFunctionAppSettings functionApp

    printfn "Deployed app: %s" functionApp.Name

    try
        printfn "Syncing triggers"
        do! functionApp.SyncTriggersAsync()
        printfn "Synced triggers"
    with
    | x ->
        let asString = x.ToString()
        let explain =
            if asString.Contains "BadRequest" then
                "BadRequest error could mean Azure services are having issues, triggers may still be correct (and stale below) - check the portal"
            else
                ""

        printfn "Failed to sync triggers:\n%s\n%s" explain asString

    do! AppService.listAllFunctions functionApp

    let proxyName = "Proxy"
    report "Adding key for proxy trigger: %s" proxyName
    let! funKey = functionApp.AddFunctionKeyAsync(proxyName, "devKey", null)

    reportSecret "Key -- %s | %s" funKey.Name funKey.Value

    reportSecret "\n\nAccess endpoints at\n"
    let appUri = sprintf "https://%s" functionApp.DefaultHostName
    reportSecret "%s/api/<endpoint>?code=%s" appUri funKey.Value

    report "To connect to streaming logs with az cli:\n\taz webapp log tail --name %s --resource-group %s" functionApp.Name rgName

    printfn "\n\nAll done :3"
}
