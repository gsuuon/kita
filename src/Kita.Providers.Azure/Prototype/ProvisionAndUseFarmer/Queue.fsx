#r "nuget: Azure.Storage.Queues"
#r "nuget: Farmer"

open Farmer
open Farmer.Builders

open Azure.Storage.Queues

let storage = storageAccount {
    name "kita3r9u8p"
    add_queue "pending"
}

let template = arm {
    location Location.WestUS
    add_resource storage

    output "key" storage.Key
}

let output =
    template
    |> Deploy.execute "farmergroup" Deploy.NoParameters

printfn "StorageAccount created: %A" storage.Name
printfn "StorageAccount created string: %A" storage.Name.ResourceName.Value
printfn "StorageAccount conn string: %A" output.["key"]

let queue = QueueClient(output.["key"], "pending")
queue.SendMessage("Hello Azure!")
