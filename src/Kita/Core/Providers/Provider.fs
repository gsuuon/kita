namespace Kita.Core.Providers

type Provider(name: string) =
    member val name = name
    member _.Initialize() = true

type ProviderLike< ^Provider
                    when ^Provider :> Provider
                    and ^Provider: (new : unit -> ^Provider)>
                    = ^Provider
