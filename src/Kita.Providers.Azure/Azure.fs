namespace Kita.Providers

open Kita.Core

type Azure() =
    interface Provider with
        member _.Run () =
            printfn "Running Azure"
        member _.Launch () =
            printfn "Launching Azure"

