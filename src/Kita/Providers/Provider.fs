namespace Kita.Providers

type Provider =
    abstract member Name : string
    abstract member Initialize : unit -> unit

type ProviderLike< ^Provider
                    when ^Provider :> Provider
                    and ^Provider: (new : unit -> ^Provider)>
                    = ^Provider
