module PulumiPrototype.Test.Deploy

open Kita.Providers
open Kita.Core
open Kita.Core.Http
open Kita.Core.Http.Helpers

let deploy (managed: Managed<PulumiAzure>) = async {
    do! managed.provider.Initialize "mykitaapp2"
    let! _ = managed.provider.RunDependents()

    let! response =
        match managed.handlers with
        | (_route, handler)::_rest ->
            match handler with
            | POST h ->
                h {
                    body = "hello"
                    queries = dict []
                    headers = []
                    cookies = []
                }
            | GET h ->
                h {
                    body = ""
                    queries = dict []
                    headers = []
                    cookies = []
                }
        | _ ->
            ok "welp"
            |> asyncReturn

    printfn "Got response: %s" response.body

    printfn "Done"
}
