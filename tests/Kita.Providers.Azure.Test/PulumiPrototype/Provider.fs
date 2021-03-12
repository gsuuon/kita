namespace Kita.Providers

open Kita.Providers

open Pulumi.FSharp.AzureNative.Resources
open Pulumi.FSharp.AzureNative.Storage
open Pulumi.FSharp.AzureNative.Storage.Inputs
open Pulumi.AzureNative.Storage
open Pulumi.AzureNative.Resources

open Pulumi.Automation
open PulumiPrototype.Helpers
open PulumiPrototype.Utility

type PulumiAzure() =
    inherit Provider("Azure.Pulumi")
    let PrimaryKey = "primaryKey"
    let StorageAccountName = "storageAccountName"

    let mutable queuedWork = []
    let mutable queuedResources = []

    let program appName =
        PulumiFn.Create(fun () ->
            let rg =
                resourceGroup {
                    name appName
                    location "westus"
                }

            let sa =
                storageAccount {
                    name appName
                    resourceGroup rg.Name
                    location rg.Location
                    kind Kind.StorageV2
                    sku {
                        name SkuName.Standard_LRS
                    }
                }

            let resourceOutputs =
                queuedResources
                |> List.collect (fun adder -> adder sa rg)

            [
                PrimaryKey, getPrimaryKey rg sa :> obj
                StorageAccountName, sa.Name :> obj
            ]
            @ resourceOutputs
            |> dict
        )

    let connectionString : string option ref = ref None

    member _.ConnectionString = waitUntilValue 100 connectionString

    member _.AddResource adder =
        queuedResources <- adder :: queuedResources

    member this.AddQueue name' =
        this.AddResource
        <| fun (sa: StorageAccount) (rg: ResourceGroup) ->
            let q =
                queue {
                    accountName sa.Name
                    resourceGroup rg.Name
                    name name'
                }

            [ name', q.Name :> obj ]
    
    member _.AddDependent (workItem: Async<unit>) =
        queuedWork <- workItem :: queuedWork

    member _.RunDependents () =
        queuedWork
        |> Async.Parallel
        
    member _.Initialize(appName) = async {
        let! outputs =
            deploy
            <| "azure-prototype-" + appName // project name
            <| "dev" // stack name
            <| program appName

            |> Async.AwaitTask

        connectionString :=
            let primaryKey = outputs.[PrimaryKey] :?> string
            let storageAccountName = outputs.[StorageAccountName] :?> string

            Some $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={primaryKey}"
    }
