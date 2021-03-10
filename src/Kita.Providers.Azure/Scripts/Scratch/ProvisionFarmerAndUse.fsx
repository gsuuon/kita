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


(*
 * Thoughts on Farmer:
 * There are some documentation issues - the docs are for a different version (vNext?) than the current latest release. I don't know how to pull vNext via NuGet.
 *  - add_table/add_tables is missing from 1.4.0 but present in master Branch.
 *  - use_managed_keyvault exists in docs but not present anywhere?
 *)

(* Compatible version of Azure CLI 2.20.0 detected *)
(* Checking Azure CLI logged in status... you are already logged in, nothing to do. *)

(* Using subscription 'kita-test' (). *)
(* Creating resource group farmergroup... *)
(* Deploying ARM template (please be patient, this can take a while)... *)
(* All done, now parsing ARM response to get any outputs... *)
(* StorageAccount created: StorageAccountName (ResourceName "kita3r9u8p") *)
(* StorageAccount created string: "kita3r9u8p" *)
(* StorageAccount conn string: "DefaultEndpointsProtocol=https;AccountName=kita3r9u" *)
(* 8p;AccountKey= *)
