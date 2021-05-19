namespace Kita.Resources.Collections

open Kita.Core
open Kita.Providers

type CloudMapFrontend<'K, 'V>() =
    interface CloudResource
    member _.TryFind key =
        async { return Unchecked.defaultof<'V> }
    member _.Set(key, item) = ()
    
type CloudMap<'K, 'V>() =
    interface ResourceBuilder<Azure, CloudMapFrontend<'K, 'V>> with
        member _.Build _p =
            printfn "Built Azure CloudMap"
            CloudMapFrontend()

