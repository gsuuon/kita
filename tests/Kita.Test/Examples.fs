module Kita.Test.Examples

open Kita.Core
open Kita.Core.Infra
open Kita.Core.Http
open Kita.Core.Http.Helpers
open Kita.Resources
open Kita.Resources.Collections
open Kita.Providers

let infra = infra'<Azure>

let cloudAbout =
    infra "about" {
        route "about" [ ok "this is kita" |> asyncReturn |> konst |> GET ] }

let cloudDebug (klog: CloudLog) =
    infra'<Local> "debug" {
        klog.Info "debugging"

        let! secretQueue = CloudQueue()

        route
            "admin"
            [ ok "You found the admin route"
              |> asyncReturn
              |> konst
              |> GET ]
    }

let cloudProcs debug =
    infra "procs" {
        let! klog = CloudLog()

        do! cloudAbout // Nesting

        proc CloudTask (
            async {
                let mutable count = 0

                while true do
                    klog.Info(sprintf "This is process tick: %i" count)
                    count <- count + 1
                    do! Async.Sleep 10000
            }
        )

        route
            "status"
            [ GET
              <| fun _ -> async { return { status = OK; body = "All good" } } ]

        // Conditional nesting, mixed provider
        do! gated debug <| cloudDebug klog
    }

let cloudMain =
    infra "main" {
        let! pendingSaves = CloudQueue()
        let! readySaves = CloudQueue()
        let! saves = CloudMap()
        let! klog = CloudLog()

        pendingSaves.NewMessage.Add
        <| fun msg -> (klog.Info <| sprintf "Got %s" msg)

        proc CloudTask (
            async {
                while true do
                    let! msgs = pendingSaves.Dequeue 30
                    do! readySaves.Enqueue msgs
                    do! Async.Sleep 10000
            }
        )

        route
            "save"
            [ GET
              <| fun req ->
                  async {
                      match! saves.TryFind req.body with
                      | Some s -> return { status = OK; body = s }
                      | None -> return { status = NOTFOUND; body = "🤷" }
                  }

              POST
              <| fun req ->
                  async {
                      pendingSaves.Enqueue req.body
                      return { status = OK; body = "You're in" }
                  } ]

        route
            "sign_in"
            [ POST
              <| fun req ->
                  async {
                      if req.body.Length > 0 then
                          return { status = OK; body = "You're in" }
                      else
                          return
                              { status = NOTFOUND
                                body = "Who are you?" }
                  } ]
    }

let program debug =
    // Composing
    Managed.empty () |> cloudProcs debug |> cloudMain
