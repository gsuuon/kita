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
        // If i want to just do provider launching, i can add a set of
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
              launch = fun () ->
                attached.provider.Launch()

                managed.nested
                |> Map.iter
                    (fun name nestedAttached -> nestedAttached.launch())

              run = fun () ->
                attached.provider.Run()

                managed.nested
                |> Map.iter
                    (fun name nestedAttached -> nestedAttached.run())

              path = [] // FIXME actually do this
              }

    [<CustomOperation("nest", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Nest
        (
            Runner retCtx,
                [<ProjectionParameter>]
            getNested,
                [<ProjectionParameter>]
            getProvider
        ) : BindState<_> -> 'a * AttachedBlock
        =
        fun s ->
            let (ctx, _) = retCtx s

            let nested = getNested ctx
            let provider = getProvider ctx

            ctx
            , nested
                { provider = provider
                  managed = Managed.Empty }

    [<CustomOperation("child", MaintainsVariableSpaceUsingBind=true)>]
    member inline block.Child
        (
            Runner retCtx,
                // This is a tuple of all variables in space as a tuple
                // wrapped with _.Return
                [<ProjectionParameter>]
            getChild
                // This is a generated fn that takes the variable
                // space tuple and returns the one passed to the operation
        ) : BindState<'Provider> -> 'a * AttachedBlock
        =
        fun s ->
            let (ctx, _) = retCtx s
            let child = getChild ctx

            ctx, child s

    member inline _.Bind
        (
            child: BindState<'Provider> -> 'a * AttachedBlock,
            f
        )
        =
        Runner
        <| fun s ->
            let (ctx, attached) = child s
            let (Runner r) = f ctx

            s
            |> addNested attached
            |> r

[<AutoOpen>]
module Operation =
    let attach (provider: #Provider) block =
        { provider = provider
          managed = Managed.Empty } |> block

    let launchAndRun (attachedBlock: AttachedBlock) =
        attachedBlock.launch()
        attachedBlock.run()

module Providers =
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

module Resources =
    open Providers
    
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

        member _.DoABThing() = printfn "%s did AB thing" name
        interface CloudResource

    type AResource(name: string) =
        new() = AResource("")
        member _.Attach (p: AProvider) =
            report "A" "A" name
        member _.DoAThing() = printfn "%s did A thing" name
        interface CloudResource

    type BResource(name: string) =
        new() = BResource("")
        member _.Attach (p: BProvider) =
            report "B" "B" name
        member _.DoBThing() = printfn "%s did B thing" name
        interface CloudResource
