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
        = task {

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

    let deployFunctionApp
        blobUri
        (functionApp: IFunctionApp)
        = task {

        let! deployment =
            functionApp
                .Deploy()
                .WithPackageUri(blobUri)
                .ExecuteAsync()

        printfn "Deployed %s with %s" functionApp.Name blobUri

        return ()

        }
