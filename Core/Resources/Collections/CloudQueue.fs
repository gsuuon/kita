namespace Kita.Core.Resources.Collections

open System.Collections.Generic
open Kita.Core

type CloudQueue<'T> () =
    let activated = false
    
    let newMessage = new Event<'T>()

    [<CLIEvent>]
    member _.NewMessage = newMessage.Publish

    member private _.CreateInstance config =
        newMessage.Trigger(Unchecked.defaultof<'T>)
        ()

    member private _.UpdateInstance config = ()
    member private _.Teardown config = ()

    member _.Enqueue item = ()

    interface CloudResource with
        member _.CBind () = ()
        member _.ReportDesiredState _c = ()
        member _.BeginActivation _c = ()
