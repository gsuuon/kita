module AzureNativePrototype.AzureNextApi

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
        Environment.GetEnvironmentVariable
            "AZURE_SUBSCRIPTION_ID"


// Management
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

        printfn "Created queue: %s" queue.Id

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

open Kita.Utility
/// Set connectionString to activate client APIs
let connectionString = Waiter<string>()
let afterConnectionString transform = 
    Waiter().Follow(connectionString , transform)
    
// Client
module Blobs =
    open System.Threading.Tasks
    open System.IO

    open Azure.Storage.Blobs
    open Azure.Storage.Blobs.Models
    open Azure.Storage.Sas

    // I'm not convinced a single static instance is ideal here
    // From my understanding, the underlying HttpClient is static and
    // cached anyways, so one instantiation per invocation only costs
    // the object instantiation and won't exhaust ports.
    
    // The price is that we can no longer handle multiple connectionStrings
    // in the same program. This means one project = one program.

    // Is that a good or bad thing?

    // Well -- it also makes the initialization of the connection string
    // per module a little weird, and obviously stateful.
    // I have to set the connection string into this api from the caller.
    // But - I only can/should do this once, yet all calling paths would
    // need to check if it's set and set it.

    // Feels like an anti-pattern in the end. What I'm really looking for
    // is a parameterized module -- aka just a normal class.

    let blobServiceClient =
        afterConnectionString
        <| fun conString -> BlobServiceClient(conString)

    let connect conString =
        connectionString.Set conString

    let blobContainerClient containerName = task {
        let! client = blobServiceClient.GetTask
        let containerClient = client.GetBlobContainerClient(containerName)
        let! info = containerClient.CreateIfNotExistsAsync() |> rValue
        return containerClient
    }

    let blobClient containerName blobName = task {
        let! containerClient = blobContainerClient(containerName)
        return containerClient.GetBlobClient(blobName)
    }

    type Permission = BlobSasPermissions

    let blobGenerateSas
        (permission: Permission)
        timeoutHrs
        (blobClient: BlobClient)
        = task {

        if not blobClient.CanGenerateSasUri then
            failwith "Blob client can't generate Sas uri :("
                // This shouldn't happen according to this gh comment
                // https://github.com/Azure/azure-sdk-for-net/issues/12414#issuecomment-757047459
                // If we hit this, then we'll need to create a new
                // blob client using this
                // https://docs.microsoft.com/en-us/dotnet/api/azure.storage.storagesharedkeycredential?view=azure-dotnet-preview
            
        let sasUri =
            blobClient.GenerateSasUri
                ( BlobSasBuilder
                    ( permission
                    , System.DateTimeOffset.Now.AddHours(timeoutHrs)
                    )
                )

        return sasUri
        }
