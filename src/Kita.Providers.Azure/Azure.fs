namespace Kita.Providers

open Kita.Providers

type Azure() =
    interface Provider with
        member _.Name = "Azure.Default"
        member _.Initialize() = printfn "Initialize Azure.Default"

