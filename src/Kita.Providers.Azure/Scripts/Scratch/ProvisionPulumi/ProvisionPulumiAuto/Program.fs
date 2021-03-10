module ProvisionPulumiAuto.Program

open Pulumi.FSharp
open Pulumi.FSharp.Output
open Pulumi.FSharp.AzureNative.Resources
open Pulumi.FSharp.AzureNative.Network
open Pulumi.FSharp.Azure.Storage
open Pulumi.FSharp.AzureNative.Storage
open Pulumi.Automation
    // can't figure out how to get connection string from this api
    // ** there's an example in the azure-fsharp pulumi template

open FSharp.Control.Tasks.Affine

let program = PulumiFn.Create( fun () ->
    let rg =
        resourceGroup {
            name "pulumigroup"
            location "westus"
        }

    let sa =
        account {
            // can't figure out how to get connection string from the AzureNative api
            name "kitapulumistorage"
            resourceGroup rg.Name
            location rg.Location
            accountKind "StorageV2"
            accessTier "hot"
            accountTier "standard"
            accountReplicationType "LRS"
        }

    let queue =
        queue {
            // several of these are required and are runtime error if not present
            name "pendingq"
            queueName "pending"
            accountName sa.Name
            resourceGroup rg.Name
        }

    dict [
        "saConnString", sa.PrimaryConnectionString :> obj
    ]
)

let projectName = "azure-inline-automation"
let stackName = "dev"
let stackArgs = InlineProgramArgs(projectName, stackName, program)

let deploy () = task {
    let! stack = LocalWorkspace.CreateOrSelectStackAsync(stackArgs)
    printfn "Initialized stack, installing plugins"

    do! stack.Workspace.InstallPluginAsync("azure-native", "0.7.1")
    do! stack.Workspace.InstallPluginAsync("azure", "3.49.0")
        // if this flow has been run using normal pulumi cli, this may be already set
        // > pulumi plugin ls
        // to see all installed plugins
    printfn "Installed, setting config"
    do! stack.SetConfigValueAsync("azure-native:location", ConfigValue("WestUs"))
        // > pulumi config
        // to show all config values

    printfn "Refreshing"

    let console = System.Action<string>(fun (x: string) -> System.Console.WriteLine x)
    
    let! updateResult =
        stack.RefreshAsync ( RefreshOptions ( OnOutput = console ))

    printfn "Updating"
    let! res =
        stack.UpAsync ( UpOptions ( OnOutput = console))

    if res.Summary.ResourceChanges <> null then
        printfn "Update summary"
        res.Summary.ResourceChanges
        |> Seq.iter (fun c -> printfn "   %A: %A" c.Key c.Value)

    printfn "Outputs:"

    return
        res.Outputs
        |> Seq.map
            (fun kv ->
                kv.Key, kv.Value.Value)
        |> dict
}

[<EntryPoint>]
let main _argv =
    let outputs =
        deploy()
        |> Async.AwaitTask
        |> Async.RunSynchronously

    printfn "Outputs:"
    outputs
    |> Seq.iter (fun (KeyValue(k,v)) -> printfn "%A: %A" k v)

    0
