namespace Kita.Providers.Azure.AzurePreviousApi

// API Reference
// https://docs.microsoft.com/en-us/dotnet/api/overview/azure/?view=azure-dotnet

open System
open System.Threading.Tasks
open FSharp.Control.Tasks

open Microsoft.Azure.Management
open Microsoft.Azure.Management.Fluent
open Microsoft.Azure.Management.AppService.Fluent
open Microsoft.Azure.Management.Graph.RBAC.Fluent
open Microsoft.Azure.Management.Sql.Fluent.Models
open Microsoft.Azure.Management.Storage.Fluent
open Microsoft.Azure.Management.Storage.Fluent.Models
open Microsoft.Azure.Management.ResourceManager.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent.Core
open Microsoft.Azure.Management.ResourceManager.Fluent.Authentication

open Kita.Providers.Azure
open Kita.Providers.Azure.Utility.LocalLog

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

    report "Using subscription: %s" azure.SubscriptionId

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
            report "Using existing functionApp: %s"
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
                    .WithSystemAssignedManagedServiceIdentity()
                    .CreateAsync()

            report "Created functionApp: %s on storage: %s"
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
                    report "Function: %s | %s"
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
            report "Using existing app service plan: %s" existing.Name
            return existing

        else
            let! appServicePlan =
                azure.AppServices.AppServicePlans
                    .Define(appServicePlanName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(rgName)
                    .WithConsumptionPricingTier()
                    .CreateAsync()

            report "Using app service plan: %s" appServicePlan.Name

            return appServicePlan
        }

    let updateFunctionAppSettings
        (functionApp: IFunctionApp)
        settings
        = task {

        report "Updating app settings:\n%s"
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

        report "Updated settings"

        return functionApp

        }

    let deployFunctionApp
        kitaConnectionString
        blobUri
        (functionApp: IFunctionApp)
        = task {

        report "Deploying blob.."

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

        report "Deployed %s" functionApp.Name

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

    let getUrl =
        let client = new System.Net.Http.HttpClient()

        fun (url: string) ->
            task {
                let! result = client.GetStringAsync url
                return result.Trim()
            }

    // Gets public ip from api's, if first 3 parts match then return range
    let getPublicIpRange () =
        task {
            let! ips =
                [ getUrl "https://checkip.amazonaws.com"
                  getUrl "https://ipinfo.io/ip"
                  getUrl "https://api.ipify.org"
                ] |> Task.WhenAll

            let ipsMatchFirst3Parts =
                let first3Parts (ipString: string) =
                    ipString.Split(".") |> Seq.take 3 |> String.concat "."

                let ipFirst3Parts = ips.[0] |> first3Parts

                ips
                |> Seq.forall (fun x -> x |> first3Parts = ipFirst3Parts )

            if not ipsMatchFirst3Parts then
                failwithf "Ip sources don't agree on public ip first 3 parts: %s" (String.concat ", " ips)

            let sortedIps = ips |> Array.sort

            let modifyLast fn (ip: string) =
                let parts = ip.Split(".")

                seq {
                    yield! parts |> Seq.take (parts.Length - 1)
                    yield parts |> Seq.last |> int |> fn |> string
                } |> String.concat "."

            let lowest =
                sortedIps
                |> Array.head
                |> modifyLast ((fun x -> x - 10) >> max 0)

            let highest =
                sortedIps
                |> Array.last
                |> modifyLast ((+) 10 >> min 255 )

            return lowest, highest
        }

    let createSqlServer
        serverName
        (location: string)
        (rgName: string)
        (databases: string list)
        (userAuth : UserAuth)
        = task {
            // check if server exists
            // do i need to?

            report "Attempting create sql server"
            report "Getting ad user name"

            let! subscription = azure.Subscriptions.GetByIdAsync(azure.SubscriptionId)
            let credentialName = subscription.DisplayName
                // We're assuming the subscription name is also valid as an AD managed identity
            report "Got credential name: %s" credentialName
            report "Checking existing"

            let publicIpRangeWork = getPublicIpRange()

            let! existingServer =
                azure.SqlServers.GetByResourceGroupAsync(rgName, serverName)

            let provisionFirewallRuleName = "kita_provision"

            if existingServer <> null then
                report "Found existing SqlServer, using"
                do! existingServer.FirewallRules.DeleteAsync(provisionFirewallRuleName)
                let! (lowIp, highIp) = publicIpRangeWork
                report "Allowing ips: [%s; %s]" lowIp highIp
                let! rule =
                    existingServer.FirewallRules
                        .Define(provisionFirewallRuleName)
                        .WithIPAddressRange(lowIp, highIp)
                        .CreateAsync()

                return existingServer
            else
                report "Requesting SqlServer: %s" serverName
                reportSecret
                    "Using (record this, not recoverable) auth\n\tuser: %s\n\tpassword: %s"
                        userAuth.username
                        userAuth.password

                let! (lowIp, highIp) = publicIpRangeWork
                report "Allowing ips: [%s; %s]" lowIp highIp

                let! sqlServer =
                    azure.SqlServers
                        .Define(serverName)
                        .WithRegion(location)
                        .WithExistingResourceGroup(rgName)
                        .WithAdministratorLogin(userAuth.username)
                        .WithAdministratorPassword(userAuth.password)
                        .WithActiveDirectoryAdministrator(credentialName, credential.ClientId)
                        .WithNewFirewallRule(lowIp, highIp, provisionFirewallRuleName)
                        .CreateAsync()

                report "Created SqlServer %s" sqlServer.FullyQualifiedDomainName
                report "SqlServer using user credential: %s" credentialName

                return sqlServer
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
            { username = generateStringBasedOnGuid "u" 20 // guarantee starts with letter
              password = generateStringBasedOnGuid "1!Aa" 128 // guarantee meets validation reqs
            }

    let createSqlDatabase
        databaseName
        (sqlServer: ISqlServer)
        = task {
            report "Creating database %s on server %s" databaseName sqlServer.Name

            let! existingDatabase =
                sqlServer.Databases.GetAsync(databaseName)

            if existingDatabase <> null then
                report "Using existing database"

                return existingDatabase
            else
                let! database =
                    sqlServer.Databases
                        .Define(databaseName)
                        .WithServiceObjective(ServiceObjectiveName.Free)
                        .CreateAsync()

                report "Created database"

                return database
        }

module ActiveDirectory =
    open System

    let createADGroup
        groupName
        = task {
        let! group =
            azure.AccessManagement.ActiveDirectoryGroups
                .Define(groupName)
                .WithEmailAlias(groupName)
                .CreateAsync()

        return group

        }

    let addMemberToADGroup
        (adGroup: IActiveDirectoryGroup)
        (memberId: string)
        = task {
        let! updatedGroup =
            adGroup
                .Update()
                .WithMember(memberId)
                .ApplyAsync()

        return updatedGroup
        }

    open System
    
    /// https://github.com/MicrosoftDocs/sql-docs/issues/2323
    /// Can't execute create user without giving sql server a
    /// managed system identity with Directory Reader role.
    /// To give that role, need a user with Global Admin or 
    /// Privileged Role Administrator.
    /// More in Notes.md
    let buildCreateUserSqlWorkaround
        (objectType: string)
        (objectName: string)
        (objectId: string)
        =
        report "Building create user sql"
        report "Object id: %s" objectId
        let guid = Guid.Parse(objectId)
        report "Guid: %A" guid

        let idBytes = guid.ToByteArray()
        let idBytesString =
            BitConverter.ToString(idBytes).Replace("-","")
        let sid = $"0x{idBytesString}"
        report "Sid: %A" sid

        $"""
create user {objectName} with SID={sid}, TYPE={objectType};
alter role db_datareader add member {objectName};
alter role db_datawriter add member {objectName};
        """

