namespace PulumiPrototype.Test.Resources

open Kita.Resources.Collections
open Kita.Providers

open PulumiPrototype.Utility
open PulumiPrototype.Waiter

open Azure.Storage.Queues

type PulumiQueue<'T>() =
    inherit CloudQueue<'T>()
    let name = "defaultqname"

    let client = Waiter<QueueClient>()

    member _.Deploy(provider: PulumiAzure) =
        provider.AddDependent
        <| async {
            let! connectionString = provider.WaitConnectionString()
            client.Set <| QueueClient(connectionString, name)
        }

        provider.AddQueue name

    member _.Enqueue item =
        async {
            let! client = client.Get()
            let res = client.SendMessage item
            let sendReceipt = res.Value
            printfn "Send receipt: %A" sendReceipt
        }

    member _.Dequeue count =
        async {
            let! client = client.Get()
            let! messages =
                client.ReceiveMessagesAsync count
                |> Async.AwaitTask

            return messages.Value |> Array.toList
        }
