module Kita.Test
// Temporary test module

open Kita.Core
open Kita.Core.Http
open Kita.Core.Resources
open Kita.Core.Resources.Collections

let infra = Infra { name = "test"; deploy = "deploy" }

let cloudMain = infra {
    let! myQueue = CloudQueue()
    let! myMap = CloudMap()
    let! klog = CloudLog()

    myQueue.NewMessage.Add
    <| fun msg ->
        sprintf "Got %s" msg
        |> klog.Info

    let transfer = async {
        while true do
            (* let! msgs = myQueue.Dequeue(30) *)
            do! Async.Sleep 10000
    }

    let x = CloudTask transfer

    do! CloudZero.Instance

    route "save" [
        GET <| fun req -> async {
            return { status = OK; body = "Hi " + req.body }
        }
    ]
}
    
