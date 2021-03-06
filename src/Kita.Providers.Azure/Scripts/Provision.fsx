#r "nuget: Azure.Identity"
#r "nuget: TaskBuilder.fs,2.1.0"
#r "nuget: Azure.ResourceManager.Resources,1.0.0-preview.1"

open System
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Control.Tasks.V2

open Azure.Core
open Azure.Identity
open Azure.ResourceManager.Resources
open Azure.ResourceManager.Resources.Models

/// Requires environment variables to be set:
// AZURE_CLIENT_ID
// AZURE_CLIENT_SECRET
// AZURE_TENANT_ID
// AZURE_SUBSCRIPTION_ID

let value (response: Task<Azure.Response<'T>>) =
    task {
        let! response' = response
        return response'.Value
    }

let iter fn (asyncEnum: IAsyncEnumerator<'T>) =
    task {
        let next = ref true
        let! n = asyncEnum.MoveNextAsync()
        n |> (:=) next
        
        while !next do
            fn asyncEnum.Current
            let! n = asyncEnum.MoveNextAsync()
            n |> (:=) next
    }

type Node =
    | StorageAccount of string
    | Queue of string

type Resource = {
    nodes: Node list
}

type Group = {
    resources: Resource list
}

type Scope = {
    applicationName: string
    location: string
    groups: Map<string, Group>
}

let resourceOp (op: Task<ResourcesCreateOrUpdateOperation>) =
    task {
        let! result = op
        let! x = result.WaitForCompletionAsync()
        return x.Value }

let setStorageAccount
    (resourceGroup: ResourceGroup)
    storageAccountName
    location
    (client: ResourcesManagementClient)
    =
    let accessTierHot () : Dictionary<string, obj>=
        Dictionary ( dict [ "accessTier", "hot" :> obj ] )

    client.Resources.StartCreateOrUpdateAsync(
        resourceGroup.Name,
        "Microsoft.Storage",
        "",
        "storageAccounts",
        storageAccountName,
        "2019-06-01",
        GenericResource(
            Location = location,
            Sku = Sku(
                Name = "Standard_LRS",
                Tier = "Standard"
            ),
            Kind = "StorageV2",
            Properties = accessTierHot() ))
    |> resourceOp
    
let setResourceGroup
    resourceGroupName location (client: ResourcesManagementClient)
    = task {
        let! res =
            client.ResourceGroups.CreateOrUpdateAsync(
                resourceGroupName, ResourceGroup(location))

        return res.Value }

let iterAllResources
    (iterFn: GenericResourceExpanded -> unit)
    (resourceGroup: ResourceGroup)
    (client: ResourcesManagementClient)
    = task {
        let resources =
            client.Resources.ListByResourceGroupAsync(
                resourceGroup.Name
            ).GetAsyncEnumerator()

        do! iter iterFn resources
    }

let managerClient credential =
    ResourcesManagementClient (
        Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID"),
        credential )

let provisionScope
    (client: ResourcesManagementClient)
    (scope: Scope)
    = task {
        let! resourceGroup =
            client
            |> setResourceGroup scope.applicationName scope.location

        let! _storageAccount =
            client
            |> setStorageAccount
                resourceGroup
                (scope.applicationName + "__storageAccount")
                scope.location

        do! client
            |> iterAllResources
                (fun res ->
                    printfn "Resource: %A - %A" res.Name res.Type)
                resourceGroup
    }

let runTask = Async.AwaitTask >> Async.RunSynchronously

let main () =
    provisionScope
    <| managerClient (DefaultAzureCredential())
    <| { applicationName = "kita-app"
         location = "westus"
         groups = Map.empty }

    |> runTask

main()
printfn "Done"
