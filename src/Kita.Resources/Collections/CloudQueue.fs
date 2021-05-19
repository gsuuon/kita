namespace Kita.Resources.Collections

open System.Collections.Generic

open Kita.Core
open Kita.Providers

type CloudQueueFrontend<'T>() =
    let newMessage = new Event<'T>()

    interface CloudResource

    [<CLIEvent>]
    member _.NewMessage = newMessage.Publish
    member _.Enqueue item = async { return () }
    member _.Enqueue(xs: 'T list) = async { return () }

    member _.Dequeue() =
        async { return Unchecked.defaultof<'T> }

    member _.Dequeue count =
        async { return [ Unchecked.defaultof<'T> ] }

type CloudQueue<'T>() =
    interface ResourceBuilder<Local, CloudQueueFrontend<'T>> with
        member _.Build _p =
            printfn "Built Local CloudQueue"
            CloudQueueFrontend()

    interface ResourceBuilder<Azure, CloudQueueFrontend<'T>> with
        member _.Build _p =
            printfn "Built Azure CloudQueue"
            CloudQueueFrontend()
