module Kita.Operations

open Kita.Core

let teardown state =
    printfn "Teardown %i handlers, %i resources from %i blocks"
        state.handlers.Length
        state.resources.Length
        state.names.Length

let deploy name (state: Managed) =
    printfn "Deploying %s\n%i handlers, %i resources from %i blocks"
        name
        state.handlers.Length
        state.resources.Length
        state.names.Length

    printfn "%A" state
    ()
