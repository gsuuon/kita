module PulumiPrototype.Program

open Kita.Core
open PulumiPrototype.Test.Deploy
open PulumiPrototype.Test.Queue

[<EntryPoint>]
let main _argv =
    Managed.empty()
    |> useQueue
    |> deploy
    |> Async.RunSynchronously

    0
