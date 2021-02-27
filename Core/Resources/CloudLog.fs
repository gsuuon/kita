namespace Kita.Core.Resources

open Kita.Core

type CloudLog() =
    let activated = false

    member _.Info = printfn "%s"
    member _.Warn = printfn "%s"
    member _.Error = printfn "%s"

    interface CloudResource with
        member _.CBind () = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()
