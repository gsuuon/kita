namespace Kita.Core.Providers

type Provider(name: string) =
    member val name = name
    member _.Initialize() = true

type ProviderLike< ^Provider
                    when ^Provider :> Provider
                    and ^Provider: (new : unit -> ^Provider)>
                    = ^Provider

module Default =
    type Local() =
        inherit Provider("Local.Default")
        member _.Initialize() = printfn "Initialize Local.Default"

    type Az() =
        inherit Provider("Azure.Default")
        member _.Initialize() = printfn "Initialize Azure.Default"

    type Gcp() =
        inherit Provider("Gcp.Default")
        member _.Initialize() = printfn "Initialize Gcp.Default"

    type Aws() =
        inherit Provider("Aws.Default")
        member _.Initialize() = printfn "Initialize Aws.Default"
