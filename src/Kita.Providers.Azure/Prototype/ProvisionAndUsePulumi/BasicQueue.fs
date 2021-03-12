module ProvisionPulumiAuto.BasicQueue

open Pulumi.AzureNative.Storage
open Pulumi.AzureNative.Resources

open Pulumi.FSharp
open Pulumi.FSharp.Output
open Pulumi.FSharp.AzureNative.Resources
open Pulumi.FSharp.AzureNative.Storage
open Pulumi.FSharp.AzureNative.Storage.Inputs

open Pulumi.Automation
open ProvisionPulumiAuto.Helpers

open Azure.Storage.Queues

let program = PulumiFn.Create( fun () ->
    let rg =
        resourceGroup {
            name "pulumigroup"
            location "westus"
        }

    let sa =
        storageAccount {
            name "kitapulumir023"
            resourceGroup rg.Name
            location rg.Location
            kind Kind.StorageV2
            sku {
                name SkuName.Standard_LRS
            }
        }

    let queue =
        queue {
            name "pendingq"
            queueName "pending"
            accountName sa.Name
            resourceGroup rg.Name
        }

    dict [
        "saConnString", getPrimaryKey rg sa :> obj
        "qName", queue.Name :> obj
    ]
)

let deployAndSendMessage () =
    let outputs =
        deploy
            "azure-prototype"
            "dev"
            program
        |> Async.AwaitTask
        |> Async.RunSynchronously

    printfn "Outputs:"
    outputs
    |> Seq.iter (fun (KeyValue(k,v)) -> printfn "%A: %A" k v)

    printfn "Sending message"

    let queue = QueueClient(outputs.["saConnString"] :?> string, "testq")
    queue.SendMessage "Hello from pulumi"
    |> ignore
