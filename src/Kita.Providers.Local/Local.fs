namespace Kita.Providers

open Kita.Core.Providers

type Local() =
    inherit Provider("Local.Default")
    member _.Initialize() = printfn "Initialize Local.Default"
