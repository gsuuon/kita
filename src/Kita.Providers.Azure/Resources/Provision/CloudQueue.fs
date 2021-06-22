namespace Kita.Providers.Azure.Resources.Provision

open Azure.Storage.Queues
open FSharp.Control.Tasks

open Kita.Core
open Kita.Utility
open Kita.Resources.Utility
open Kita.Resources.Collections
open Kita.Providers.Azure.Client
open Kita.Providers.Azure.Resources.Utility
open Kita.Providers.Azure.AzureNextApi
open Kita.Providers.Azure.Activation

open System.Text.Json

type AzureCloudQueue<'T>
    (
        client: Waiter<QueueClient>,
        serializer: Serializer<string>
    ) =

    new (
        name: string,
        requestProvision,
        serializer
        ) =
        requestProvision <| noEnv (Storage.createQueue name)

        let conString = getVariable AzureConnectionStringVarName
        let queueClient =
            produceWithEnv
            <| AzureConnectionStringVarName
            <| fun conString -> QueueClient(conString, name)

        AzureCloudQueue (queueClient, serializer)

    new (
        name: string,
        requestProvision
        ) =
        AzureCloudQueue
            ( name
            , requestProvision
            , Serializer.json
            )

    interface ICloudQueue<'T> with
        member _.Enqueue xs = async {
            do! Async.AwaitTask
                <| task {
                    let! client = client.GetTask
                    for x in xs do
                        let jsonX =  x
                        let! sendReceiptRes =
                            x
                            |> JsonSerializer.Serialize
                            |> client.SendMessageAsync

                        let msgId = sendReceiptRes.Value.MessageId

                        printfn "Sent msgId: %s" msgId

                        ignore sendReceiptRes

                        }

            return () }

        member _.Dequeue count = async {
            // TODO send read receipt to actually dequeue messages
            let! client = client.GetAsync
            let! rMsgs =
                client.ReceiveMessagesAsync count
                |> Async.AwaitTask

            return
                rMsgs.Value
                |> Array.map (fun x ->
                    client.DeleteMessage (x.MessageId, x.PopReceipt) |> ignore

                    x
                    )
                |> Array.map (fun x -> x.MessageText)
                |> Array.map JsonSerializer.Deserialize<'T>
                |> Array.toList
            }
