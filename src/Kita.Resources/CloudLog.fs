namespace Kita.Resources

open Kita.Core
open Kita.Providers

type CloudLogFrontend() =
    interface CloudResource

    member _.Info = printfn "%s"
    member _.Warn = printfn "%s"
    member _.Error = printfn "%s"

type CloudLog() = 
    interface ResourceBuilder<Local, CloudLogFrontend> with
        member _.Build _p =
            printfn "Built Local CloudLog"
            CloudLogFrontend()

    interface ResourceBuilder<Azure, CloudLogFrontend> with
        member _.Build _p =
            printfn "Built Azure CloudLog"
            CloudLogFrontend()
