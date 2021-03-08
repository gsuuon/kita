#r "nuget: Azure.Identity"
#r "nuget: Azure.ResourceManager.Resources,1.0.0-preview.1"
#r "nuget: Ply, 0.3.1"

open System
open System.Collections.Generic
open Azure.Identity
open Azure.ResourceManager.Resources
open Azure.ResourceManager.Resources.Models
open FSharp.Control.Tasks.Affine

let example () = task {
    let resourceGroupName = "groupA"

    let credential = DefaultAzureCredential()
    let client =
        ResourcesManagementClient
            ( Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID")
            , credential
            )

    printfn "Creating resource group.."
    let! result =
        client.ResourceGroups.CreateOrUpdateAsync
            ( resourceGroupName
            , ResourceGroup("westus")
            )

    let resourceGroup = result.Value

    printfn "Created resource group %s" resourceGroup.Name

    printfn "Creating storage account.."
    let! storageAccountResult =
        client.Resources.StartCreateOrUpdateAsync
            ( resourceGroupName
            , "Microsoft.Storage"
            , ""
            , "storageAccounts"
            , "storeA"
            , "2019-06-01"
            , GenericResource
                ( Location = "westus"
                , Sku = Sku
                    ( Name = "Standard_LRS"
                    , Tier = "Standard"
                    )
                , Kind = "StorageV2"
                , Properties = ([ "accessTier", "hot" ] |> dict |> Dictionary)
                )
            )

    let storageAccount = storageAccountResult.Value
    printfn "Created storage account %s" storageAccount.Name
}

let smallExampleTask () = task {
    let resourceGroupName = "groupA"

    let credential = DefaultAzureCredential()
    let client =
        ResourcesManagementClient
            ( Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID")
            , credential
            )

    printfn "Creating resource group %s" resourceGroupName
    let! result =
        client.ResourceGroups.CreateOrUpdateAsync
            ( resourceGroupName
            , ResourceGroup("westus")
            )

    let resourceGroup = result.Value
    printfn "Created %s" resourceGroup.Name

    return resourceGroup.Name
}

let smallExampleAsync () = async {
    let resourceGroupName = "groupA"

    let credential = DefaultAzureCredential()
    let client =
        ResourcesManagementClient
            ( Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID")
            , credential
            )

    printfn "Creating resource group %s" resourceGroupName
    let! result =
        client.ResourceGroups.CreateOrUpdateAsync
            ( resourceGroupName
            , ResourceGroup("westus")
            )
        |> Async.AwaitTask

    let resourceGroup = result.Value
    printfn "Created %s" resourceGroup.Name

    return resourceGroup.Name
}


let smallExampleWrapped () = async {
    // async/await stalls in fsi
    // async/await stalls in fsi, this is the workaround
    return! smallExampleTask() |> Async.AwaitTask
}

smallExampleWrapped()
|> Async.RunSynchronously
