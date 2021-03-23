namespace FarmerPrototype

open Kita.Resources.Collections
open Kita.Providers
open Kita.Utility

open Azure.Storage.Queues

type FarmerQueue<'T>(?name: string) =
    inherit CloudQueue<'T>()
    let name = defaultArg name "defaultfarmerqname"

    let client = Waiter<QueueClient>()

    member _.Attach(provider: FarmerAzure) =
        provider.OnConnect.Add
        <| fun connectionString ->
            QueueClient(connectionString, name) |> client.Set 

        // I don't want to do the provisioning work here because
        // I don't want the provisioning code as a dependency here
        // Does that make sense? Does it become a dependency anyways?
        // It's transient, but does that have less impact than direct?

        // I think this also makes it easier to have different 'frontend'
        // classes for the same backend resource

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
