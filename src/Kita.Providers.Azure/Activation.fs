module Kita.Providers.Azure.Activation

open System
open FSharp.Control.Tasks
open System.Threading.Tasks

let AzureConnectionStringVarName = "Kita_Azure_ConnectionString"

let getVariable name =
    Environment.GetEnvironmentVariable name

let noEnv provision rg sa = task {
    let x : Task<unit> = provision rg sa 
    do! x
    let res : (string * string) option = None
    return res
}
