open Kita.Core
open Kita.Compile
open Kita.Domains

let sayLaunched = printfn "Launched %s"
let noop = fun () -> ()
let retOk = Routes.Http.Helpers.returnOk

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
        member _.Build _x =
            SomeResourceFrontend(v)

    interface ResourceBuilder<BProvider, SomeResourceFrontend<'T>> with
        member _.Build _x =
            SomeResourceFrontend(v)

module RestrictedProviderScenario =
    // Restrict an add-on to a specific provider
    
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
                let _proc = getProc ctx

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
                member _.get _x = NoType.Instance
                member _.set x _y = x }

    type AppState = NoState

    type Chunk< ^T when 'T :> Provider> = string -> Block< 'T, AppState>

    let chunkA (chunk: Chunk<AProvider>) =
        // restrictedToAProvider add-on breaks if we switch the provider type to BProvider
        chunk "hey" {
            let! _x = SomeResource()

            do! restrictedToAProvider {
                proc noop
            }

            return ()
        }

module NestScenario = 
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
            let _y = x.GetThing()
            do! routes {
                post "blockA hello" retOk
            }
            return ()
        }

    let blockB : BlockRunner<BProvider, AppState> =
        block "blockB" {
            do! routes {
                post "blockB hello" retOk
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

            do! routes {
                post "main hi" retOk
            }

            nest blockB bProvider
            child blockA
            child blockC
        }

    let aProvider = AProvider()

    let app = main |> Operation.attach aProvider

    let launch () =
        let routes =
            let mutable knownRoutes = Map.empty
            {|  add = fun routeAddress handler ->
                        knownRoutes <-
                            Map.add routeAddress handler knownRoutes
                show = fun () -> printfn "Known routes: %A" knownRoutes
            |}

        let launchRoutes (routeState: RouteState) =
            routeState.routes
            |> Map.iter routes.add

        app |> Operation.launchAndRun ( (fun s -> s.routeState) >> launchRoutes)

        routes
        

[<EntryPoint>]
let main _argv =
    let knownRoutes = NestScenario.launch()
    knownRoutes.show()
    0
