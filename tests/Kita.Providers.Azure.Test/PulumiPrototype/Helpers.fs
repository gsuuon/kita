module ProvisionPulumiAuto.Helpers

open Pulumi.FSharp
open Pulumi.Automation
open Pulumi.AzureNative.Storage
open Pulumi.AzureNative.Resources

open FSharp.Control.Tasks.Affine

let getPrimaryKey (resourceGroup: ResourceGroup) (storageAccount: StorageAccount) =
    let getStorageAccountPrimaryKey(resourceGroupName: string, accountName: string): Async<string> = async {
        let! accountKeys =
            ListStorageAccountKeysArgs(ResourceGroupName = resourceGroupName, AccountName = accountName) |>
            ListStorageAccountKeys.InvokeAsync
            |> Async.AwaitTask
        return accountKeys.Keys.[0].Value
    }

    Outputs.pair resourceGroup.Name storageAccount.Name
    |> Outputs.applyAsync getStorageAccountPrimaryKey

let console = System.Action<string>(fun (x: string) -> System.Console.WriteLine x)

let deploy projectName stackName program =
    let stackArgs = InlineProgramArgs(projectName, stackName, program)

    task {
        let! stack = LocalWorkspace.CreateOrSelectStackAsync(stackArgs)

        do! stack.Workspace.InstallPluginAsync("azure-native", "0.7.1")

        let! _updateResult = stack.RefreshAsync ( RefreshOptions ( OnOutput = console ))
        let! res = stack.UpAsync ( UpOptions ( OnOutput = console))

        return
            res.Outputs
            |> Seq.map
                (fun kv ->
                    kv.Key, kv.Value.Value)
            |> dict
    }
