namespace Kita.Providers.Azure.Resources.Provision

open System.Collections.Generic
open FSharp.Control.Tasks
open Azure.Messaging.WebPubSub

open Kita.Utility
open Kita.Providers.Azure.AzureNextApi
open Kita.Providers.Azure.Resources.Definition
open Kita.Providers.Azure.Activation

type AzureWebPubSub (hubName: string, appName: string) =
    let envVarName = "Kita_Azure_WebPubSub_ConString_" + appName

    let client =
        produceWithEnv
        <| envVarName
        <| fun conString ->
            new WebPubSubServiceClient(conString, hubName)

    member _.ProvisionRequest 
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
                Some (envVarName, primaryConString)
        }

    interface IAzureWebPubSub with
        member _.Client = client
