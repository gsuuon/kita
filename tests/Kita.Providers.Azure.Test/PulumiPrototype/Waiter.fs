module PulumiPrototype.Waiter

// TODO threadsafety?
type Waiter<'T>() =
    let mutable item : 'T option = None

    let onSet = new Event<'T>()

    member _.Set x =
        item <- Some x
        onSet.Trigger x

    member _.Get () =
        async {
            match item with
            | Some x ->
                return x
            | None ->
                return! Async.AwaitEvent onSet.Publish
        }
