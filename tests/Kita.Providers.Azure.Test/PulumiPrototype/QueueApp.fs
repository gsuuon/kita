module PulumiPrototype.Test.Queue

open Kita.Core.Http
open Kita.Core.Infra
open Kita.Providers

open PulumiPrototype.Test.Resources

let kitaApp = infra'<PulumiAzure>

let useQueue = kitaApp "basicQueue" {
    let! q = PulumiQueue<string>()

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
