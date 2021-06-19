namespace Kita.Providers.Azure.Resources.Provision

open System.IO
open FSharp.Control.Tasks
open Azure.Storage.Blobs

open Kita.Utility
open Kita.Resources.Utility
open Kita.Resources.Collections
open Kita.Providers.Azure.AzureNextApi
open Kita.Providers.Azure.Client
open Kita.Providers.Azure.Activation
open Kita.Providers.Azure.Resources.Utility

type AzureCloudMap<'K, 'V>
    (
        client: BlobContainerClient,
        serializer: Serializer<string>
    ) =

    let getBlob key = task {
        let blobName = serializer.Serialize key
        return client.GetBlobClient blobName
    }

    new (
        name: string,
        requestProvision,
        serializer
        ) =
        requestProvision <| noEnv (Storage.createMap name)

        let conString = getVariable AzureConnectionStringVarName
        let blobContainerClient = BlobContainerClient(conString, name)

        AzureCloudMap(blobContainerClient, serializer)
        
    new (name: string, requestProvision) =
        AzureCloudMap(
            name,
            requestProvision,
            Serializer.json
        )

    interface ICloudMap<'K, 'V> with
        member _.TryFind key = Async.AwaitTask <| task {
            let! blob = getBlob key

            try
                let! x = blob.DownloadAsync()
                let result = x.Value
                let reader = new StreamReader(result.Content)
                let! content = reader.ReadToEndAsync()

                return content |> serializer.Deserialize<'V> |> Some
            with
            | :? Azure.RequestFailedException ->
                let! blobExists = blob.ExistsAsync()
                if blobExists.Value then
                    // TODO is this the correct way to passthrough the exception?
                    return failwithf
                        "Failed to retrieve blob, but it exists: %s" 
                        (serializer.Serialize key)
                else
                    return None
        }

        member _.Set (key, value) = Async.AwaitTask <| task {
            let! blob = getBlob key

            let content =
                value
                |> serializer.Serialize
                |> System.Text.Encoding.UTF8.GetBytes
                |> fun s -> new MemoryStream(s)

            let! res = blob.UploadAsync(content, true)

            return ()
        }
