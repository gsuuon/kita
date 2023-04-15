module Kita.Providers.Azure.Operations

open FSharp.Control.Tasks
open System.Threading.Tasks

open Kita.Providers.Azure.Client
open Kita.Providers.Azure.AzurePreviousApi
open Kita.Providers.Azure.AzureNextApi
open Kita.Providers.Azure.Activation
open Kita.Providers.Azure.Utility.LocalLog
open Microsoft.Azure.Management.AppService.Fluent

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
        Blobs.generateSasInfinite
            BlobPermission.Read
            blobClient

    return blobUri.AbsoluteUri
}

let provision
    appName
    location
    cloudTasks
    (executeProvisionRequests:
        string * string -> Task<(string * string) seq>)
    (executeProvisionRequestsAfterApp:
        IFunctionApp -> Task<(string * string) seq>)
    = task {
    report "Provisioning app"

    let! (conString, rgName, saName) =
        provisionCloudGroup appName location

    let executeProvisionsWork =
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


    let blobUriWork = uploadZipGetBlobSas conString zipProject
    let! appPlan = AppService.createAppServicePlan appName location rgName
    let! blobUri = blobUriWork
    let! functionApp = AppService.createFunctionApp appName appPlan rgName saName

    let executeProvisionsAfterAppWork =
        executeProvisionRequestsAfterApp functionApp

    report "Waiting for provision requests to finish"
    let! envVarsFromProvisions = executeProvisionsWork
    let! envVarsFromProvisionsAfterApp = executeProvisionsAfterAppWork
    report "Finished provision requests"

    report "Updating app settings"
    let! updatedFunctionApp =
        seq {
            yield! envVarsFromProvisions
            yield! envVarsFromProvisionsAfterApp
            yield AzureConnectionStringVarName, conString
            yield "WEBSITE_RUN_FROM_PACKAGE", blobUri 
        }
        |> dict
        |> AppService.updateFunctionAppSettings functionApp
    report "Finished updating settings"

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
    let! funKey = functionApp.AddFunctionKeyAsync("Proxy", "devKey", null)

    reportSecret "Proxy trigger key -- %s | %s" funKey.Name funKey.Value
    reportSecret "Access endpoints at\nhttps://%s/api/<endpoint>?code=%s"
        functionApp.DefaultHostName funKey.Value

    report "To stream logs you may need to interact with the Azure portal. Hitting refresh on the app page seems to work. Run:\naz webapp log tail --name %s --resource-group %s" functionApp.Name rgName
        // there's a sentinel file that needs to be touched for log streaming to happen
        // could do this via kudu api or something here

    printfn "\n\nAll done :3"
}
