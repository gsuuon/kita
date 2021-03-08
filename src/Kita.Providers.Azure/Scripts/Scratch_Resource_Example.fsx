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
    let! storageAccountOp =
        client.Resources.StartCreateOrUpdateAsync
            ( resourceGroupName
            , "Microsoft.Storage"
            , ""
            , "storageAccounts"
            , "as2qriu9h"
                // storeA is not a valid storage account name. Storage account name must be between 3 and 24 characters in length and use numbers and lower-case letters only.
            , "2019-06-01"
            , GenericResource
                ( Location = "westus"
                , Sku = Sku
                    ( Name = "Standard_LRS"
                    , Tier = "Standard"
                    )
                , Kind = "StorageV2"
                , Properties = ([ "accessTier", "hot" :> obj ] |> dict |> Dictionary)
                )
            )

    let! storageAccountResult = storageAccountOp.WaitForCompletionAsync()
    let storageAccount = storageAccountResult.Value

    printfn "Created storage account %s" storageAccount.Name
}

let fsiTaskWorkaround aTaskCtor = async {
    return! aTaskCtor() |> Async.AwaitTask
}

fsiTaskWorkaround example
|> Async.RunSynchronously

printfn "Finished"
