open Kita.Core
open Kita.Core.Infra
open Kita.Core.Http
open Kita.Core.Http.Helpers
open Kita.Compile

let mutable root = Unchecked.defaultof<AttachedBlock>

let sayLaunched = printfn "Launched %s"


type AProvider() =
    interface Provider with
        member _.Launch () =
            sayLaunched "A"

type BProvider() =
    interface Provider with
        member _.Launch () =
            sayLaunched "B"

type CProvider() =
    interface Provider with
        member _.Launch () =
            sayLaunched "C"

type SomeResource() =
    member _.Attach(provider: AProvider) =
        ()

    member _.Attach(provider: BProvider) =
        ()

    interface CloudResource

module App =
    type MainProvider = BProvider

    type Chunk< ^T when ^T :> Provider> = string -> Infra< ^T>

    let bBlock (chunk: Chunk<BProvider>) =
        chunk "inner b" {
            let! x = SomeResource()
            route "hello" []
        }

    let bBlock2 (chunk: Chunk<BProvider>) =
        chunk "other b" {
            let! x = SomeResource()
            route "hey" []
        }

    let aBlock (chunk: Chunk<AProvider>) =
        chunk "top a" {
            let! x = SomeResource()
            route "hi" []
        }

    let coreBlock (chunk: Chunk<MainProvider>) =
        chunk "core" {
            let! x = SomeResource()
            route "/" []
        }

    let root (chunk: Chunk<MainProvider>) aChunk bChunk =
        fun sayHey ->
            chunk "root" {
                nest (bBlock bChunk)

                nest (gated sayHey (bBlock2 bChunk))

                nest (aBlock aChunk)

                route "hi" [
                    get <| fun _ -> 
                        "hello"
                        |> ok
                        |> asyncReturn
                ]
            }

let inline chunked provider name = Infra (name, provider)

let program = 
    let bProvider = BProvider()

    App.root
    <| chunked bProvider
    <| chunked (AProvider())
    <| chunked bProvider
    <| true

[<EntryPoint>]
let main argv =
    let block =
        Managed.empty()
        |> program.Attach

    block
    |> launch "my app" "earth"

    printfn "targetted at say hey"

    block
    |> launchNested "my nested app" "earth" ["say hey"]

    0
