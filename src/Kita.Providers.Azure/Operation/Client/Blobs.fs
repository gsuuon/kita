namespace Kita.Providers.Azure.Client

open System.Threading.Tasks
open System.IO
open FSharp.Control.Tasks

open Azure.Storage.Blobs
open Azure.Storage.Blobs.Models
open Azure.Storage.Sas

open Kita.Providers.Azure.AzureNextApi.Utility

type BlobPermission = BlobSasPermissions

module Blobs =
    let generateSas
        (permission: BlobPermission)
        expiryTime
        (blobClient: BlobClient)
        = task {

        if not blobClient.CanGenerateSasUri then
            failwith "Blob client can't generate Sas uri :("
                // This shouldn't happen according to this gh comment
                // https://github.com/Azure/azure-sdk-for-net/issues/12414#issuecomment-757047459
                // If we hit this, then we'll need to create a new
                // blob client using this
                // https://docs.microsoft.com/en-us/dotnet/api/azure.storage.storagesharedkeycredential?view=azure-dotnet-preview

        return blobClient.GenerateSasUri(permission, expiryTime)
        }

    let generateSasTimeout
        (permission: BlobPermission)
        (timeoutHrs: float)
        (blobClient: BlobClient)
        =
        generateSas
        <| permission
        <| System.DateTimeOffset.UtcNow.AddHours(timeoutHrs)
        <| blobClient

    let generateSasInfinite
        (permission: BlobPermission)
        (blobClient: BlobClient)
        =
        generateSas
        <| permission
        <| System.DateTimeOffset.MaxValue
        <| blobClient
    
type Blobs(connectionString: string) =
    member val BlobServiceClient = BlobServiceClient(connectionString)

    member this.BlobContainerClient containerName = task {
        let containerClient =
            this.BlobServiceClient.GetBlobContainerClient(containerName)

        let! response = containerClient.CreateIfNotExistsAsync()
            // If container already exists, null is returned :'(
        if response <> null then
            let info = response.Value
            printfn "Created blob container: %s" containerName

        return containerClient
    }

    member this.BlobClient containerName blobName = task {
        let! containerClient = this.BlobContainerClient(containerName)
        return containerClient.GetBlobClient(blobName)
    }
