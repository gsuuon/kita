module PulumiPrototype.Test.Queue

open Kita.Core.Http
open Kita.Core.Infra
open Kita.Resources.Collections
open Kita.Providers

let kitaApp = infra'<Azure>

let useQueue = kitaApp "basicQueue" {
    let! q = CloudQueue<string>()

    route "item" [
        POST
        <| fun req ->
            async {
                do! q.Enqueue req.body
                return { status = OK; body = "Got it" }
            }
        GET
        <| fun _ ->
            async {
                let! x = q.Dequeue()
                return { status = OK; body = x }
            }
    ]
}
