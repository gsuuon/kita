namespace Kita.Core.Resources.Collections

open Kita.Core
open Kita.Core.Providers.Default

type CloudMap<'K, 'V> () =
    let activated = false

    member private _.CreateInstance config = ()
    member private _.UpdateInstance config = ()
    member private _.Teardown config = ()

    member _.TryFind key = async { return Unchecked.defaultof<'V> }
    member _.Set(key, item) = ()

    interface CloudResource with
        member _.CBind () = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()

    member _.Deploy (az: Az) =
        printfn "Deploy: Azure Map"

