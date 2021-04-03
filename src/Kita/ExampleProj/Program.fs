open Kita.Core
open Kita.Core.Infra
open Kita.Core.Http
open Kita.Core.Http.Helpers

type AProvider() =
    interface Provider with
        member _.Name = "AProvider"
        member _.Launch(name, loc) =
            printfn "Launched A"

type BProvider() =
    interface Provider with
        member _.Name = "BProvider"
        member _.Launch(name, loc) =
            printfn "Launched B"

module App =
    let aApp = infra'<AProvider> "myaapp"
    let bApp = infra'<BProvider> "mybapp"

    let bBlock = bApp {
        route "hello" [
            get <| fun _ -> 
                "hi"
                |> ok
                |> asyncReturn
        ]
    }

    let program = aApp {
        nest bBlock

        route "hi" [
            get <| fun _ -> 
                "hello"
                |> ok
                |> asyncReturn
        ]
    }

let launch a b (p: Provider) =
    p.Launch(a, b)

[<EntryPoint>]
let main argv =
    Managed.empty()
    |> App.program.Attach
    |> launch "hi" "here"

    0
