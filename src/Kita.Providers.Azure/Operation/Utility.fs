namespace Kita.Providers.Azure.Utility

open System

module LocalLog =
    let monitor = new Object()

    let reportMessage message =
        lock monitor
            (fun () ->
                Console.WriteLine
                    $"|Thread {Threading.Thread.CurrentThread.ManagedThreadId}| {message}"
            )

    let report format = Printf.ksprintf reportMessage format
