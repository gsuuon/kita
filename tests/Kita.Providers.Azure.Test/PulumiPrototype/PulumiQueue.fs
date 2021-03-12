namespace PulumiPrototype.Test.Resources

open Kita.Resources.Collections
open Kita.Providers
open PulumiPrototype.Utility

open Azure.Storage.Queues

type PulumiQueue<'T>() =
    inherit CloudQueue<'T>()
    let name = "defaultqname"
    // got invalid resource name?
    // with uppercase? or because it hasn't been provisioned?

    let _qClient : QueueClient option ref = ref None
    let qClient = waitUntilValue 100 _qClient

    member _.Deploy(provider: PulumiAzure) =
        provider.AddDependent <| async {
            let! connectionString = provider.ConnectionString
            _qClient := Some <| QueueClient(connectionString, name)
        }

        provider.AddQueue name

    member _.Enqueue item =
        async {
            let! client = qClient
            let res = client.SendMessage item
            let sendReceipt = res.Value
            printfn "Send receipt: %A" sendReceipt
        }

    member _.Dequeue count =
        async {
            let! client = qClient
            let! messages =
                client.ReceiveMessagesAsync count
                |> Async.AwaitTask

            return messages.Value |> Array.toList
        }
