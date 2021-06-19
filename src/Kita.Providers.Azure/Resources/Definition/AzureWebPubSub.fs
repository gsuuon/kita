namespace Kita.Providers.Azure.Resources.Definition

open Azure.Messaging.WebPubSub

open Kita.Core

type IAzureWebPubSub =
    inherit CloudResource
    abstract Client : WebPubSubServiceClient

type WebPubSubConfig =
    { tier : string
      capacity : int }

type AzureWebPubSubProvider =
    abstract Provide : string * WebPubSubConfig -> IAzureWebPubSub 

type AzureWebPubSub(name: string, ?config: WebPubSubConfig) =
    let config = defaultArg config { tier = "free"; capacity = 1 }

    member _.Create (p: #AzureWebPubSubProvider) =
        p.Provide (name, config)
