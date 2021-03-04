namespace Kita.Resources

open Kita.Core
open Kita.Providers

type CloudLog() =
    let activated = false

    member _.Info = printfn "%s"
    member _.Warn = printfn "%s"
    member _.Error = printfn "%s"

    member _.Deploy(cfg: Azure) = printfn "Deploy: Azure Log"
    member _.Deploy(cfg: Local) = printfn "Deploy: Local Log"

    interface CloudResource with
        member _.CBind() = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()
