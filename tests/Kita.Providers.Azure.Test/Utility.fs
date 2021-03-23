module Kita.Utility

type Waiter<'T>() =
    let mutable item : 'T option = None

    let onSet = new Event<'T>()

    [<CLIEvent>]
    member _.OnSet = onSet.Publish
    member _.Set x =
        item <- Some x
        onSet.Trigger x

    member this.Get () =
        async {
            match item with
            | Some x ->
                return x
            | None ->
                return! Async.AwaitEvent this.OnSet
        }
