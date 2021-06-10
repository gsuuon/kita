namespace Kita.Resources

open Kita.Core

type Logger =
    abstract Info : string -> unit
    abstract Warn : string -> unit
    abstract Error : string -> unit
    
type ICloudLog =
    inherit CloudResource
    inherit Logger

type CloudLogProvider =
    abstract Provide : unit -> ICloudLog

type CloudLog() =
    member _.Create (p: #CloudLogProvider) =
        p.Provide()
