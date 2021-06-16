module ExampleProj.Program

open Kita.Core
open Kita.Compile
open Kita.Domains

let sayLaunched = printfn "Launched %s"
let noop = fun () -> ()
let retOk = Routes.Http.Helpers.returnOk

module Resources =
    type IValResource<'T> =
        inherit CloudResource
        abstract Get : unit -> 'T
        abstract Set : 'T -> unit
        
    type ValResourceProvider =
        abstract Provide : 'T -> IValResource<'T>

    type ValResource<'T>(x: 'T) =
        member _.Create (p: #ValResourceProvider) =
            p.Provide x


    type ILogResource =
        inherit CloudResource
        abstract Log : string -> unit

    type LogResourceProvider =
        abstract Provide : unit -> ILogResource

    type LogResource() =
        member _.Create (provider: #LogResourceProvider) =
            provider.Provide ()


module Providers =
    open Resources

    type FooProvider() =
        interface Provider with
            member _.Launch () =
                sayLaunched "Foo"
            member _.Run () = ()

        interface ValResourceProvider with
            member _.Provide x =
                let mutable x' = x
                { new IValResource<_> with
                    member _.Get () = x'
                    member _.Set x = x' <- x }

        interface LogResourceProvider with
            member _.Provide () =
                { new ILogResource with
                    member _.Log x = printfn "%s" x }

    type BarProvider() =
        interface Provider with
            member _.Launch () =
                sayLaunched "Bar"
            member _.Run () = ()

        interface LogResourceProvider with
            member _.Provide () =
                { new ILogResource with
                    member _.Log x = printfn "%s" x }


module SimpleScenario =
    open Resources
    open Providers

    let blockA =
        Block<FooProvider, unit> "A" {
            let! x = ValResource 0
            let! logger = LogResource ()
            logger.Log "hey"
            let x1 = x.Get()
            x.Set 1
            let x2 = x.Get()

            return ()
        }


module ExtendResource =
    open Resources
    open Providers

    type IQueueResource<'T> =
        inherit CloudResource
        abstract Queue : 'T -> unit
        abstract Dequeue : unit -> 'T option

    type QueueResourceProvider =
        abstract Provide : unit -> IQueueResource<'T>

    type QueueResource<'T>() =
        member _.Create (provider: QueueResourceProvider) =
            provider.Provide<'T> ()

    type FooQProvider() =
        inherit FooProvider()

        interface QueueResourceProvider with
            member _.Provide<'T> () =
                let mutable q = List.empty

                { new IQueueResource<'T> with
                    member _.Queue x =
                        q <- x :: q
                    member _.Dequeue () =
                        match q with
                        | head :: rest ->
                            q <- rest
                            Some head
                        | [] ->
                            None
                        }

    let blockB =
        Block<FooQProvider, unit> "B" {
            let! x = ValResource 0
            let x1 = x.Get()

            let! logger = LogResource ()

            let! q = QueueResource<char> ()
            let x = q.Dequeue ()

            return ()
        }

module RestrictedProviderScenario =
    // Restrict an add-on to a specific provider
    open Providers
    open Resources
    
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
                |> UserDomain.update<FooProvider, 'U, 'D>
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

    let chunkA (chunk: Chunk<FooProvider>) =
        // restrictedToAProvider add-on breaks if we switch the provider type to BarProvider
        chunk "hey" {
            let! _x = LogResource()

            do! restrictedToAProvider {
                proc noop
            }

            return ()
        }

module NestScenario = 
    open Providers
    open Resources

    open Kita.Domains.Routes
    
    type AppState =
        { routeState : RouteState }
        static member Empty =
            { routeState = RouteState.Empty }

    [<AutoOpen>]
    module private AppState =
        let ``.routeState`` (appState: AppState) =
            appState.routeState

        let ``|routeState`` routeState appState =
            { appState with routeState = routeState }

    let inline block name =
        Block<_, AppState>(name)

    let routesDomain = 
        { new UserDomain<_,_> with
            member _.get s = ``.routeState`` s
            member _.set s rs = ``|routeState`` rs s }

    let routes =
        RoutesBlock<AppState> routesDomain

    let blockA : BlockRunner<FooProvider, AppState> =
        block "blockA" {
            let! x = ValResource 0
            let _y = x.Get()
            do! routes {
                post "blockA hello" retOk
            }
            return ()
        }

    let blockB : BlockRunner<BarProvider, AppState> =
        block "blockB" {
            do! routes {
                post "blockB hello" retOk
            }
            return ()
        }

    let blockC : BlockRunner<FooProvider, AppState> =
        block "blockC" {
            let! _x = LogResource()
            return ()
        }

    let main =
        let bProvider = BarProvider()

        Block<FooProvider, AppState> "main" {
            let! _x = LogResource()

            do! routes {
                post "main hi" retOk
            }

            nest blockB bProvider
            child blockA
            child blockC
        }

    open Kita.Domains

    let launch withRoutes =
        printfn "Starting attach"
        let attached = main |> Operation.attach (FooProvider())
        printfn "Finished attach"
        attached |> Routes.Operation.launchRoutes routesDomain withRoutes
    
[<EntryPoint>]
let main _argv =
    let _routes =
        NestScenario.launch
        <| fun routeState ->
            printfn "Known routes: %A" routeState
            routeState

    0
