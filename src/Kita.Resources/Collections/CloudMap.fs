namespace Kita.Resources.Collections

open Kita.Core
open Kita.Resources
open Kita.Providers

type CloudMap<'K, 'V>() =
    let activated = false

    member private _.CreateInstance config = ()
    member private _.UpdateInstance config = ()
    member private _.Teardown config = ()

    member _.TryFind key =
        async { return Unchecked.defaultof<'V> }

    member _.Set(key, item) = ()

    interface CloudResource with
        member _.CBind() = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()

    member _.Deploy(az: Azure) = printfn "Deploy: Azure Map"
