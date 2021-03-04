#load "../ReferenceKita.fsx"

open System.Collections.Generic

open Kita.Core
open Kita.Providers
open Kita.Resources
open Kita.Resources.Collections

// Instantiate providers
let az = Azure()
let loc = Local()

// Instantiate resources
let azCloudQueue1 : CloudQueue<string> = CloudQueue()
let azCloudQueue2 : CloudQueue<int> = CloudQueue()

let locCloudLog = CloudLog()

// Register provider?
azCloudQueue1.Deploy(az)
azCloudQueue2.Deploy(az)

// Bind resources to provider Managed record
let myDict : IDictionary<string, Managed<Provider>>
    = dict [
    "foo", {
        resources = [
            azCloudQueue1
            azCloudQueue2
        ]
        handlers = []
        names = ["foo"]
        provider = az
    }

    "bar", {
        resources = [locCloudLog]
        handlers = []
        names = ["bar"]
        provider = loc
    }
]

// Compare this dictionary with last known dictionary
let newResources = dict [
    "foo", {
        resources = [
            azCloudQueue2 // these are the same instances as above
        ]
        handlers = []
        names = ["foo"] // ignored
        provider = az
    }
]


for KeyValue(k,v) in myDict do
    printfn "-- Key: %A" k
    printfn "%A" v

