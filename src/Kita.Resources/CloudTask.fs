namespace Kita.Resources

open System.Threading.Tasks

open Kita.Core
open Kita.Resources
open Kita.Providers

type CloudTask(asyncWork: Async<unit>) =
    member _.Attach(az: Azure) =
        printfn "Attach: Azure Task as Function"

    interface CloudResource with
        member _.CBind() = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()
