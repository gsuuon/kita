namespace Kita.Providers

open Kita.Providers
open Kita.Utility

open Farmer
open Farmer.Builders

type FarmerAzure() =
    inherit Provider("Azure.Pulumi")

    let connectionString = Waiter<string>()

    let mutable resourceRequests = []
    let requestResource requester =
        resourceRequests <- requester :: resourceRequests

    member _.OnConnect = connectionString.OnSet

    // Called after all resources are attached
    member _.Initialize(appName: string) =
        // I feel like this is hitting a dead-end
        // I need programatic construction of provisioning
        // this is more config-like
        // not every resource will have a list version
        // how do I create multiple tables?
        let storage = storageAccount {
            name appName

            (* for x in ["a";"b"] do *)
            (*     add_table x *)
        }

        let template = arm {
            location Location.WestUS
            add_resource storage
            output "key" storage.Key
        }

        let output =
            template
            |> Deploy.execute appName Deploy.NoParameters

        let conString = output.["key"]

        connectionString.Set conString

        
    member _.RequestQueue(name: string) =
        requestResource
        <| fun _ -> ()
            
    member _.AddTable() = ()
    member _.AddFile() = ()
