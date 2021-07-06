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

// TODO
// move all environment access to here
// change from environment to secrets config file
let getActivationData activationPath =
    match Environment.GetEnvironmentVariable activationPath with
    | null ->
        None
    | value ->
        Some value
    
let produceWithEnv envName withEnv =
    let waiter = Waiter()

    async {
        match getActivationData envName with
        | None ->
            printfn "Missing activation data for: %s" envName
        | Some value ->
            value
            |> withEnv
            |> waiter.Set
    } |> Async.Start

    waiter

