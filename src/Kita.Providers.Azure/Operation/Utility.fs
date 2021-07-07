namespace Kita.Providers.Azure.Utility

open System
open Microsoft.FSharp.Core.Printf

module LocalLog =
    let allowSecrets =
#if DEBUG
        true
#else
        false
#endif

    let monitor = new Object()

    let reportMessage message =
        lock monitor
            (fun () ->
                Console.WriteLine
                    $"|Thread {Threading.Thread.CurrentThread.ManagedThreadId}| {message}"
            )

    let report format = Printf.ksprintf reportMessage format
    let reportSecret (format: StringFormat<'a, unit>) =
        if allowSecrets then
            Printf.ksprintf reportMessage format
        else
            Printf.ksprintf
            <| fun s -> reportMessage "Secret hidden"
            <| format
