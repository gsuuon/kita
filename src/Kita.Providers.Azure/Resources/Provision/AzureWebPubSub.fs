namespace Kita.Providers.Azure.Resources.Provision

open Azure.Messaging.WebPubSub
open Kita.Providers.Azure.Resources.Definition
open Kita.Utility

type AzureWebPubSub
    (
        name,
        config: WebPubSubConfig,
        webPubSubConStringWaiter: Waiter<string>
    ) =
    // arm deploy
    // one webpubsub service per app
    // so name of webpubsubservice should be app
    // name here is hub name

    // get connectionstring
    interface IAzureWebPubSub with
        member _.Client =
            async {
                let! conString = webPubSubConStringWaiter.GetAsync
                
                let client = new WebPubSubServiceClient(conString, name)

                return client
            }
