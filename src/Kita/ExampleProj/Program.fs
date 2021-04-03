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
    let bApp = infra'<BProvider>

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

    let program sayHey = aApp {
        nest bBlock

        nest (gated sayHey bBlock2)

        route "hi" [
            get <| fun _ -> 
                "hello"
                |> ok
                |> asyncReturn
        ]
    }

let launch a b (m: Managed<Provider>) =
    m.provider.Launch(a, b)

[<EntryPoint>]
let main argv =
    Managed.empty()
    |> (App.program false).Attach
    |> launch "hi" "here"

    0
