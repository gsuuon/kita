namespace Kita.Resources

open System.Threading.Tasks

open Kita.Core
open Kita.Resources
open Kita.Providers

type CloudTask(asyncWork: Async<unit>) =
    member _.Deploy(az: Azure) =
        printfn "Deploy: Azure Task as Function"

    interface CloudResource with
        member _.CBind() = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()
