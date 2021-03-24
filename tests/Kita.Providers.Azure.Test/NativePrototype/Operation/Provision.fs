module AzureNativePrototype.Provision

open FSharp.Control.Tasks
open AzureNextApi.Resources
open AzureNextApi.Storage

let provision appName location = task {
    let! rg = createResourceGroup appName location
    let! sa = createStorageAccount appName location

    let! conString = getStorageConnectionString rg.Name sa.Name

    return (rg.Name, sa.Name, conString)
}
