open System
open FSharp.Control.Tasks

open AzureNativePrototype
open Kita.Core
open Kita.Core.Infra
open Kita.Core.Http
open Kita.Core.Http.Helpers

module App = 
    let azure = infra'<AzureNative>

    let app = azure "myaznativeapp" {
        let! q = Resource.Queue<string>("myaznatq")

        route "hi" [
            GET <| fun _ -> async {
                let! xs = q.Dequeue 20
                return
                    sprintf "Got %A" xs
                    |> ok
            }

            POST <| fun req -> async {
                let text =
                    req.body
                    |> Seq.toArray
                    |> Text.Encoding.UTF8.GetString
                do! q.Enqueue text
                return ok "Ok sent"
            }
        ]
    }

    let app1 = azure "myaznativeapp" {
        let! q = Resource.Queue<string>("myaznatq")

        route "hi" [
            GET <| fun _ -> async {
                let! xs = q.Dequeue 20
                return
                    sprintf "Got %A" xs
                    |> ok
            }

            POST <| fun req -> async {
                let text =
                    req.body
                    |> Seq.toArray
                    |> Text.Encoding.UTF8.GetString
                do! q.Enqueue text
                return ok "Ok sent"
            }
        ]
    }

    let combined = app >> app1

    let deploy () =
        (* AzureNative.Run("myaznativeapp", "eastus", app) *)

        Managed.empty()
        |> combined
        |> fun managed ->
            AzureNative.Run("myaznativeapp", "eastus", managed)
            |> Async.AwaitTask
            // FIXME enforce lowercase only
            // Same with queue names
            // azure most names must be lowercase i guess?

[<EntryPoint>]
let main argv =
    let server =
        printfn "Running"

        App.deploy()
        |> Async.RunSynchronously

    0 // return an integer exit code
