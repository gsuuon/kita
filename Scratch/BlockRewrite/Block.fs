namespace BlockRewrite

type Provider =
        // Needs to be idempotent if many-to-one Block / Provider relationship
    abstract member Launch : unit -> unit
    abstract member Run : unit -> unit

type CloudResource = interface end

type MethodHandler = interface end

type BasicResource() = interface CloudResource

type ProviderLauncher =
    abstract Launch : unit -> unit

type Managed =
    { resources : CloudResource list
      handlers: MethodHandler list
      nested : Map<string, AttachedBlock>
        // TODO-next
        // does this work to allow me to launch/run any node in tree?
        // NOTE
        // If I launch/run per provider, then providers that are used for
        // multiple blocks will run all those blocks
        // I don't think I want this, but maybe it's okay?
        // problems:
        // can't target a specific block to run
        //  -- but when would i need to do that?
        //  -- i'm running root, i want all children to run, eventually
        //  -- only in the context of a specific child would i want to run
        //  -- just that child. but if each context is actually provider
        //  -- specific rather than child specific, (which i think makes
        //  -- sense) then it's not necessary to ever launch / run a
        //  -- specific node / managed
        // If i want to just to provider launching, i can add a set of
        // providers to BindState, and add to that set as I add child/nest
        // then the top level run exposes a way to launch / run the set
        // order shouldn't matter, everything should communicate async
        // (as in, nothing expects that anything else is already
        // provisioned or deployed, or even available)
      }
      static member Empty = // FIXME easy to get launch/run mixed up, same signature
        { resources = []
          handlers = []
          nested = Map.empty }
and AttachedBlock =
    { name : string
      managed : Managed
      launch : unit -> unit
      run : unit -> unit
      path : string list }

type BindState<'T when 'T :> Provider> =
    { provider : 'T
      managed : Managed }

type Runner<'T, 'result when 'T :> Provider> =
    Runner of (BindState<'T> -> 'result * BindState<'T>)

[<AutoOpen>]
module BindState =
    let updateManaged updater state : BindState<_> =
        { state with managed = updater state.managed }

    let addResource resource =
        updateManaged
        <| fun managed ->
            { managed with
                resources =
                    resource :> CloudResource :: managed.resources }

    let addNested nested =
        updateManaged
        <| fun managed ->
            { managed with
                nested = Map.add nested.name nested managed.nested }

type PublicBlock<'P>(name: string) =
    member _.Name = name

type Block< ^Provider when 'Provider :> Provider>(name: string) =
    inherit PublicBlock<'Provider>(name)

    member inline _.Bind
        (
            resource:
                ^R when 'R: (member Attach : 'Provider -> unit)
                    and 'R :> CloudResource
                ,
            f
        ) =
        Runner
        <| fun s ->
            let (Runner m) = f resource

            ( ^R : (member Attach : ^Provider -> unit) (resource, s.provider))

            s |> addResource resource |> m

    member inline _.Return (x) = Runner (fun s -> x, s)

    member inline _.Zero () = Runner (fun s -> (), s)

    member inline _.Delay (f) = f
    member inline block.Run (f) =
        let (Runner runner) = f()


        fun s ->
            let (_, attached : BindState<'Provider>) = runner s
            let managed = attached.managed

            printfn "Attached %s" block.Name

            { name = block.Name
              managed = managed
              launch = fun () -> attached.provider.Launch()
              run = fun () -> attached.provider.Run()
              path = [] // FIXME actually do this
              }

    [<CustomOperation("nest", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Nest
        (
            ctx,
                [<ProjectionParameter>]
            getNested,
                [<ProjectionParameter>]
            getProvider
        ) : 'a -> AttachedBlock
        =
        fun _ ->
            let nested = getNested ctx
            let provider = getProvider ctx

            nested
                { provider = provider
                  managed = Managed.Empty }

    [<CustomOperation("child", MaintainsVariableSpaceUsingBind=true)>]
    member inline block.Child
        (
            ctx,
                [<ProjectionParameter>]
            getChild
        ) : BindState<'Provider> -> AttachedBlock
        =
        fun s ->
            let (Runner r) = ctx
            let child = getChild r

            let (x, s') = r s

            child s'

    member inline _.Bind
        (

            child: BindState<'Provider> -> AttachedBlock,
            f
        )
        =
        Runner
        <| fun s ->
            let (Runner r) = f ()
            let attached = child s

            s
            |> addNested attached
            |> r

type AProvider() =
    member _.Name = "A"
    interface Provider with
        member this.Launch () = printfn "Launched %s" this.Name
        member this.Run () = printfn "Ran %s" this.Name

type BProvider() =
    member _.Name = "B"
    interface Provider with
        member this.Launch () = printfn "Launched %s" this.Name
        member this.Run () = printfn "Ran %s" this.Name

[<AutoOpen>]
module Operation =
    let attach (provider: #Provider) block =
        { provider = provider
          managed = Managed.Empty } |> block

module Program =
    [<AutoOpen>]
    module Resources =
        let report resource provider name =
            let endText =
                match name with
                | "" -> ""
                | n -> " - " + n

            printfn "Attached %s to %s%s" resource provider endText

        type ABResource(name: string) =
            new() = ABResource("")
            member _.Attach (p: AProvider) =
                report "AB" "A" name
            member _.Attach (p: BProvider) =
                report "AB" "B" name
            interface CloudResource

        type AResource(name: string) =
            new() = AResource("")
            member _.Attach (p: AProvider) =
                report "A" "A" name
            interface CloudResource

        type BResource(name: string) =
            new() = BResource("")
            member _.Attach (p: BProvider) =
                report "B" "B" name
            interface CloudResource
            
    module SimpleScenario =
        let mainProvider name = Block<AProvider>(name)
        
        let leafBlock =
            mainProvider "leaf" {
                let! x = ABResource()
                return ()
            }

        let rootBlock =
            mainProvider "root" {
                let! x = ABResource()
                let! y = AResource()
                return ()
            }

        let go () =
            let aProvider = AProvider()

            let attachedRoot = rootBlock |> attach aProvider
            let attachedLeaf = leafBlock |> attach aProvider 
            ()

    module NestedScenario =
        let mainProvider name = Block<AProvider>(name)

        module SameProviderScenario =
            let blockInner =
                mainProvider "inner" {
                    let! x = AResource("three")
                    return ()
                }

            let blockOuter =
                mainProvider "outer" {
                    let! x = ABResource("one")
                    printfn "hi"
                    child blockInner
                    let! y = ABResource("two")
                    printfn "bye"
                    return ()
                }

            let go () =
                blockOuter |> attach (AProvider())

        module DifferentProvidersScenario =
            let blockInner =
                Block<BProvider> "inner" {
                    let! x = BResource()
                    return ()
                }

            let blockOuter =
                let bProvider = BProvider()

                mainProvider "outer" {
                    let! x = AResource()
                    nest blockInner bProvider
                    return ()
                }

            let go () =
                blockOuter |> attach (AProvider())
