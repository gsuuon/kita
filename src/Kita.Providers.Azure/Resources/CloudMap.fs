namespace Kita.Providers.Azure.Resources

open System.IO
open FSharp.Control.Tasks
open Azure.Storage.Blobs

open Kita.Utility
open Kita.Resources.Utility
open Kita.Resources.Collections
open Kita.Providers.Azure.Client

type AzureCloudMap<'K, 'V>
    (
        blobContainerClient: Waiter<BlobContainerClient>,
        serializer: Serializer<string>
    ) =

    let getBlob key = task {
        let! client = blobContainerClient.GetAsync

        let blobName = serializer.Serialize key
        return client.GetBlobClient blobName
    }

    new (
        name: string,
        conStringWaiter: Waiter<string>,
        requestProvision,
        serializer
        ) =
        requestProvision()

        let blobContainerClient = Waiter<BlobContainerClient>()

        async {
            let! conString = conStringWaiter.GetAsync
            BlobContainerClient(conString, name) |> blobContainerClient.Set
        } |> Async.Start

        AzureCloudMap(blobContainerClient, serializer)
        
    new (name: string, conStringWaiter: Waiter<string>, requestProvision) =
        AzureCloudMap(
            name,
            conStringWaiter,
            requestProvision,
            Utility.Serializer.json
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
