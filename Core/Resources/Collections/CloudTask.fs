namespace Kita.Core.Resources.Collections

open System.Threading.Tasks

open Kita.Core
open Kita.Core.Resources

type CloudTask (asyncWork : Async<unit>) =
    new (task : Task) =
        CloudTask(task |> Async.AwaitTask)

    new () =
        CloudTask(async { return () })

    member _.Run () = CloudZero.Instance

    interface CloudResource with
        member _.CBind () = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()
