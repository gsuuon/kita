module AzureNativePrototype.Deploy

open Kita.Core

let deploy appName (managed: Managed<AzureNative>) = async {
        do! managed.provider.Deploy appName |> Async.AwaitTask
        return managed
    }

