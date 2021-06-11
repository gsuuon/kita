namespace Kita.Resources

open Kita.Core

type ICloudTask =
    inherit CloudResource
    abstract Chron : string
    abstract Work : (unit -> Async<unit>)

type CloudTaskProvider =
    abstract Provide : string * (unit -> Async<unit>) -> ICloudTask

/// Use https://crontab.guru/ to check chron schedule expression format.
// Not worth a dependency IMO.
type CloudTask(chronSchExp: string, work: unit -> Async<unit>) =
    member _.Create (provider: #CloudTaskProvider) =
        provider.Provide (chronSchExp, work)
