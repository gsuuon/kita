namespace Kita.Resources.Collections

open Kita.Core

type ICloudQueue<'T> =
    inherit CloudResource
    abstract Enqueue : 'T list -> Async<unit>
    abstract Dequeue : int -> Async<'T list>

type CloudQueueProvider =
    abstract Provide<'T> : string -> ICloudQueue<'T>

type CloudQueue<'T>(name) =
    member _.Create (p: #CloudQueueProvider) =
        p.Provide<'T> name
