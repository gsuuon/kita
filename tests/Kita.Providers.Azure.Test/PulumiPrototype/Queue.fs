module ProvisionPulumiAuto.KitaApp.Queue

open Kita.Core.Http
open Kita.Core.Infra
open Kita.Resources
open Kita.Providers

let kitaApp = infra'<Azure>

let useQueue = kitaApp "basicQueue" {
    let! q = CloudQueue()

    route "add_item" [
        GET
        <| fun req ->
            q.
    ]
}
