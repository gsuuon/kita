module AzureApp.App

open System
open Kita.Core
open Kita.Domains
open Kita.Domains.Routes
open Kita.Domains.Routes.Http
open Kita.Domains.Routes.Http.Helpers
open Kita.Resources
open Kita.Resources.Collections

open Kita.Providers.Azure
open Kita.Providers.Azure.Resources.Definition

type AppState =
    { routeState : RouteState }
    static member Empty = { routeState = RouteState.Empty }

type ChatMessage =
    { author : string
      body : string }

let routesDomain = 
    { new UserDomain<_,_> with
        member _.get s = s.routeState
        member _.set s rs = { s with routeState = rs } }

let routes = RoutesBlock<AppState> routesDomain

let app =
    Block<AzureProvider, AppState> "myaznativeapp" {

    // TODO name is thrown away
    // This should be the app name
    // app names must be between 2-60 characters alphanumeric + non-leading hyphen
    let! q = CloudQueue<string>("myaznatq")

        // Naming rules for some common resources
        // 3 - 63 characters
        // alphanumeric + hyphen
        // can't start or end with hyphen
        // no consecutive hyphens
        // lowercase
        // Reference: https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules#microsoftstorage

    let! map = CloudMap<string, string>("myaznatmap")
    let! chats = CloudMap<string, ChatMessage list>("chats-typed1")
    let! webPubSub = AzureWebPubSub("realtimechat")

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

    let readBody (req: RawRequest) =
        req.body
        |> Seq.toArray
        |> Text.Encoding.UTF8.GetString

    do! routes {
        get "hi" (fun _ -> async {
            let! xs = q.Dequeue 20
            return ok <| sprintf "Got %A" xs
        })

        post "hi" (fun req -> async {
            let text = readBody req
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
                let body = readBody req

                do! map.Set (k, body)

                lg.Info <| sprintf "Saved %A to %A" body k

                return ok <| sprintf "Set %s" k

            | None ->
                return ok "No key in queries"
        })

        get "chat1" (fun req -> async {
            let roomOpt = getFirstQuery "room" req
            match roomOpt with
            | Some room ->
                let! messagesOpt = chats.TryFind room
                match messagesOpt with
                | Some messages ->
                    let formattedMessages =
                        messages
                        |> List.map (fun msg -> sprintf "%s: %s" msg.author msg.body)
                        |> String.concat "\n"
                        
                    return ok <| sprintf "Got messages:\n%s" formattedMessages
                | None ->
                    return ok <| sprintf "No messages in room %s" room
            | None ->
                return ok <| "No room specified"
        })

        post "chat1" (fun req -> async {
            let roomOpt = getFirstQuery "room" req
            match roomOpt with
            | Some room ->
                let authorOpt = getFirstQuery "author" req
                match authorOpt with
                | None ->
                    return ok <| "No author specified"
                | Some author ->
                    let! messagesOpt = chats.TryFind room
                    let chatMessage =
                        { author = author
                          body = readBody req }
                        
                    match messagesOpt with
                    | None ->
                        do! chats.Set(room, [chatMessage])
                        return ok <| "Added message"
                    | Some messages ->
                        do! chats.Set(room, messages @ [chatMessage])
                        return ok <| "Added message"
            | None ->
                return ok <| "No room specified"
        })

        get "chat" (fun req -> async {
            let! wpsClient = webPubSub.Client.GetAsync
            match getFirstQuery "userId" req with
            | Some userId ->
                let roomPermissions =
                    match getFirstQuery "rooms" req with
                    | Some rooms ->
                        rooms.Split(" ")
                        |> Array.map (fun room -> [| "webpubsub.joinLeaveGroup."+room;  "webpubsub.sendToGroup."+room |])
                        |> Array.concat
                    | None ->
                        [||]

                let uri = wpsClient.GetClientAccessUri(userId, roomPermissions)
                return ok <| sprintf "Client access uri: %s" uri.AbsoluteUri
            | None ->
                return ok "Missing userId query param"
        })

        post "chat" (fun req -> async {
            let! wpsClient = webPubSub.Client.GetAsync
            let message = readBody req
            wpsClient.SendToAll message |> ignore
            return ok <| sprintf "Sent message to all: %s" message
        })
    }
}
