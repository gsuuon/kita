namespace Kita.Core

open Kita.Core.Providers
open Kita.Core.Resources

type Infra (config: Config) =
    let mutable resources = []

    member _.Bind (m: #CloudResource, f) =
        m.ReportDesiredState config
        resources <- m :> CloudResource :: resources
        f m

    member _.Zero () = ()
        
    member _.Yield x = x

    [<CustomOperation("route")>]
    member _.Route (mx, path, handlers) =
        printfn "%s" path
        handlers
        |> List.iter (printfn "%A")

    [<CustomOperation("logger")>]
    member _.Logger (mx, ()) =
        printfn "%s"

