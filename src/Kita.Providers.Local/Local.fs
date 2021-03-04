namespace Kita.Providers

open Kita.Providers

type Local() =
    inherit Provider("Local.Default")
    member _.Initialize() = printfn "Initialize Local.Default"
