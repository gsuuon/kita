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
        route "about" [ ok "this is kita" |> asyncReturn |> konst |> get ] }

let cloudDebug (klog: CloudLog) =
    infra'<Local> "debug" {
        klog.Info "debugging"

        route
            "admin"
            [ ok "You found the admin route"
              |> asyncReturn
              |> konst
              |> get ]
    }

let cloudProcs debug =
    infra "procs" {
        let! klog = CloudLog()

        nest cloudAbout
            // Nesting

        proc CloudTask
            (async {
                let mutable count = 0

                while true do
                    klog.Info(sprintf "This is process tick: %i" count)
                    count <- count + 1
                    do! Async.Sleep 10000
            })

        route
            "status"
            [ get <| fun _ ->
                async { return { status = OK; body = "All good" } } ]

        nest (gated debug <| cloudDebug klog)
            // Conditional nesting, mixed provider
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
            [ get <| fun req ->
                  async {
                      match! saves.TryFind req.queries.["sid"] with
                      | Some s -> return { status = OK; body = s }
                      | None -> return { status = NOTFOUND; body = "🤷" }
                  }

              post <| fun req ->
                  async {
                      do! pendingSaves.Enqueue req.body
                      return { status = OK; body = "You're in" }
                  } ]

        route
            "sign_in"
            [ post <| fun req ->
                  async {
                      match Seq.tryHead req.body with
                      | Some _ ->
                          return { status = OK; body = "You're in" }
                      | None ->
                          return
                              { status = NOTFOUND
                                body = "Who are you?" }
                  } ]
    }

let program debug =
    // Composing
    Managed.empty ()
    |> (cloudProcs debug).Attach
    (* |> cloudMain.Attach *)

(*
I'm not sure what composition actually gets me
I can change the api to spit out the provider and managed
composition would have to be only with the same providers, i think
what are some use cases for composition?

letting a block have access to resources of another block
letting a block define resources within another block

which use cases does composition cover that nesting doesn't?

It's much easier to just put them together

But i could just similarly create a helper that does this like
[ list of things ] |> runThemAll

One use case I could see is defining a block without a given Provider type
and letting composition infer the type
*)
