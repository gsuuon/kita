open Kita.Core
open Kita.Core.Infra
open Kita.Core.Http
open Kita.Core.Http.Helpers
open Kita.Compile

let mutable root = Unchecked.defaultof<AttachedBlock>

let launch prov (name, loc, block) =
    let path = 
        try
            Reflect.getStaticAccessPath block
        with e ->
            sprintf "Couldn't get path: %A" e

    printfn "Launched %s: %s" prov path

type AProvider() =
    interface Provider with
        member _.Launch(name, loc, block) =
            launch "A" (name, loc, block)

type BProvider() =
    interface Provider with
        member _.Launch(name, loc, block) =
            launch "B" (name, loc, block)

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

    let root sayHey = aApp {
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
    root <- program.Attach(Managed.empty())
    root.launch("my app", "earth")

    0
