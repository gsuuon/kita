namespace Kita.Core.Providers

type Config(name: string) =
    member val name = name
    member _.Initialize() = true

type ConfigLike< ^Config when ^Config :> Config and ^Config: (new :
                                                                  unit ->
                                                                  ^Config)> =
    ^Config

module Default =

    type Local() =
        inherit Config("Local.Default")
        member _.Initialize() = printfn "Initialize Local.Default"

    type Az() =
        inherit Config("Azure.Default")
        member _.Initialize() = printfn "Initialize Azure.Default"

    type Gcp() =
        inherit Config("Gcp.Default")
        member _.Initialize() = printfn "Initialize Gcp.Default"

    type Aws() =
        inherit Config("Aws.Default")
        member _.Initialize() = printfn "Initialize Aws.Default"
