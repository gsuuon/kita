module Kita.Operations

open Kita.Core

let teardown state =
    printfn
        "Teardown %i handlers, %i resources from %i blocks"
        state.handlers.Length
        state.resources.Length
        state.names.Length

let deploy name (state: Managed<_>) =
    (*
    Deploy should be 'Commit' or something similar, the actual action will be to write out the desired changes of state to a file
    or
    deploy should return a nullary function which i accumulate, then execute all sequentially at the end
    or
    - [x] i can use srtp here just to enforce the existence of deploy without executing it. i wait until the end to execute.
*)
    printfn
        "Deploying %s\n%i handlers, %i resources from %i blocks"
        name
        state.handlers.Length
        state.resources.Length
        state.names.Length

    printfn "%A" state
    ()
