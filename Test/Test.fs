module Kita.Test

open Kita.Core
open Kita.Core.Http
open Kita.Core.Http.Helpers
open Kita.Core.Resources
open Kita.Core.Resources.Collections

let infra name = Infra (name, { name = "test"; deploy = "deploy" })

let cloudAbout = infra "about" {
    route "about" [
        ok "this is kita"
        |> asyncReturn
        |> konst
        |> GET
    ]
}

let cloudDebug = infra "debug" {
    route "admin" [
        ok "You found the admin route"
        |> asyncReturn
        |> konst
        |> GET
    ]
}

let cloudProcs debug = infra "procs" {
    let! klog = CloudLog()

    do! cloudAbout // Nesting

    proc (async {
        let mutable count = 0
        while true do
            klog.Info (sprintf "This is process tick: %i" count)
            count <- count + 1
            do! Async.Sleep 10000
    })

    route "status" [
        GET <| fun _ -> async {
            return { status = OK; body = "All good" } }
    ]

    if debug then
        // Conditional
        // Must be at end of infra block if custom operations
        // (proc, route) are used

        // Cannot be used with custom operations
        do! cloudDebug
}

let cloudMain = infra "main" {
    let! pendingSaves = CloudQueue()
    let! readySaves = CloudQueue()
    let! myMap = CloudMap()
    let! klog = CloudLog()

    pendingSaves.NewMessage.Add
    <| fun msg ->
        (printfn "Got %s" msg)

    proc (async {
        while true do
            let! msgs = pendingSaves.Dequeue 30
            do! readySaves.Enqueue msgs
            do! Async.Sleep 10000
    } )

    route "save" [
        GET <| fun req -> async {
            pendingSaves.Enqueue req.body
            return { status = OK; body = "Hi " + req.body } }
    ]

    route "sign_in" [
        POST <| fun req -> async {
            return { status = OK; body = "You're in" } }
    ]
}

let program debug =
    // Composing
    Managed.Empty
    |> cloudProcs debug
    |> cloudMain
