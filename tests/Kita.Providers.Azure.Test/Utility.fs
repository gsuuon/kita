module Kita.Utility

open System.Threading.Tasks
open FSharp.Control.Tasks

type Task with
    static member AwaitEvent (ev: IEvent<_>) =
        let task = TaskCompletionSource<_>()
        ev.Add <| fun x -> task.SetResult x
        task.Task

type Waiter<'T>() =
        
    let mutable item : 'T option = None

    let onSet = new Event<'T>()

    [<CLIEvent>]
    member _.OnSet = onSet.Publish
    member _.Set x =
        // TODO
        // What should happen if already set? Error?
        item <- Some x
        onSet.Trigger x

    member this.GetAsync =
        async {
            match item with
            | Some x ->
                return x
            | None ->
                return! Async.AwaitEvent this.OnSet
        }

    member this.GetTask =
        task {
            match item with
            | Some x ->
                return x
            | None ->
                return! Task.AwaitEvent this.OnSet
        }

    member this.Follow(waiter: Waiter<'R>, transform: 'R -> 'T) =
        waiter.OnSet.Add <| fun (x: 'R) -> transform x |> this.Set
        this
