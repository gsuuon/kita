namespace Kita.Resources

open Kita.Core

type ICloudTask =
    inherit CloudResource
    abstract Exec : unit -> unit
    abstract Stop : unit -> unit

type CloudTaskProvider =
    abstract Provide : Async<unit> -> ICloudTask

type CloudTask(work: Async<unit>) =
    member _.Create (provider: #CloudTaskProvider) =
        provider.Provide work
