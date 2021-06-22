module ExampleProj.ResourceAPIRewriteExample

open Kita.Core

// Frontend contract
type IValResource<'T> =
    inherit CloudResource
    abstract Get : unit -> 'T
    abstract Set : 'T -> unit

// Backend constructor contract
type ValResourceProvider =
    abstract Provide : 'T -> IValResource<'T>

// Captures constructor argument
type ValResource<'T>(v: 'T) =
    member _.Create (provider: #ValResourceProvider) =
        provider.Provide v


type ILogResource =
    inherit CloudResource
    abstract Log : string -> unit

type LogResourceProvider =
    abstract Provide : unit -> ILogResource

type LogResource() =
    member _.Create (provider: #LogResourceProvider) =
        provider.Provide ()



type FooProvider() =
    interface Provider with
        member _.Activate () = ()
        member _.Launch () = async { return () }

    interface ValResourceProvider with
        member _.Provide arg =
            let mutable v = arg
            { new IValResource<'T> with
                member _.Get () = v
                member _.Set x = v <- x }

    interface LogResourceProvider with
        member _.Provide () =
            { new ILogResource with member _.Log x = printfn "%s" x }


let inline create< ^p, ^rc, ^r when 'rc : (member Create : 'p -> 'r)>
    (resBuilder: 'rc)
    (provider: 'p) : 'r
    =
    let resource = (^rc : (member Create: 'p -> 'r )(resBuilder, provider))

    resource

let tryCall () =
    let provider = FooProvider()

    let valBuilder = ValResource 0
    let logBuilder = LogResource ()

    let v = valBuilder.Create provider
    let l = logBuilder.Create provider

    let v' = create valBuilder provider
    let l' = create logBuilder provider

    ()


type PublicX< 'T>(x: 'T) =
    member _.X = x

type ResourceBlock< ^Provider>(provider: 'Provider) =
    inherit PublicX<'Provider>(provider)

    member inline this.Bind< ^rc, ^r when
                                    'rc : (member Create : 'Provider -> 'r)>
        ( rc: 'rc, f)
        =
        let resource = create rc this.X
        f resource

    member inline _.Return x = x


let blockA =
    ResourceBlock(FooProvider()) {
        let! x = ValResource 0

        x.Set 1
        let x' = x.Get

        let! y = LogResource()

        y.Log "Hi dere"

        return x'
    }

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
    ResourceBlock(FooQProvider()) {
        let! x = ValResource 0
        x.Set 1
        let x' = x.Get

        let! y = LogResource()

        y.Log "Hi dere"

        let! z = QueueResource<char> ()

        return x'
    }

