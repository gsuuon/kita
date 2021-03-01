namespace Kita.Core.Resources

open System.Threading.Tasks

open Kita.Core
open Kita.Core.Resources

type CloudTask (asyncWork : Async<unit>) =
    interface CloudResource with
        member _.CBind () = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()
