namespace Kita.Providers.Azure.Resources

open Azure.Storage.Queues
open FSharp.Control.Tasks

open Kita.Core
open Kita.Utility
open Kita.Resources.Utility
open Kita.Resources.Collections
open Kita.Providers.Azure.Client

open System.Text.Json

type AzureCloudQueue<'T>
    (
        queueClient: Waiter<QueueClient>,
        serializer: Serializer<string>
    ) =
    new (
        name,
        conStringWaiter: Waiter<string>,
        requestProvision,
        serializer
        ) =
        requestProvision()

        let queueClient = Waiter<QueueClient>()

        async {
            // Using waiter means I could attach after deploy
            // Since the event is only fired when connection happens
            let! conString = conStringWaiter.GetAsync
            QueueClient(conString, name) |> queueClient.Set
        } |> Async.Start

        AzureCloudQueue (queueClient, serializer)

    new (
        name,
        conStringWaiter: Waiter<string>,
        requestProvision
        ) =
        AzureCloudQueue
            ( name
            , conStringWaiter
            , requestProvision
            , Utility.Serializer.json
            )

    interface ICloudQueue<'T> with
        member _.Enqueue xs = async {
            let! client = queueClient.GetAsync

            do! Async.AwaitTask
                <| task {
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
            let! client = queueClient.GetAsync
            let! rMsgs =
                client.ReceiveMessagesAsync count
                |> Async.AwaitTask

            return
                rMsgs.Value
                |> Array.map (fun x -> x.MessageText)
                |> Array.map JsonSerializer.Deserialize<'T>
                |> Array.toList
            }
