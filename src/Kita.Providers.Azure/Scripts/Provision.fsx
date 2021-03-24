#r "nuget: Azure.Identity"
#r "nuget: TaskBuilder.fs,2.1.0"
#r "nuget: Azure.ResourceManager.Resources,1.0.0-preview.1"
#r "nuget: Microsoft.Azure.ConfigurationManager, 4.0.0"
// This may fail to load due to https://github.com/dotnet/fsharp/issues/10893
// Depending on fsi version

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

let accessTierHot () : Dictionary<string, obj>=
    Dictionary ( dict [ "accessTier", "hot" :> obj ] )

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

type ResourceNode =
    | StorageAccount of name: string
    | Queue of string

type Resource = {
    nodes: ResourceNode list
    (* NOTE
        Nodes could effectively be paths
        the stack includes the storageaccount and any parents
        provisioning the same nodes should be noops
    *)
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
        printfn "Finished"
        return x.Value }

let managerClient credential =
    printfn "Creating manager client"
    ResourcesManagementClient (
        Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID"),
        credential )

// Not sure I should keep this pattern
// setX - composable (maybe?), client last arg
// provisionX - map/iter, item collection last arg, client first arg

let setQueue
    (resourceGroup: ResourceGroup)
    storageName
    queueName
    location
    (client: ResourcesManagementClient)
    =
    client.Resources.StartCreateOrUpdateAsync
        ( resourceGroup.Name // resourcegroup
        , "Microsoft.Storage" // namespace
        , storageName // parent resource path
        , "queueServices/queues" // resourceType
        , queueName // resourceName
        , "2019-06-01" // apiversion
        , GenericResource
            ( Location = location
            , Sku = Sku
                ( Name = "Standard_LRS"
                , Tier = "Standard")
            , Kind = "StorageV2"
            , Properties = accessTierHot()
            )
        )
    |> resourceOp

let setStorageAccount
    (resourceGroup: ResourceGroup)
    storageAccountName
    location
    (client: ResourcesManagementClient)
    =
    printfn "Setting storage account %s" storageAccountName
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
    =
    task {
        printfn "Setting resource group %s" resourceGroupName

        let! result =
            client.ResourceGroups.CreateOrUpdateAsync
                (resourceGroupName, ResourceGroup(location))

        printfn "Set resource group %s" resourceGroupName
        return result.Value
    }

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

let provisionResourceNode
    (client: ResourcesManagementClient)
    (resourceGroup: ResourceGroup)
    (scope: Scope)
    =
    function
    | StorageAccount storageName ->
        setStorageAccount
            resourceGroup
            storageName
            scope.location
            client
    | Queue queueName ->
        setQueue
            resourceGroup
            "kitappstorage"
                // TODO
            queueName
            scope.location
            client

let waitAll x = // FIXME There's gotta be a built-in way to do this with a task?
    x
    |> List.map Async.AwaitTask
    |> Async.Sequential
    |> Async.StartAsTask

let provisionResource 
    (client: ResourcesManagementClient)
    (resourceGroup: ResourceGroup)
    (scope: Scope)
    ({nodes=nodes})
    =
    nodes
    |> List.map (provisionResourceNode client resourceGroup scope)
    |> waitAll

let provisionGroup
    (client: ResourcesManagementClient)
    (scope: Scope)
    (groupName: string, group: Group)
    = task {
        let! resourceGroup =
            client
            |> setResourceGroup
                groupName
                scope.location

        return!
            group.resources
            |> List.map (provisionResource client resourceGroup scope)
            |> waitAll
    }
    
let provisionScope
    (client: ResourcesManagementClient)
    (scope: Scope)
    = task {
        printfn "Provisioning resources"
        let! provisionedResources =
            scope.groups
            |> Map.toList
            |> List.map
                (provisionGroup client scope)
            |> waitAll

        printfn "Provisioned: %A" provisionedResources

        provisionedResources
        |> Array.iter (printfn "%A")

        (* do! client *)
        (*     |> iterAllResources *)
        (*         (fun res -> *)
        (*             printfn "Resource: %A - %A" res.Name res.Type) *)
        (*         resourceGroup *)
    }

let main () = async {
    return!
        provisionScope
        <| managerClient (DefaultAzureCredential())
        <| { applicationName = "kita-app"
             location = "westus"
             groups = Map.ofList [
                "groupA", {
                    resources = [
                        {
                            nodes = [
                                StorageAccount "kitaappstorage"
                                Queue "kitappqueue"
                            ]
                        }
                    ]
                }
             ] }
        |> Async.AwaitTask
}

main()
|> Async.RunSynchronously

printfn "Done"