namespace Kita.Providers

open Kita.Providers

type Local() =
    member _.Initialize() = printfn "Initialize Local.Default"

    interface Provider with
        member _.Name = "Local.Default"
        member this.Initialize() = 
            this.Initialize()


