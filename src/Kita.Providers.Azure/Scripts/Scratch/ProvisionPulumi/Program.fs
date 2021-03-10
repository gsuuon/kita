﻿module Program

open Pulumi.FSharp
open Pulumi.AzureNative.Resources
open Pulumi.AzureNative.Storage
open Pulumi.AzureNative.Storage.Inputs

// Helper function to retrieve the primary key of a storage account
let getStorageAccountPrimaryKey(resourceGroupName: string, accountName: string): Async<string> = async {
    let! accountKeys =
        ListStorageAccountKeysArgs(ResourceGroupName = resourceGroupName, AccountName = accountName) |>
        ListStorageAccountKeys.InvokeAsync
        |> Async.AwaitTask
    return accountKeys.Keys.[0].Value
}

let infra () =
    // Create an Azure Resource Group
    let resourceGroup =
        ResourceGroup("pulumigroup")

    // Create an Azure Storage Account
    let storageAccount =
        StorageAccount("pulumitest1230u8",
            StorageAccountArgs
                (ResourceGroupName = io resourceGroup.Name,
                 Sku = input (SkuArgs(Name = inputUnion2Of2 SkuName.Standard_LRS)),
                 Kind = inputUnion2Of2 Kind.StorageV2))

    let queue =
        Queue
            ( "pending"
            , QueueArgs
                ( AccountName = io storageAccount.Name
                , ResourceGroupName = io resourceGroup.Name
                )
            )

    // Get the primary key
    let primaryKey =
        Outputs.pair resourceGroup.Name storageAccount.Name
        |> Outputs.applyAsync getStorageAccountPrimaryKey

    // Export the primary key for the storage account
    dict [("connectionString", primaryKey :> obj)]

[<EntryPoint>]
let main _ =
  Deployment.run infra
