namespace Kita.Resources.Collections

open Kita.Core

type ICloudMap<'K, 'V> =
    inherit CloudResource
    abstract TryFind : 'K -> 'V option
    abstract Set : ('K * 'V) -> unit

type CloudMapProvider =
    abstract Provide<'K, 'V> : string -> ICloudMap<'K, 'V>

type CloudMap<'K, 'V>(name) =
    member _.Create (p: #CloudMapProvider) =
        p.Provide<'K, 'V> name
