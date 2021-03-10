// This doesn't work
// Breaks because can't find Microsoft.Extensions.Hosting assembly when executing in fsi
// explicitly adding it doesn't fix
#r "nuget: Pulumi.FSharp.AzureNative,0.7.1.2"
#r "nuget: Pulumi.FSharp.Azure,3.49.0.29"
#r "nuget: Pulumi.FSharp.Core,2.0.15"
#r "nuget: Pulumi.Automation, 2.23.0-alpha.1615392815"
#r "nuget: Ply,0.3.1"
#load "../Common.fsx"
open Common

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
        }

    let queue =
        queue {
            queueName "pending"
            accountName sa.Name
        }

    dict [
        "saConnString", sa.PrimaryConnectionString :> obj
    ]
)

let projectName = "azure-inline-automation"
let stackName = "dev"
let stackArgs = InlineProgramArgs(projectName, stackName, program)

let main () = task {
    let! stack = LocalWorkspace.CreateOrSelectStackAsync(stackArgs)
    printfn "Initialized stack, installing plugins"

    do! stack.Workspace.InstallPluginAsync("azure-native", "0.7.1")
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


    printfn "Conn string: %A" res.Outputs.["saConnString"].Value
}

fsiTaskWorkaround main
|> Async.RunSynchronously
