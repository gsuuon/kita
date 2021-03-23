open System

open AzureNativePrototype
open Kita.Core
open Kita.Core.Infra
open Kita.Core.Http
open Kita.Core.Http.Helpers

open GiraffePrototype.Program

module App = 
    let azure = infra'<AzureNative>

    let app = azure "myaznativeapp" {
        let! q = Resource.Queue<string>("myaznatq")

        route "hi" [
            GET <| fun _ -> async {
                let! xs = q.Dequeue 1
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
        Managed.empty()
        |> app
        |> Deploy.deploy "myaznativeapp"
            // FIXME enforce lowercase only
            // Same with queue names
            // azure most names must be lowercase i guess?

[<EntryPoint>]
let main argv =

    let server =
        App.deploy()
        |> Async.RunSynchronously
        |> fun managed ->
            Server.start managed.handlers

    printfn "Deployed.."
    0 // return an integer exit code
