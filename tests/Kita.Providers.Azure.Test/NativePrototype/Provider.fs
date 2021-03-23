namespace AzureNativePrototype

open System

open Azure.Identity
open Azure.ResourceManager.Resources
open Azure.ResourceManager.Resources.Models
open Azure.ResourceManager.Storage
open Azure.ResourceManager.Storage.Models

open FSharp.Control.Tasks

open Kita.Providers
open Kita.Utility

[<AutoOpen>]
module AzureNative =
    let defaultLocation = "eastus"

    let subId () =
        Environment.GetEnvironmentVariable
            "AZURE_SUBSCRIPTION_ID"

    let credential () =
        DefaultAzureCredential()

    let storageClient () =
        StorageManagementClient(subId(), credential())

    let resourceClient () =
        ResourcesManagementClient(subId(), credential())
            
    let createResourceGroup
        rgName
        location
        (client: StorageManagementClient)
        = task {
        let resourceClient = resourceClient()
        let! rawResult =
            resourceClient.ResourceGroups.CreateOrUpdateAsync
                ( rgName
                , ResourceGroup(location)
                )
        let rg = rawResult.Value

        printfn "Finished resource group: %s" rg.Id

        return rg
        }
        
    let createStorageAccount
        appName
        location
        (client: StorageManagementClient)
        = task {
        let! rawResult =
            client.StorageAccounts.StartCreateAsync
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

        return storageAccount

        }
        
    let createQueue
        queueName
        rgName
        saName
        (client: StorageManagementClient)
        = task {

        let! x =
            client.Queue.CreateAsync
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
        (client: StorageManagementClient)
        = task {
        let! keysResponse =
            client.StorageAccounts.ListKeysAsync
                ( rgName
                , saName
                )

        let keysResult = keysResponse.Value

        return keysResult.Keys

        }

    let getStorageConnectionString
        rgName
        saName
        (client: StorageManagementClient)
        = task {
            let! keys = listKeys rgName saName client

            if keys.Count > 0 then
                let key = keys.[0]
                printfn "First key permissions: %A" key.Permissions

                return $"DefaultEndpointsProtocol=https;AccountName={saName};AccountKey={key}"
            else
                return failwith "Couldn't get storage connection string"
        }

type AzureNative() =
    inherit Provider("Azure.Native")

    let connectionString = Waiter<string>()
    let mutable deployRequests = []
    let requestDeploy deploy =
        deployRequests <- deploy :: deployRequests

    member _.Deploy appName = task {
        let client = storageClient()
        let! rg =
            client |> createResourceGroup appName defaultLocation

        let! sa =
            client |> createStorageAccount appName defaultLocation

        let! conString = getStorageConnectionString rg.Name sa.Name client

        for deploy in deployRequests do
            do! deploy rg.Name sa.Name client

        connectionString.Set conString

        return ()
    }

    member _.RequestQueue (qName) =
        requestDeploy <| createQueue qName
