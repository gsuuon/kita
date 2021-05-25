open Kita.Core
open Kita.Compile

let mutable root = Unchecked.defaultof<AttachedBlock>

let sayLaunched = printfn "Launched %s"
let noop = fun () -> ()

type AProvider() =
    interface Provider with
        member _.Launch () =
            sayLaunched "A"
        member _.Run () = ()

type BProvider() =
    interface Provider with
        member _.Launch () =
            sayLaunched "B"
        member _.Run () = ()

type CProvider() =
    interface Provider with
        member _.Launch () =
            sayLaunched "C"
        member _.Run () = ()

type SomeResourceFrontend<'T>(v: 'T) =
    member _.GetThing () = v
    interface CloudResource

type SomeResource<'T>(v) =
    interface ResourceBuilder<AProvider, SomeResourceFrontend<'T>> with
        member _.Build x =
            SomeResourceFrontend(v)

    interface ResourceBuilder<BProvider, SomeResourceFrontend<'T>> with
        member _.Build x =
            SomeResourceFrontend(v)

module RestrictedProviderScenario =
    // Restrict an add-on to a specific provider
    open Kita.Domains
    
    type RestrictedBuilderToAProvider<'U, 'D>(userDomain) =
        // Add-on, borrows proc as example
        inherit DomainBuilder<'U, 'D>(userDomain)
        
        [<CustomOperation("proc", MaintainsVariableSpaceUsingBind=true)>]
        member inline this.Proc
            (
                DomainRunner runCtx,
                [<ProjectionParameter>] getProc
            ) =
            DomainRunner <| fun s ->
                let (ctx, s') = runCtx s
                let proc = getProc ctx

                ctx,
                s'
                |> UserDomain.update<AProvider, 'U, 'D>
                    // Use the provider type parameter to restrict this custom op to AProvider
                    this.UserDomain
                    id

    type NoState = class end
    type NoType() =
        static member Instance =
            NoType()

    let restrictedToAProvider =
        RestrictedBuilderToAProvider<NoState,NoType>
            { new UserDomain<NoState,NoType> with
                member _.get x = NoType.Instance
                member _.set x y = x }

    type AppState = NoState

    type Chunk< ^T when 'T :> Provider> = string -> Block< 'T, AppState>

    let chunkA (chunk: Chunk<AProvider>) =
        // restrictedToAProvider add-on breaks if we switch the provider type to BProvider
        chunk "hey" {
            let! x = SomeResource()

            do! restrictedToAProvider {
                proc noop
            }

            return ()
        }

module NestScenario = 
    open Kita.Domains
    open Kita.Domains.Routes
    
    type AppState =
        { routeState : RouteState }
        static member Empty =
            { routeState = RouteState.Empty }

    let inline block name =
        Block<_, AppState>(name)

    let routes =
        RoutesBlock<AppState>
            { new UserDomain<_,_> with
                member _.get s = s.routeState
                member _.set s rs = { s with routeState = rs } }

    let blockA : BlockRunner<AProvider, AppState> =
        block "blockA" {
            let! x = SomeResource 0
            let y = x.GetThing()
            return ()
        }

    let blockB : BlockRunner<BProvider, AppState> =
        block "blockB" {
            do! routes {
                let x = "hello"
                let y = "hey"
                post x noop
                post y noop
            }
            return ()
        }

    let blockC : BlockRunner<AProvider, AppState> =
        block "blockC" {
            let! _x = SomeResource()
            return ()
        }

    let main =
        let bProvider = BProvider()

        Block<AProvider, AppState> "main" {
            let! _x = SomeResource()

            child blockA
            nest blockB bProvider
            child blockC
        }

    let aProvider = AProvider()

    let app = main |> Operation.attach aProvider

[<EntryPoint>]
let main argv =
    NestScenario.app |> Operation.launchAndRun
    0
