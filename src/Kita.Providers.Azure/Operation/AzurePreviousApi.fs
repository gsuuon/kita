namespace Kita.Providers.Azure.AzurePreviousApi

// API Reference
// https://docs.microsoft.com/en-us/dotnet/api/overview/azure/?view=azure-dotnet

open System
open System.Threading.Tasks
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
        let! existingFunctionApp =
            azure.AppServices.FunctionApps
                .GetByResourceGroupAsync(rgName, appName)
            // NOTE
            // Will there be a problem here if we change saName?
            // We get existing based on only rgName and appName

        if existingFunctionApp <> null then
            printfn "Using existing functionApp: %s"
                        existingFunctionApp.Name

            return existingFunctionApp
        else
            let! storageAccount = azure.StorageAccounts.GetByResourceGroupAsync(rgName, saName)

            let settings =
                [ "SCM_DO_BUILD_DURING_DEPLOYMENT", "false"
                  "FUNCTIONS_EXTENSION_VERSION", "~3"
                  "FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated"
                ] |> dict

            let! functionApp =
                azure.AppServices.FunctionApps
                    .Define(appName)
                    .WithExistingAppServicePlan(appPlan)
                    .WithExistingResourceGroup(rgName)
                    .WithExistingStorageAccount(storageAccount)
                    .WithAppSettings(settings)
                    .CreateAsync()

            printfn "Created functionApp: %s on storage: %s"
                functionApp.Name
                functionApp.StorageAccount.Name

            return functionApp
        }

    let listAllFunctions (functionApp: IFunctionApp) = task {
        let! functions = functionApp.ListFunctionsAsync()

        let rec listAllFunctions pagedCollection = task {
            if pagedCollection = null then
                return ()
            else
                for (func: IFunctionEnvelope) in pagedCollection do
                    printfn "Function: %s | %s"
                        func.Name
                        func.Type

                let! next = functions.GetNextPageAsync()
                return! listAllFunctions next
        }

        return! listAllFunctions functions
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

    let updateFunctionAppSettings
        (functionApp: IFunctionApp)
        settings
        = task {

        printfn "Updating app settings:\n%s"
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
            // This supposedly triggers a restart
            functionApp
                .Update()
                .WithAppSettings(settings)
                .ApplyAsync()

        printfn "Updated settings"

        return functionApp

        }

    let deployFunctionApp
        kitaConnectionString
        blobUri
        (functionApp: IFunctionApp)
        = task {

        printfn "Deploying blob.."

        let! update =
        // This fails a lot?
        // ARM-MSDeploy Deploy Failed: 'System.Threading.ThreadAbortException: Thread was being aborted.
        // at Microsoft.Web.Deployment.NativeMethods.SetFileInformationByHandle(SafeFileHandle hFile, FILE_INFO_BY_HANDLE_CLASS fileInformationClass, FILE_BASIC_INFO&amp; baseInfo, Int32 nSize)
        // Apparently it's because something else is causing the service
        // to restart before this, and this get cut off
            functionApp
                .Update()
                .WithAppSettings(
                    [ "WEBSITE_RUN_FROM_PACKAGE", blobUri ]
                    |> dict
                )
                .ApplyAsync()

        printfn "Deployed %s" functionApp.Name

        return update

        }

module SqlServer =
    open Microsoft.Azure.Management.Sql.Fluent

    let rec generateStringBasedOnGuid (start: string) desiredLength =
        let guid = System.Guid.NewGuid()

        let missingLength = desiredLength - start.Length

        let generatedString =
            guid.ToString()
            |> fun s -> s.Replace("-", "")
            |> Seq.truncate missingLength
            |> Seq.map string
            |> String.concat ""

        let result = start + generatedString

        if desiredLength > result.Length then
            generateStringBasedOnGuid result desiredLength
        else
            result

    type UserAuth =
        { username : string
          password : string }

    let createSqlServer
        serverName
        (location: string)
        (rgName: string)
        (databases: string list)
        (userAuth : UserAuth)
        = task {
            // check if server exists
            // do i need to?

            let! servicePrincipal = azure.AccessManagement.ServicePrincipals.GetByIdAsync (credential.ClientId)
            let credentialName = servicePrincipal.Name

            let! sqlServer =
                azure.SqlServers
                    .Define(serverName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(rgName)
                    .WithAdministratorLogin(userAuth.username)
                    .WithAdministratorPassword(userAuth.password)
                    .WithActiveDirectoryAdministrator(credentialName, credential.ClientId)
                    .CreateAsync()

            let! databasesCreated =
                databases
                |> List.map
                    (fun db ->
                        sqlServer.Databases
                            .Define(db)
                            .CreateAsync())
                |> List.toArray
                |> Task.WhenAll

            return databasesCreated
        }

    let createSqlServerRngUser
        serverName
        (location: string)
        (rgName: string)
        (databases: string list)
        =
        createSqlServer
            serverName
            location
            rgName
            databases
            { username = generateStringBasedOnGuid "u" 20
              password = generateStringBasedOnGuid "" 128 }

