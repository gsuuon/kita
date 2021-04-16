open Kita.Core
open Kita.Core.Infra
open Kita.Core.Http
open Kita.Core.Http.Helpers
open Kita.Compile

let mutable root = Unchecked.defaultof<AttachedBlock>

let sayLaunched name _ = printfn "Launched %s" name

type AProvider() =
    interface Provider with
        member _.Launch(name, loc, block) =
            sayLaunched "A" (name, loc, block)

type BProvider() =
    interface Provider with
        member _.Launch(name, loc, block) =
            sayLaunched "B" (name, loc, block)

module App =
    let aApp = AProvider() |> infra
    let bApp = BProvider() |> infra

    let bBlock = bApp "say hello" {
        route "hello" [
            get <| fun _ -> 
                "hi"
                |> ok
                |> asyncReturn
        ]
    }

    let bBlock2 = bApp "say hey" {
        route "hey" [
            get <| fun _ -> 
                "hey"
                |> ok
                |> asyncReturn
        ]
    }

    let root sayHey = aApp "" {
        nest bBlock

        nest (gated sayHey bBlock2)

        route "hi" [
            get <| fun _ -> 
                "hello"
                |> ok
                |> asyncReturn
        ]
    }

let program = App.root true

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
