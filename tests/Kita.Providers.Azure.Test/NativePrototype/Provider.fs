namespace AzureNativePrototype

open System

open FSharp.Control.Tasks

open Kita.Providers
open Kita.Utility

open Provision
open AzureNextApi

type AzureNative() =
    inherit Provider("Azure.Native")
    let defaultLocation = "eastus"

    let mutable deployRequests = []
    let requestDeploy deploy =
        // Could instantly deploy resource if provider already deployed
        // to support attaching resources after deployment
        deployRequests <- deploy :: deployRequests

    let connectionString = Waiter<string>()
    member _.WaitConnectionString = connectionString
    member _.OnConnection = connectionString.OnSet
    member _.Deploy appName = task {
        let! (rgName, saName, conString) =
            provision appName defaultLocation

        for deploy in deployRequests do
            do! deploy rgName saName

        connectionString.Set conString
    }
        
    member _.RequestQueue (qName) =
        requestDeploy <| Storage.createQueue qName
