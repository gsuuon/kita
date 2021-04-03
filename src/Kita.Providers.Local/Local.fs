namespace Kita.Providers

open Kita.Core

type Local() =
    member _.Initialize() = printfn "Initialize Local.Default"

    interface Provider with
        member _.Name = "Local.Default"
        member _.Launch(name, loc) =
            printfn "Launching Local.Default"


