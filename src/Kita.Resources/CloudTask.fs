namespace Kita.Resources

open System.Threading.Tasks

open Kita.Core
open Kita.Providers

type ResourceProvider<'Resource, 'a> =
    abstract BuildResource : 'a -> 'Resource

type ResourceBuilder<'Resource, 'a> =
    abstract Build : ResourceProvider<'Resource, 'a> -> 'Resource


module CloudTask =
    type Frontend(asyncWork: Async<unit>) =
        interface CloudResource
        member _.Exec () = ()
        member _.Stop () = ()

type ICloudTask =
    inherit CloudResource
    abstract Exec : unit -> unit
    abstract Stop : unit -> unit

type ProviderActivatedCloudTask(work) =
    interface ICloudTask with
        member _.Exec () = ()
        member _.Stop () = ()
    interface Activate with
        member _.Activate

type FooProvider() =
    member _.QueueProvisionProc work =
        client.requestProvision worker work

    interface ResourceProvider<CloudTask, Async<unit>> with
        member this.Provision (work) =
            this.QueueProvisionProc work

type NCloudTask(work: Async<unit) =
    interface ICloudTask with
        member _.Exec () = ()
        member _.Stop () = ()

    interface ProviderProvision<ResourceProvider<ICloudTask, Async<unit>> with
        member this.Provision provider =
            provider.Provision this

type CloudTask(asyncWork: Async<unit>) =
    interface ResourceBuilder<CloudTask.Frontend, Async<unit>> with
        member _.Build (provider) = provider.BuildResource(asyncWork)

type MyProvider() =
    interface ResourceProvider<CloudTask.Frontend, Async<unit>> with
        member _.BuildResource work =
            // queueProvisionCloudTask()
            CloudTask.Frontend(work)

type MyAddendumProvider() =
    inherit MyProvider()

    interface ResourceProvider<CloudTaskFrontend, Async<bool>> with
        member _.BuildResource work =
            let work' = async {
                let! _x = work
                return ()
            }

            CloudTaskFrontend(work')

let x =
    block "" {
        let! cloudTask = CloudTask(asyncRet ())
        cloudTask.Exec()
        cloudTask.Stop()
    }

type Builder(provider) =
    member _.Bind (
        rb: ResourceBuilder<'Resource, 'a>,
        f
    ) =
    let r = rb.Build provider
