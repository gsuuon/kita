namespace Kita.Resources

open System.Threading.Tasks

open Kita.Core
open Kita.Providers

type CloudTaskFrontend(asyncWork: Async<unit>) =
    interface CloudResource
    
type CloudTask(asyncWork: Async<unit>) =
    interface ResourceBuilder<Azure, CloudTaskFrontend> with
        member _.Build _p =
            printfn "Built Azure CloudTask"
            CloudTaskFrontend(asyncWork)
