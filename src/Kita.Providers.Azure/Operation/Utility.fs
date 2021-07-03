namespace Kita.Providers.Azure.Utility

open System

module LocalLog =
    let monitor = new Object()

    let reportMessage message =
        lock monitor
            (fun () ->
                Console.WriteLine
                    $"|{Threading.Thread.CurrentThread.Name}| {message}"
            )

    let report format = Printf.ksprintf reportMessage format
