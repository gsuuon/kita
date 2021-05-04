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
      path : string list
      nested : Map<string, Managed>
      }
      static member Empty =
        { resources = []
          handlers = []
          nested = Map.empty
          path = List.empty }

type BindState<'T when 'T :> Provider> =
    { provider : 'T
      managed : Managed }

type Runner<'T, 'result when 'T :> Provider> =
    Runner of (BindState<'T> -> 'result * BindState<'T>)

[<AutoOpen>]
module BindState =
    let addResource (resource: #CloudResource) state =
        { state with
            managed =
                { state.managed with
                      resources =
                        resource :> CloudResource
                            :: state.managed.resources } }

type AttachedBlock =
    { name : string
      state : Managed
      launch : unit -> unit
      run : unit -> unit
      nested : Map<string, AttachedBlock> }

type PublicBlock<'P>(name: string) =
    member _.Name = name

type Block< ^Provider when 'Provider :> Provider>(name: string) =
    inherit PublicBlock<'Provider>(name)

    member inline block.Bind
        (
            resource: ^R when ^R: (member Attach : 'Provider -> unit),
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
    member inline _.Run (f) =
        let (Runner runner) = f()

        fun s ->
            let (_, attached) = runner s
            attached

    [<CustomOperation("nest", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Nest
        (
            ctx,
                [<ProjectionParameter>]
            getNested
        ) =
        Runner
        <| fun s ->
            let nested = getNested ctx

            nested

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
    let run provider block =
        let mutable managed = Managed.Empty

        { provider = provider
          managed = managed } |> block

module Program =
    [<AutoOpen>]
    module Resources =
        type ABResource() =
            member _.Attach (p: AProvider) =
                printfn "Attached t34n"
            member _.Attach (p: BProvider) =
                printfn "Attached f0wa"
            interface CloudResource

        type AResource() =
            member _.Attach (p: AProvider) =
                printfn "Attached qr23"
            interface CloudResource

        type BResource() =
            member _.Attach (p: BProvider) =
                printfn "Attached aa9e"
            interface CloudResource
            
    let mainProvider name = Block<AProvider>(name)
    
    let leafBlock =
        mainProvider "leaf" {
            let! x = ABResource()
            return ()
        }

    let rootBlock =
        mainProvider "root" {
            let! x = ABResource()
            return ()
        }

    let go =
        let aProvider = AProvider()

        rootBlock |> run aProvider |> ignore
        leafBlock |> run aProvider 
