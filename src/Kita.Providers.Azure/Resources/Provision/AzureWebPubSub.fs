namespace Kita.Providers.Azure.Resources.Provision

open FSharp.Control.Tasks
open Azure.Messaging.WebPubSub

open Kita.Utility
open Kita.Providers.Azure.AzureNextApi
open Kita.Providers.Azure.Resources.Definition

type AzureWebPubSub
    (
        hubName: string,
        appName: string,
        armParameters: WebPubSubArmParameters,
        webPubSubConStringWaiter: Waiter<string>
    ) =
    
    let provisionRequest rgName _saName = task {
        let! deployment =
            Resources.createArmDeployment
            <| rgName
            <| appName
            <| AzureWebPubSubTemplates.armTemplate
            <| AzureWebPubSubTemplates.parameters armParameters

        return ()
    }

    interface IAzureWebPubSub with
        member _.Client =
            async {
                let! conString = webPubSubConStringWaiter.GetAsync
                
                let client = new WebPubSubServiceClient(conString, hubName)

                return client
            }
