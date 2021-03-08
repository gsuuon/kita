// Exploring issues with FSI and tasks

// These were closed as "by design" so won't be fixed
// https://github.com/dotnet/fsharp/issues/5885
// https://github.com/dotnet/fsharp/issues/4354#issuecomment-365681192

#r "nuget: Ply, 0.3.1"
open FSharp.Control.Tasks.Affine

let wc = new System.Net.WebClient()

let getYt () = task {
    printfn "Getting yt"

    let! yt = wc.DownloadStringTaskAsync (System.Uri "https://www.youtube.com")
    printfn "Got string: %i" yt.Length

    return yt
}

let getGoogle () = task {
    printfn "Getting goog"

    let! theGoog = wc.DownloadStringTaskAsync (System.Uri "https://www.google.com")
    printfn "Got string: %i" theGoog.Length

    return theGoog
}

let getSiteSizes () = task {
    let! goog = getGoogle()
    let! yt = getYt()

    let size = goog.Length + yt.Length
    printfn "Got sizes: %i" size

    return size
}

// Blocks forever
(* getGoogle().Wait() *)

// Works
(* async { return! getGoogle() |> Async.AwaitTask } *)
(* |> Async.RunSynchronously *)

// Blocks forever
(* let wrapWorkaround aTask = *)
(*     async { return! aTask |> Async.AwaitTask } *)
(* in *)
(*     wrapWorkaround (getGoogle()) *)
(*     |> Async.RunSynchronously *)

// Works
(* async { return! getSiteSizes() |> Async.AwaitTask } *)
(* |> Async.RunSynchronously *)

// Works
let wrapWorkaround aTaskCtor =
    async { return! aTaskCtor() |> Async.AwaitTask }
in
    wrapWorkaround getGoogle
    |> Async.RunSynchronously
