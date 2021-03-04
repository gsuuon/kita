namespace Kita.Providersc

open Kita.Providers

type Azure() =
    inherit Provider("Azure.Default")
    member _.Initialize() = printfn "Initialize Azure.Default"

