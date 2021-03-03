module Kita.Test.MixedProvider

open Kita.Core
open Kita.Core.Http
open Kita.Core.Http.Helpers
open Kita.Core.Resources
open Kita.Core.Resources.Collections
open Kita.Core.Providers

let infraAz name = Infra<Default.Az>(name)
let infraLocal name = Infra<Default.Local>(name)

let debug = infraLocal "debug" {
    let! klog = CloudLog()
    klog.Info "Debugging"
}

let mixBotWRoute = infraAz "mix bot w route" {
    let! queue = CloudQueue()
    route "hello" [
        POST <| fun req -> async {
            queue.Enqueue req.body
            return ok "Got it"
        }
    ]

    do! debug
}

#if BROKEN // Just to take these out of compile path
let mixTopWRoute = infraAz "mix top w route" {
    do! debug

    let! queue = CloudQueue()
    route "hello" [
        POST <| fun req -> async {
            queue.Enqueue req.body
            return ok "Got it"
        }
    ]
}

let mixMidWRoute = infraAz "mix Mid w route" {
    let! queue = CloudQueue()

    do! debug

    route "hello" [
        POST <| fun req -> async {
            queue.Enqueue req.body
            return ok "Got it"
        }
    ]
}

let mixBotCondWRoute = infraAz "mix bot w route" {
    let! queue = CloudQueue()
    route "hello" [
        POST <| fun req -> async {
            queue.Enqueue req.body
            return ok "Got it"
        }
    ]

    let x = true

    if x then
        do! debug
    }
#endif
