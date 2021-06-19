namespace Kita.Providers.Azure.Resources.Provision

open System.Collections.Generic
open FSharp.Control.Tasks
open Azure.Messaging.WebPubSub

open Kita.Utility
open Kita.Providers.Azure.AzureNextApi
open Kita.Providers.Azure.Resources.Definition

type AzureWebPubSub (hubName: string) =
    let webPubSubConStringWaiter = Waiter<string>()

    let envVarName appName =
        "Kita_Azure_WebPubSub_ConString_" + appName

    member _.ProvisionRequest 
        (appName: string)
        (armParameters: WebPubSubArmParameters)
        =
        fun rgName _saName -> task {
            let! deployment =
                Resources.createArmDeployment
                <| rgName
                <| appName
                <| AzureWebPubSubTemplates.armTemplate
                <| AzureWebPubSubTemplates.parameters armParameters

            let outputs =
                deployment.Properties.Outputs
                :?> IDictionary<string, obj>

            let primaryConStringField =
                outputs.["primaryConString"]
                :?> IDictionary<string, obj>

            let primaryConString =
                primaryConStringField.["value"]
                :?> string

            printfn "AzureWebPubSub outputs:\n%s" primaryConString

            return
                Some (envVarName appName, primaryConString)
        }

    interface IAzureWebPubSub with
        member _.Client =
            async {
                let! conString = webPubSubConStringWaiter.GetAsync
                
                let client = new WebPubSubServiceClient(conString, hubName)

                return client
            }
