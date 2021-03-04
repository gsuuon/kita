#load "ReferenceKita.fsx"

open System.Collections.Generic

open Kita.Core
open Kita.Providers
open Kita.Resources.Collections

let myDict : Dictionary<string, Managed<Provider>> = Dictionary()

let az = Azure()
let loc = Local()

let azCloudQueue1 : CloudQueue<string> = CloudQueue()
let azCloudQueue2 : CloudQueue<int> = CloudQueue()
azCloudQueue1.Deploy(az)
azCloudQueue2.Deploy(az)

myDict.["foo"] <-
    {
        resources = [
            azCloudQueue1
            azCloudQueue2
        ]
        handlers = []
        names = ["foo"]
        provider = az
    }
myDict.["bar"] <-
    {
        resources = []
        handlers = []
        names = ["bar"]
        provider = loc
    }

for KeyValue(k,v) in myDict do
    printfn "-- Key: %A" k
    printfn "%A" v

(*
how does this get used in resources?
*)
