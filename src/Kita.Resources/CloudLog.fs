namespace Kita.Resources

open Kita.Core

type ICloudLog =
    inherit CloudResource
    abstract Info : string -> unit
    abstract Warn : string -> unit
    abstract Error : string -> unit

type CloudLogProvider =
    abstract Provide : unit -> ICloudLog

type CloudLog() =
    member _.Create (p: #CloudLogProvider) =
        p.Provide()
