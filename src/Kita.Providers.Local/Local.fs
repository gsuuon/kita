namespace Kita.Providers

open Kita.Core

type Local() =
    interface Provider with
        member _.Run () =
            printfn "Running Local"
        member _.Launch () =
            printfn "Launching Local"


