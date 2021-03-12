namespace Kita.Resources.Collections

open System.Collections.Generic

open Kita.Core
open Kita.Resources
open Kita.Providers

type CloudQueue<'T>() =
    let activated = false

    let newMessage = new Event<'T>()

    let mutable provider = None

    [<CLIEvent>]
    member _.NewMessage = newMessage.Publish

    member private _.CreateInstance config =
        newMessage.Trigger(Unchecked.defaultof<'T>)
        ()

    member private _.UpdateInstance config = ()
    member private _.Teardown config = ()

    member _.Enqueue item = async { return () }
    member _.Enqueue(xs: 'T list) = async { return () }

    member _.Dequeue() =
        async { return Unchecked.defaultof<'T> }

    member _.Dequeue count =
        async { return [ Unchecked.defaultof<'T> ] }

    member _.Deploy(provider: Azure) = printfn "Deploy: Azure Queue"
    member _.Deploy(provider: Local) =
        provider.Initialize()

        printfn "Deploy: Local Queue"

    interface CloudResource with
        member _.CBind() = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation (provider': Provider) =
            provider <- Some provider'
