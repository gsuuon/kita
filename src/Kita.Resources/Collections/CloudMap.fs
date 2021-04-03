namespace Kita.Resources.Collections

open Kita.Core
open Kita.Providers

type CloudMap<'K, 'V>() =
    let activated = false

    member private _.CreateInstance config = ()
    member private _.UpdateInstance config = ()
    member private _.Teardown config = ()

    member _.TryFind key =
        async { return Unchecked.defaultof<'V> }
    member _.Set(key, item) = ()
    member _.Attach(az: Azure) = printfn "Attach: Azure Map"

    interface CloudResource
