namespace Kita.Providers.Azure.AzureNextApi
// Management

// API Reference
// https://docs.microsoft.com/en-us/dotnet/api/overview/azure/?view=azure-dotnet-preview

open FSharp.Control.Tasks
open Kita.Providers.Azure.Utility.LocalLog

[<AutoOpen>]
module Utility =
    open System.Threading.Tasks

    let rValue (work: Task<Azure.Response<'T>>) = task {
        let! response = work
        return response.Value
    }

        
module Credential =
    open Azure.Identity
    open System
    
    let credential () =
        DefaultAzureCredential()
            
    let subId () =
        let subId =
            Environment.GetEnvironmentVariable
                "AZURE_SUBSCRIPTION_ID"

        if subId = null then
            failwith "Missing AZURE_SUBSCRIPTION_ID in env"

        subId


module Resources = 
    open Azure.ResourceManager.Resources
    open Azure.ResourceManager.Resources.Models

    open Credential

    let resourceClient =
        ResourcesManagementClient(subId(), credential())

    let createResourceGroup
        rgName
        location
        = task {
        let! rawResult =
            resourceClient.ResourceGroups.CreateOrUpdateAsync
                ( rgName
                , ResourceGroup(location)
                )
        let rg = rawResult.Value

        report "Using resource group: %s" rg.Id

        return rg
        }

    /// armParameters should be json string of JUST THE PARAMETERS.
    /// Do not include $schema, contentVersion, etc.; e.g.
    /// {
    ///   "param1" : {
    ///       "value" : "myvalue"
    ///    }
    /// }
    let createArmDeployment
        rgName
        deploymentName
        (armTemplate: string)
        (armParameters: string)
        = task {

        let deploymentProperties = new DeploymentProperties(DeploymentMode.Incremental)

        report "ARM Deployment %s template:\n%s"
            deploymentName
            armTemplate

        report "ARM Deployment %s parameters:\n%s"
            deploymentName
            armParameters

        deploymentProperties.Template <- armTemplate
        deploymentProperties.Parameters <- armParameters

        let! operation =
            // TODO
            // handle if a deployment already exists with given name
            // throws an error
            // could catch and if the error contains "is still active"
            // then get a handle for that deployment, cancel it, and retry
            // https://docs.microsoft.com/en-us/dotnet/api/azure.resourcemanager.resources.deploymentsoperations.cancel?view=azure-dotnet-preview

            resourceClient.Deployments.StartCreateOrUpdateAsync
                ( rgName
                , deploymentName
                , new Deployment (deploymentProperties)
                )

        let! rawResult = operation.WaitForCompletionAsync()

        let deployment = rawResult.Value

        return deployment

        }

module Storage =
    open Azure.ResourceManager.Storage
    open Azure.ResourceManager.Storage.Models

    open Credential

    let storageClient =
        StorageManagementClient(subId(), credential())
        
    // TODO
    // all the create* api's should be use* apis
    // since they may or may not be created depending on if they already exist
    let createStorageAccount
        appName
        location
        = task {

        let! rawResult =
            storageClient.StorageAccounts.StartCreateAsync
                ( appName // resource group name
                , appName // storage account name
                , new StorageAccountCreateParameters
                    ( new Sku(SkuName.StandardLRS)
                    , Kind.StorageV2
                    , location
                    )
                )

        let! storageAccount =
            rawResult.WaitForCompletionAsync().AsTask() |> rValue

        report "Using storage account: %s" storageAccount.Id

        return storageAccount

        }
        
    let createQueue
        queueName
        rgName
        saName
        = task {

        let! queue =
            storageClient.Queue.CreateAsync
                ( rgName
                , saName
                , queueName
                , new StorageQueue () )

            |> rValue

        report "Using queue: %s" queue.Id

        return ()

        }

    let createMap
        mapName
        rgName
        saName
        = task {

        let! blobContainer =
            storageClient.BlobContainers.CreateAsync
                ( rgName
                , saName
                , mapName
                , new BlobContainer() )
                |> rValue

        report "Using blob container as map: %s" blobContainer.Id

        return ()

        }

    let createBlobContainer
        containerName
        rgName
        saName
        = task {

        let! x =
            storageClient.BlobContainers.CreateAsync
                ( rgName
                , saName
                , containerName
                , new BlobContainer()
                )
            |> rValue

        report "Using blob container: %s" x.Name

        return x
        }

    let listKeys
        rgName
        saName
        = task {

        let! keysResponse =
            storageClient.StorageAccounts.ListKeysAsync
                ( rgName
                , saName
                )

        let keysResult = keysResponse.Value

        return keysResult.Keys

        }

    let getFirstKey
        rgName
        saName
        = task {

        let! keys = listKeys rgName saName

        if keys.Count > 0 then
            let key = keys.[0]
            report "First key permissions: %A" key.Permissions

            return key

        else
            return failwithf "Couldn't get any keys for %s" saName

        }

    let formatKeyToConnectionString (key: StorageAccountKey) saName =
        $"DefaultEndpointsProtocol=https;AccountName={saName};AccountKey={key.Value}"
