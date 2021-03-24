module AzureNativePrototype.AzureNextApi

// API Reference
// https://docs.microsoft.com/en-us/dotnet/api/overview/azure/?view=azure-dotnet-preview

open FSharp.Control.Tasks

module Credential =
    open Azure.Identity
    open System
    
    let credential () =
        DefaultAzureCredential()
            
    let subId () =
        Environment.GetEnvironmentVariable
            "AZURE_SUBSCRIPTION_ID"


module Blobs =
    open Azure.Storage.Blobs

    let blobClient () =
        BlobClient


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

        printfn "Finished resource group: %s" rg.Id

        return rg
        }


module Storage =
    open Azure.ResourceManager.Storage
    open Azure.ResourceManager.Storage.Models

    open Credential

    let storageClient =
        StorageManagementClient(subId(), credential())
        
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

        let! storageAccountRes = rawResult.WaitForCompletionAsync()
        let storageAccount = storageAccountRes.Value

        printfn "Finished storage account: %s" storageAccount.Id

        return storageAccount

        }
        
    let createQueue
        queueName
        rgName
        saName
        = task {

        let x = new StorageQueue()
        let! x =
            storageClient.Queue.CreateAsync
                ( rgName
                , saName
                , queueName
                , new StorageQueue () )

        let queue = x.Value
        printfn "Created queue: %s" queue.Id

        return ()

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

    let getStorageConnectionString
        rgName
        saName
        = task {
            let! keys = listKeys rgName saName

            if keys.Count > 0 then
                let key = keys.[0]
                printfn "First key permissions: %A" key.Permissions

                let conString = $"DefaultEndpointsProtocol=https;AccountName={saName};AccountKey={key.Value}"
                printfn "Connection string: %s" conString

                return conString
            else
                return failwith "Couldn't get storage connection string"
        }
