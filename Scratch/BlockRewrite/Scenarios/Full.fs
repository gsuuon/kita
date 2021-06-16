module BlockRewrite.Scenarios.Full

open BlockRewrite

type FullConfiguration =
    { location : string
      performance : string }

type FullProvider(cfg: FullConfiguration) =
    let mutable requestedResources = List.empty
    // Launch time used to launch resources
    // run time?

    member _.Attach (name: string) =
        requestedResources <- name :: requestedResources
        
    interface Provider with
        member _.Launch () = ()
        member _.Run () = ()

    // I'd prefer if runtime provider is a different type than
    // launch-time provider
    // run-time gets necessary connection info at construction
    // launch-time doesn't need it at all

type FullResource(name: string) =
    member _.Attach (p: FullProvider) =
        p.Attach name
        printfn "Attached %s to Full" name

    interface CloudResource

type RoutedUserState = { routes : Map<string, unit -> unit> }

[<AutoOpen>]
module BindState =
    let updateUser updater (s: BindState<_, RoutedUserState>) =
        { s with user = updater s.user }

    let addRoute route handler =
        updateUser
        <| fun u -> { u with routes = Map.add route handler u.routes }

type BlockRouted< ^P when 'P :> Provider>(name: string) =
    inherit Block<'P, RoutedUserState>(name)

    [<CustomOperation("route", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Route
        (
            Runner runCtx,
                [<ProjectionParameter>]
            getRoute,
                [<ProjectionParameter>]
            getHandler
        ) =
        fun s ->
            let (ctx, _) = runCtx s

            let route = getRoute ctx
            let handler = getHandler ctx

            s
            |> addRoute route handler

module Blocks =
    open Providers
    open Resources

    module PretendPlatform =
        type PretendFullClient() =
            member _.Id x = x

    let block name = BlockRouted<AProvider>(name)

    type SafeConfig = string


    let sketch =
        block "A" {
            let v = 1
            let! attachedSafeResource = SafeResource("config", v)
            let gotValue = attachedSafeResource.Value
            // gotValue = 1
            return ()
        }
        
    type SimpleResource<'a> =
        { request : unit -> 'a }

    type SimpleResources() =
        static member Value (name, value) =
            fun (p: FullProvider) ->
                p.Attach name
                { request = fun () -> value }

        static member Value (_name: string, value) =
            fun (_p: AProvider) -> 
                { request = fun () -> value }

    let fn resource =
        let provider = AProvider()
        resource "hey" 12 provider

    let mainBlock =
        block "main" {
            let! x = AResource()
            (* let! myResource = simpleResource "config" // #Provider -> 'a *)

            route "hey" (fun () -> printfn "Handled hey")

            return ()
        }
