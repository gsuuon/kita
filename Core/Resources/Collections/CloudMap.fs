namespace Kita.Core.Resources.Collections

open Kita.Core

type CloudMap<'T> () =
    let activated = false

    member private _.CreateInstance config = ()
    member private _.UpdateInstance config = ()
    member private _.Teardown config = ()

    member _.TryFind key = ()
    member _.Set(key, item) = ()

    interface CloudResource with
        member _.CBind () = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()


