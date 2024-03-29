module AzureApp.App
#nowarn "0760"
    // FS0760: It is recommended that objects supporting the IDisposable interface are created using the syntax 'new Type(args)', rather than 'Type(args)' or 'Type' as a function value representing the constructor, to indicate that resources may be owned by the generated value
    // `use` x = db.GetContext()

open System
open FSharp.Control.Tasks

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
    { routeState : RouteState
      authedRouteState : RouteState
    }
    static member Empty =
        { routeState = RouteState.Empty
          authedRouteState = RouteState.Empty
        }

let authedRoutesDomain =
    { new UserDomain<_,_> with
        member _.get s = s.authedRouteState
        member _.set s rs = { s with authedRouteState = rs } }

let authedRoutes =
    RoutesBlock<AppState> authedRoutesDomain

let routesDomain =
    { new UserDomain<_,_> with
        member _.get s = s.routeState
        member _.set s rs = { s with routeState = rs } }

let routes =
    RoutesBlock<AppState> routesDomain

type ChatMessage =
    { author : string
      body : string }

type User =
    { userId : string
      permittedRooms : string list }

let roomsToPermissions roomList = seq {
    for room in roomList do
        yield "webpubsub.joinLeaveGroup."+room
        yield "webpubsub.sendToGroup."+room
}

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

open AzureApp.DbModel
open EntityFrameworkCore.FSharp.Extensions
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.SqlServer


let app =
    Block<AzureProvider, AppState> "chat-app" {
        // App names must be between 2-60 characters alphanumeric + non-leading hyphen

        // Naming rules for some common resources
        // 3 - 63 characters
        // alphanumeric + hyphen
        // can't start or end with hyphen
        // no consecutive hyphens
        // lowercase
        // Reference: https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules#microsoftstorage

    let! userPermissions = CloudMap<string, string list>("permissions")
    let! activeUsers = CloudQueue<string>("active-users")
        // On request client, add active user
        // on task, check all users - if user Id still exists, add back to queue
    let! roomUsers = CloudCache<string, string list>("active-rooms")
    let! lastActive = CloudMap<string, DateTime>("users-last-active")
    let! webPubSub = AzureWebPubSub("realtime")

    let! sqlServer = AzureDatabaseSQL("kita-test-db", ApplicationDbContext)

    let! lg = CloudLog()

    let addUserToRoom userId roomId = async {
        // Could check permissions first

        let addPubSub =
            task {
                let! wpsClient = webPubSub.Client.GetTask
                let! x = wpsClient.AddUserToGroupAsync(roomId, userId)
                
                return x
            }

        lastActive.Set (userId, DateTime.UtcNow) |> Async.Start

        match! roomUsers.TryFind roomId with
        | Some users ->
            roomUsers.Set (roomId, userId :: users) |> Async.Start
        | None ->
            roomUsers.Set (roomId, [userId]) |> Async.Start

        // Not waiting for anything to finish

        return ()
    }

    do! routes {
        get "chat" (fun req -> async {
            let! wpsClient = webPubSub.Client.GetAsync
            match getFirstQuery "userId" req with
            | Some userId ->
                let roomPermissions =
                    match getFirstQuery "rooms" req with
                    | Some rooms ->
                        rooms.Split(" ")
                        |> roomsToPermissions
                        |> Seq.toArray
                    | None ->
                        [||]

                let uri = wpsClient.GetClientAccessUri(userId, roomPermissions)
                activeUsers.Enqueue [userId] |> Async.Start
                return okf "Client access uri: %s" uri.AbsoluteUri
            | None ->
                return ok "Missing userId query param"
        })

        post "chat-add" (fun req -> async {
            match getFirstQuery "userId" req with
            | Some userId ->
                match getFirstQuery "roomId" req with
                | Some roomId ->
                    do! addUserToRoom userId roomId

                    return okf "Added %s to %s" userId roomId
                | None ->
                    return ok "Missing roomId query param"
            | None ->
                return ok "Missing userId query param"
        })

        get "chat-room-users" (fun req -> async {
            match getFirstQuery "roomId" req with
            | None -> 
                return ok "Missing roomId query param"
            | Some roomId ->
                let! usersOpt = roomUsers.TryFind roomId
                match usersOpt with
                | Some users ->
                    return okf "Found %i users: %s" users.Length (users |> String.concat ", ")
                | None ->
                    return ok "Room hasn't been used"
        })

        get "user" (fun req -> async {
            match getFirstQuery "userName" req with
            | None ->
                return ok "Missing userName query param"
            | Some userName ->
                let cred = Resources.Operation.AzureIdentityToken.credential
                printf "using credential: %A" cred

                try
                    printf "trying to get context"
                    use db = sqlServer.GetContext()
                    printf "got context, trying query"

                    let userFound =
                        query {
                            for user in db.Users do
                            where (user.name = userName)
                            select user
                        } |> Seq.tryHead
                    printf "got query"

                    match userFound with
                    | None ->
                        return okf "Didn't find user named %s" userName
                    | Some user ->
                        return
                            okf "Found user %s with bio: %A"
                                user.name user.bio
                with error ->

                    return okf "Failed credential %A\n%A" cred error
        })
        
        get "version" (fun _ -> async {
            return
                Reflection
                    .Assembly
                    .GetExecutingAssembly()
                    .GetName()
                    .Version
                |> okf "Version: %A"
        })
    }

    do! authedRoutes {
        post "chat-admin" (fun req -> async {
            let! wpsClient = webPubSub.Client.GetAsync
            let message = readBody req
            wpsClient.SendToAll message |> ignore
            return okf "Sent message to all: %s" message
        })
    }
}
