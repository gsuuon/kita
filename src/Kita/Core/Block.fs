namespace Kita.Core

type Provider =
        // Needs to be idempotent if many-to-one Block / Provider relationship
    abstract member Launch : unit -> unit
    abstract member Run : unit -> unit

type CloudResource = interface end

type ResourceBuilder<'P, 'A when 'P :> Provider and 'A :> CloudResource> =
    abstract Build : 'P -> 'A

type Managed =
    { resources : CloudResource list
      nested : Map<string, AttachedBlock> }
    static member Empty =
        { resources = []
          nested = Map.empty }
and AttachedBlock =
    { name : string
      managed : Managed
      launch : unit -> unit
      run : unit -> unit
      path : string list }

type BlockBindState<'P, 'U when 'P :> Provider> =
    { provider : 'P
      user : 'U
      managed : Managed }
    static member Provider provider =
        { provider = provider
          user = Unchecked.defaultof<'U>
          managed = Managed.Empty }

type Runner<'P, 'U, 'result when 'P :> Provider> =
    Runner of (BlockBindState<'P, 'U> -> 'result * BlockBindState<'P, 'U>)

module BlockBindState =
    let updateManaged updater state : BlockBindState<_, _> =
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

    let getResources = Runner (fun s -> s.managed.resources, s)

module Runner =
    let inline ret x = Runner (fun s -> x, s)

module AttachedBlock =
    let getNestedByPath (root: AttachedBlock) pathsAll =
        let rec getNested pathsLeft (current: AttachedBlock) =
            match pathsLeft with
            | [] -> current
            | head::rest ->
                match current.managed.nested.TryGetValue head with
                | true, attachedBlock ->
                    getNested rest attachedBlock
                | false, _ ->
                    let pathsTraveled =
                        pathsAll
                        |> List.take (pathsAll.Length - pathsLeft.Length)

                    failwithf
                        "No block found\nLost at: %s\nLooking for: %s"
                            (pathsTraveled |> String.concat ".")
                            (pathsAll |> String.concat ".")

        getNested pathsAll root

type PublicBlockBuilder<'P>(name: string, provider: 'P) =
    // Gets around issues with inline accessing private data and SRTP:
    // error FS0670: This code is not sufficiently generic. The type variable  ^Provider when  ^Provider :> Config and  ^Provider : (new : unit ->  ^Provider) could not be generalized because it would escape its scope
    // error FS1113: The value 'Run' was marked inline but its implementation makes use of an internal or private function which is not sufficiently accessible
    member _.Name = name
    member _.Provider = provider

type BlockBuilder< ^Provider, 'U when 'Provider :> Provider>
    (
        name: string,
        provider: 'Provider
    ) =
    inherit PublicBlockBuilder<'Provider>(name, provider)

    member inline block.Bind
        (
            builder: ResourceBuilder<'Provider, 'A>,
            f
        ) =
        Runner
        <| fun s ->
            let resource = builder.Build block.Provider
                // TODO
                // if i get a compile error that provider is not public
                // use block.Provider
                // else remove PublicBlockBuilder inherit

            let (Runner r) = f resource

            s |> BlockBindState.addResource resource |> r

    member inline _.Return (x) = Runner.ret x
    member inline _.Zero () = Runner.ret ()
    member inline _.Yield x = Runner.ret x
    member inline _.Delay f = f
    member inline block.Run (f) =
        let (Runner r) = f()

        fun s ->
            let (_, attached : BlockBindState<'Provider,'U>) = r s
            let managed = attached.managed

            printfn "Attached %s" block.Name

            { name = block.Name
              managed = managed
              launch = fun () ->
                attached.provider.Launch()

                managed.nested
                |> Map.iter
                    (fun _name nestedAttached -> nestedAttached.launch())

              run = fun () ->
                attached.provider.Run()

                managed.nested
                |> Map.iter
                    (fun _name nestedAttached -> nestedAttached.run())

              path = [] // FIXME actually do this
              }


        (* [<CustomOperation("route", MaintainsVariableSpaceUsingBind = true)>] *)
        (* member inline _.Route *)
        (*     ( *)
        (*         State m, *)
        (*         [<ProjectionParameter>] getPath, *)
        (*         [<ProjectionParameter>] getHandlers *)
        (*     ) = *)
        (*     State *)
        (*     <| fun s -> *)

        (*         let (ctx, s) = m s *)
        (*         let path = getPath ctx *)
        (*         let handlers = getHandlers ctx *)

        (*         print s "Route" path *)

        (*         let pathedHandlers = handlers |> List.map (fun h -> h path) *)

        (*         pathedHandlers *)
        (*         |> List.iter (fun mh -> print s "Handler" mh.handler) *)

        (*         ctx, s |> addRoutes pathedHandlers *)

        (* [<CustomOperation("proc", MaintainsVariableSpaceUsingBind = true)>] *)
        (* member inline _.Proc *)
        (*     // NOTE in this form, it's basically some sugar around a do! *)
        (*     // with ability to put constraints on the creator argument type *)
        (*     // trying to use do! was kind of busted because overload resolution *)
        (*     // doesn't differentiate null type, so I'd have to create *)
        (*     // a wrapper type for null-like resources and wrap everything *)
        (*     // custom operation makes this easier *)
        (*     ( *)
        (*         State m, *)
        (*         [<ProjectionParameter>] getCreator, *)
        (*         [<ProjectionParameter>] getResourceDef *)
        (*     ) = *)
        (*     State *)
        (*     <| fun s -> *)
        (*         print s "Task" "" *)

        (*         let (ctx, s) = m s *)

        (*         let resourceDef = getResourceDef ctx *)
        (*         let creator = getCreator ctx *)

        (*         let resource = creator resourceDef *)

        (*         ctx, s |> addResource resource *)

    [<CustomOperation("nest", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Nest
        (
            Runner retCtx,
                [<ProjectionParameter>]
            getNested,
                [<ProjectionParameter>]
            getProvider
        ) : BlockBindState<_, 'u> -> 'a * AttachedBlock
        =
        fun s ->
            let (ctx, _) = retCtx s

            let nested = getNested ctx
            let provider = getProvider ctx

            ctx, nested (BlockBindState<_,_>.Provider provider)

    [<CustomOperation("child", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Child
        (
            Runner retCtx,
                // This is a tuple of all variables in space as a tuple
                // wrapped with _.Return
                [<ProjectionParameter>]
            getChild
                // This is a generated fn that takes the variable
                // space tuple and returns the one passed to the operation
        ) : BlockBindState<'Provider, _> -> 'a * AttachedBlock
        =
        fun s ->
            let (ctx, _) = retCtx s
            let child = getChild ctx

            ctx, child s

    member inline _.Bind
        (
            child: BlockBindState<'Provider, _> -> 'a * AttachedBlock,
            f
        ) =
        Runner
        <| fun s ->
            let (ctx, attached) = child s
            let (Runner r) = f ctx

            s
            |> BlockBindState.addNested attached
            |> r

    member inline _.Bind
        (
            carry: BlockBindState<'Provider, _> ->
                   BlockBindState<'Provider, _>,
            f
        ) =
        Runner
        <| fun s ->
            let (Runner r) = f()
            carry s |> r

module Block =
    let inline block< ^Provider when 'Provider :> Provider>
        provider
        name
        =
        BlockBuilder< ^Provider, unit>(name, provider)
