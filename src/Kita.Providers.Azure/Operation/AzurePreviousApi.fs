namespace Kita.Providers.Azure.AzurePreviousApi

// API Reference
// https://docs.microsoft.com/en-us/dotnet/api/overview/azure/?view=azure-dotnet

open System
open FSharp.Control.Tasks

open Microsoft.Azure.Management.AppService.Fluent
open Microsoft.Azure.Management
open Microsoft.Azure.Management.Fluent
open Microsoft.Azure.Management.Storage.Fluent
open Microsoft.Azure.Management.Storage.Fluent.Models
open Microsoft.Azure.Management.Graph.RBAC.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent.Core
open Microsoft.Azure.Management.ResourceManager.Fluent.Authentication

open Kita.Providers.Azure

[<RequireQualifiedAccess>]
[<AutoOpen>]
module Credential =
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

    let azure =
        Azure
            .Configure()
            .Authenticate(credential)
            .WithSubscription(AzureNextApi.Credential.subId())

    printfn "Using subscription: %s" azure.SubscriptionId

module AppService = 
    let createFunctionApp
        appName
        (appPlan: IAppServicePlan)
        (rgName: string)
        (saName: string)
        = task {

        let! storageAccount = azure.StorageAccounts.GetByResourceGroupAsync(rgName, saName)

        let! functionApp =
            azure.AppServices.FunctionApps
                .Define(appName)
                .WithExistingAppServicePlan(appPlan)
                .WithExistingResourceGroup(rgName)
                .WithExistingStorageAccount(storageAccount)
                .CreateAsync()

        printfn "Created functionApp: %s on storage: %s"
            functionApp.Name
            functionApp.StorageAccount.Name

        return functionApp

        }

    let createAppServicePlan
        appServicePlanName
        (location: string)
        (rgName: string)
        = task {
        let! existing =
            azure.AppServices.AppServicePlans.GetByResourceGroupAsync
                (rgName, appServicePlanName)

        if existing <> null then
            printfn "Using existing app service plan: %s" existing.Name
            return existing

        else
            let! appServicePlan =
                azure.AppServices.AppServicePlans
                    .Define(appServicePlanName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(rgName)
                    .WithConsumptionPricingTier()
                    .CreateAsync()

            printfn "Using app service plan: %s" appServicePlan.Name

            return appServicePlan
        }

    let deployFunctionApp
        kitaConnectionString
        blobUri
        (functionApp: IFunctionApp)
        = task {

        let settings =
            [ "WEBSITE_RUN_FROM_PACKAGE", "0"
                // WEBSITE_RUN_FROM_PACKAGE = 1 fails
                // During deployment, there's an attempt to
                // create a directory at wwwroot which fails because
                // it becomes read-only. Works fine if I use
                // the cli to upload the zip. Doesn't work if
                // I use the package uri here. Not sure what's up.
                // Check Notes.md for actual error.
              "SCM_DO_BUILD_DURING_DEPLOYMENT", "false"
              "FUNCTIONS_EXTENSION_VERSION", "~3"
              "FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated"
              "Kita_AzureNative_ConnectionString", kitaConnectionString
            ] |> dict

        printfn "Using app settings:\n%s"
                (settings
                |> Seq.map
                    (fun (KeyValue(k,v)) ->
#if !DEBUG
                        if k.Contains "ConnectionString" then
                            sprintf "%A = <connection string>" k
                        else
#endif
                            sprintf "%A = %A" k v
                    )
                |> String.concat "\n")

        let! functionApp =
            functionApp
                .Update()
                .WithAppSettings(settings)
                .ApplyAsync()

        let! deployment =
        // This fails a lot?
        // ARM-MSDeploy Deploy Failed: 'System.Threading.ThreadAbortException: Thread was being aborted.
        // at Microsoft.Web.Deployment.NativeMethods.SetFileInformationByHandle(SafeFileHandle hFile, FILE_INFO_BY_HANDLE_CLASS fileInformationClass, FILE_BASIC_INFO&amp; baseInfo, Int32 nSize)
            functionApp
                .Deploy()
                .WithPackageUri(blobUri)
                .WithExistingDeploymentsDeleted(true)
                .ExecuteAsync()

        printfn "Deployed %s with %s" functionApp.Name blobUri

        return deployment

        }
