namespace Kita.Providers.Azure.Resources.Provision

open Azure.Messaging.WebPubSub
open Kita.Providers.Azure.Resources.Definition

type AzureWebPubSub
    (
        name,
        config: WebPubSubConfig
    ) =
    // arm deploy
    // get result
    let client = new WebPubSubServiceClient()
    { new 
    
