module Kita.Providers.Azure.Activation

open System
open System.Threading.Tasks
open FSharp.Control.Tasks

open Kita.Utility

let AzureConnectionStringVarName = "Kita_Azure_ConnectionString"

let getVariable name =
    let env = Waiter<string>()

    try
        env.Set <| Environment.GetEnvironmentVariable name
    with
    | :? ArgumentNullException -> ()

    env

let noEnv provision rg sa = task {
    let x : Task<unit> = provision rg sa 
    do! x
    let res : (string * string) option = None
    return res
}

let produceWithEnv envName withEnv =
    let waiter = Waiter()

    (task {
        let varWaiter = getVariable envName
        let! envValue = varWaiter.GetTask

        waiter.Set <| withEnv envValue
        return waiter
    }).Start()

    waiter
        
