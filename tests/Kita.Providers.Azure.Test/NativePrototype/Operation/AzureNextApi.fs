module AzureNativePrototype.AzureNextApi
// Management

// API Reference
// https://docs.microsoft.com/en-us/dotnet/api/overview/azure/?view=azure-dotnet-preview

open FSharp.Control.Tasks

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

        printfn "Using resource group: %s" rg.Id

        return rg
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

        printfn "Using storage account: %s" storageAccount.Id

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

        printfn "Using queue: %s" queue.Id

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

        printfn "Using blob container: %s" x.Name

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
            printfn "First key permissions: %A" key.Permissions

            return key

        else
            return failwithf "Couldn't get any keys for %s" saName

        }

    let formatKeyToConnectionString (key: StorageAccountKey) saName =
        $"DefaultEndpointsProtocol=https;AccountName={saName};AccountKey={key.Value}"
