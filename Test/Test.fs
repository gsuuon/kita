module Kita.Test

open Kita.Core
open Kita.Core.Http
open Kita.Core.Http.Helpers
open Kita.Core.Resources
open Kita.Core.Resources.Collections

let internal infra name = Infra (name, Providers.Default.Az() )
(* let infra name = Infra (name, Providers.Default.Aws() ) *)
    // Compile error for resources which don't support Aws

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
        // Must be after all custom operations (route, proc)
        // Cannot contain custom operations
        do! cloudDebug
}

let cloudMain = infra "main" {
    let! pendingSaves = CloudQueue()
    let! readySaves = CloudQueue()
    let! saves = CloudMap()
    let! klog = CloudLog()

    pendingSaves.NewMessage.Add
    <| fun msg ->
        (klog.Info <| sprintf "Got %s" msg)

    proc (async {
        while true do
            let! msgs = pendingSaves.Dequeue 30
            do! readySaves.Enqueue msgs
            do! Async.Sleep 10000
    } )

    route "save" [
        GET <| fun req -> async {
            match! saves.TryFind req.body with
            | Some s ->
                return { status = OK; body = s }
            | None ->
                return { status = NOTFOUND; body = "🤷" } }
                
        POST <| fun req -> async {
            pendingSaves.Enqueue req.body
            return { status = OK; body = "You're in" } }
    ]

    route "sign_in" [
        POST <| fun req -> async {
            if req.body.Length > 0 then
                return { status = OK; body = "You're in" }
            else
                return { status = NOTFOUND; body = "Who are you?" } }
    ]
}

let program debug =
    // Composing
    Managed.Empty
    |> cloudProcs debug
    |> cloudMain
