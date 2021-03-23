module AzureNativePrototype.Resource

open FSharp.Control.Tasks
open Azure.Storage.Queues

open Kita.Resources.Collections
open Kita.Utility

open AzureNativePrototype


type Queue<'T>(?name: string) =
    inherit CloudQueue<'T>()
    let name = defaultArg name "defaultqname"
        // FIXME generate name based on
        // position + type + ownership hierarchy

    let queueClient = Waiter<QueueClient>()
    member _.Attach(provider: AzureNative) =
        provider.RequestQueue(name)

    member this.Deploy(x) = this.Attach(x)
        // TODO rename this to Attach

    member _.Enqueue item = async {
        let! client = queueClient.Get()
        let! sendReceiptRes =
            client.SendMessageAsync item |> Async.AwaitTask

        printfn "Sent msgId: %s" sendReceiptRes.Value.MessageId

        return () }

    member _.Enqueue (xs: string seq) = async {
        let! client = queueClient.Get()

        do! Async.AwaitTask
            <| task {
                for x in xs do
                    let! sendReceiptRes = client.SendMessageAsync x
                    printfn "Sent msgId: %s" sendReceiptRes.Value.MessageId

                    ignore sendReceiptRes }

        return () }

    member _.Dequeue count = async {
        let! client = queueClient.Get()

        let! rMsgs = client.ReceiveMessagesAsync(count) |> Async.AwaitTask

        return
            rMsgs.Value
            |> Array.map (fun x -> x.Body)
            |> Array.toList }
