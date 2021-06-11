open System
open FSharp.Control.Tasks

open Kita.Core
open Kita.Domains
open Kita.Domains.Routes
open Kita.Providers.Azure

module AppSpec =
    type AppState =
        { routeState : RouteState }
        static member Empty = { routeState = RouteState.Empty }

    let routesDomain = 
        { new UserDomain<_,_> with
            member _.get s = s.routeState
            member _.set s rs = { s with routeState = rs } }

    let routes = RoutesBlock<AppState> routesDomain

module App = 
    open AppSpec
    open Kita.Domains.Routes.Http
    open Kita.Domains.Routes.Http.Helpers
    open Kita.Compile.Reflect
    open Kita.Resources
    open Kita.Resources.Collections

    let app =
        Block<AzureProvider, AppState> "myaznativeapp" {

        // TODO name is thrown away
        // This should be the app name
        // app names must be between 2-60 characters alphanumeric + non-leading hyphen
        let! q = CloudQueue<string>("myaznatq")

        let! map = CloudMap<string, string>("myaznatmap")
        let! chats = CloudMap<string, string list>("mychatroom")

        let! lg = CloudLog()

        let! _task =
            CloudTask("0 * * * * *",
                fun () -> async {
                    let! items = q.Dequeue 1

                    for item in items do
                        do! q.Enqueue [item + "boop"]

                    if items.Length > 0 then
                        lg.Info "Booped some snoots"
                    else
                        lg.Warn "No snoots to boop"
                })

        let getFirstQuery param (req: RawRequest) =
            match req.queries.TryGetValue param with
            | true, values ->
                match values with
                | [] -> None
                | k::_rest -> Some k
            | false, _ ->
                None

        do! routes {
            get "hi" (fun _ -> async {
                let! xs = q.Dequeue 20
                return ok <| sprintf "Got %A" xs
            })

            post "hi" (fun req -> async {
                let text =
                    req.body
                    |> Seq.toArray
                    |> Text.Encoding.UTF8.GetString
                do! q.Enqueue [text]
                return ok "Ok sent"
            })

            get "val" (fun req -> async {
                let key = getFirstQuery "key" req
                match key with
                | Some k ->
                    let! x = map.TryFind k
                    match x with
                    | Some v ->
                        return ok v
                    | None ->
                        return ok <| sprintf "Didn't find that key %s" k
                | None ->
                    return ok "No key in queries"
            })

            post "val" (fun req -> async {
                let key = getFirstQuery "key" req

                match key with
                | Some k ->
                    let body =
                        req.body
                        |> Seq.toArray
                        |> Text.Encoding.UTF8.GetString

                    do! map.Set (k, body)

                    lg.Info <| sprintf "Saved %A to %A" body k

                    return ok <| sprintf "Set %s" k

                | None ->
                    return ok "No key in queries"
            })

            get "chat" (fun req -> async {
                let roomOpt = getFirstQuery "room" req
                match roomOpt with
                | Some room ->
                    let! messagesOpt = chats.TryFind room
                    match messagesOpt with
                    | Some messages ->
                        let formattedMessages =
                            messages
                            |> String.concat "\n"
                            
                        return ok <| sprintf "Got messages:\n%s" formattedMessages
                    | None ->
                        return ok <| sprintf "No messages in room %s" room
                | None ->
                    return ok <| "No room specified"
            })

            post "chat" (fun req -> async {
                let roomOpt = getFirstQuery "room" req
                match roomOpt with
                | Some room ->
                    let messageOpt = getFirstQuery "message" req
                    match messageOpt with
                    | None ->
                        return ok <| "No message specified"
                    | Some message ->
                        let! messagesOpt = chats.TryFind room
                        match messagesOpt with
                        | None ->
                            do! chats.Set(room, [message])
                            return ok <| "Added message"
                        | Some messages ->
                            do! chats.Set(room, messages @ [message])
                            return ok <| "Added message"
                | None ->
                    return ok <| "No room specified"
            })
        }
    }

module AppOp =
    open AppSpec
    open App
    open Kita.Compile.Domains.Routes
    open Kita.Providers.Azure.RunContext
    open Kita.Providers.Azure.Compile

    let provider = AzureProvider("myaznativeapp", "eastus")
    let attachedApp = app |> Operation.attach provider
    let runRouteState withDomain =
        attachedApp |> Routes.Operation.runRoutes routesDomain withDomain

    [<AzureRunModuleFor("myaznativeapp")>]
    type AzureRunner() =
        interface AzureRunModule<AppState> with
            member _.Provider = provider
            member _.RunRouteState x = runRouteState x

    // Does it make more sense to run / launch, and _then_ do work on domains?
    // In the proxy project, if I run routes then run logs, I'd need to remember
    // in the RunApp if I've run or not. But if I've already run, there's no way to
    // access all the blocks again without running again. That means run needs to be
    // idempotent, which it currently is but is not guaranteed to stay that way.

    let launchRouteState withDomain =
        attachedApp |> Routes.Operation.launchRoutes routesDomain withDomain

[<EntryPoint>]
let main argv =
    // NOTE this needs to launch (provision + deploy)
    printfn "Deploying"

    AppOp.launchRouteState (fun routes -> printfn "\n\nApp launched routes: %A" routes)

    0 // return an integer exit code
