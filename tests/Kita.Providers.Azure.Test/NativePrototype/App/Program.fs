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
        // TODO name is thrown away
        // This should be the app name
        // app names must be between 2-60 characters alphanumeric + non-leading hyphen
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

    let deploy () =
        AzureNative.Run("myaznativeapp2", "eastus", app)
            // TODO this should be the resourcegroup name

[<EntryPoint>]
let main argv =
    let managed =
        printfn "Deploying"

        App.deploy().Wait()

    0 // return an integer exit code
