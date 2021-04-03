namespace Kita.Providers

open Kita.Core

type Azure() =
    interface Provider with
        member _.Name = "Azure.Default"
        member _.Launch(name, loc) =
            printfn "Launching Azure.Default"

