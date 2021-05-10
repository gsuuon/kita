module BlockRewrite.Scenarios.Full

open BlockRewrite

type FullConfiguration =
    { location : string
      performance : string }

type FullProvider(cfg: FullConfiguration) =
    interface Provider with
        member _.Launch () = ()
        member _.Run () = ()

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

    let block name = BlockRouted<AProvider>(name)

    let mainBlock =
        block "main" {
            let! x = AResource()

            route "hey" (fun () -> printfn "Handled hey")

            return ()
        }
