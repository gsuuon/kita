let fsiTaskWorkaround aTaskCtor = async {
    return! aTaskCtor() |> Async.AwaitTask
}
