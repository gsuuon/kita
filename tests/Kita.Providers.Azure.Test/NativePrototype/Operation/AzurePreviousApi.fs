module AzureNativePrototype.AzurePreviousApi

// API Reference
// https://docs.microsoft.com/en-us/dotnet/api/overview/azure/?view=azure-dotnet

open System

open Microsoft.Azure.Management.AppService.Fluent
open Microsoft.Azure.Management
open Microsoft.Azure.Management.Fluent
open Microsoft.Azure.Management.Storage.Fluent
open Microsoft.Azure.Management.Storage.Fluent.Models
open Microsoft.Azure.Management.Graph.RBAC.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent.Core
open Microsoft.Azure.Management.ResourceManager.Fluent.Authentication

open FSharp.Control.Tasks

let credential =
    let x = new AzureCredentialsFactory()
    x.FromServicePrincipal
        ( Environment.GetEnvironmentVariable
            "AZURE_CLIENT_ID"
        , Environment.GetEnvironmentVariable
            "AZURE_CLIENT_SECRET"
        , Environment.GetEnvironmentVariable
            "AZURE_TENANT_ID"
        , AzureEnvironment.AzureGlobalCloud
        )

let createAzure () =
    let azure =
        Azure
            .Configure()
            .Authenticate(credential)
            .WithSubscription(AzureNextApi.Credential.subId())

    printfn "Using subscription: %s" azure.SubscriptionId
    azure

let createFunctionApp
    appName
    (appPlan: IAppServicePlan)
    (rgName: string)
    (azure: IAzure)
    = task {
    // TODO set app service plan?
    // app service plan should be
    //   tier = "Dynamic"
    //   Name = "Y1"
    let! functionApp =
        azure.AppServices.FunctionApps
            .Define(appName)
            .WithExistingAppServicePlan(appPlan)
            .WithExistingResourceGroup(rgName)
                // TODO
                // create a resourceGroup
                // set plan for resource group to be consumption
            .CreateAsync()

    printfn "Created functionApp: %s on storage: %s"
        functionApp.Name
        functionApp.StorageAccount.Name

    return functionApp
}

let createAppServicePlan
    appServicePlanName
    (rgName: string)
    (azure: IAzure)
    = task {
        let! appServicePlan =
            azure.AppServices.AppServicePlans
                .Define(appServicePlanName)
                .WithRegion(Region.USEast)
                .WithExistingResourceGroup(rgName)
                .WithConsumptionPricingTier()
                .CreateAsync()

        printfn "Using app service plan %s" appServicePlan.Name
        return appServicePlan
    }

let createBlobContainer containerName rgName saName (azure: IAzure) = task {
    let! blobContainer = 
        azure.StorageAccounts.Manager.BlobContainers
            .DefineContainer(containerName)
            .WithExistingBlobService(rgName, saName)
            .WithPublicAccess(PublicAccess.None)
            .CreateAsync()
    
    printfn "Using blob container: %s" blobContainer.Id
    return blobContainer
}

let deployFunctionApp (fApp: IFunctionApp) (azure: IAzure) = task {
    // I may need to just upload a blob, there doesn't seem to be an sdk way for zip deployment
    // I can curl and PUT to the endpoint
    // https://docs.microsoft.com/en-us/azure/azure-functions/deployment-zip-push#rest
    // or with az cli
    // https://docs.microsoft.com/en-us/azure/azure-functions/deployment-zip-push#cli
    // blob deploy generates untracked garbage. I can stick it all into a specific blob/storage account?
    // potentially useful as you could restore to previous state
    // but that could actually cause your application to be in an inconsistent state with other app components
    // I could use the same blob, overwrite it here, then use it so that the garbage doesn't accumulate?
    let blobUrl = "pretend this is the blob url"
    let! deployment =
        fApp
            .Deploy()
            .WithPackageUri(blobUrl)
            .WithExistingDeploymentsDeleted(false)
            .ExecuteAsync()

    if deployment.Complete then
        printfn "Finished deployment: %s" deployment.Key

    return deployment
}
