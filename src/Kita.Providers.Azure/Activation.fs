module Kita.Providers.Azure.Activation

open System
open System.Threading.Tasks
open System.Text.RegularExpressions


open FSharp.Control.Tasks

open Kita.Utility

let AzureConnectionStringVarName = "Kita_Azure_ConnectionString"

/// Enforces environment variable naming rules
/// only letters, numbers and underscores
/// replaces everything else with underscores
let canonEnvVarName name =
    let pattern = "[^A-z0-9_]"

    Regex.Replace (name, pattern, "_")

let noEnv provision rg sa = task {
    let x : Task<unit> = provision rg sa 
    do! x
    let res : (string * string) option = None
    return res
}

let produceWithEnv envName withEnv =
    let waiter = Waiter()

    async {
        match Environment.GetEnvironmentVariable envName with
        | null ->
            printfn "Expected missing environment variable: %s" envName
                // TODO remove
                // or only print if we're in run context
                // okay to be missing in launch
        | value ->
            value
            |> withEnv
            |> waiter.Set
    } |> Async.Start

    waiter
        
