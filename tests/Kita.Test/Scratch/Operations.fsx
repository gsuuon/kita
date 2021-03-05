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
// When do we say a resource has changed?
// - When it's been changed in a way that the existing resource can be modified to use in current position
// - Certain resource options, e.g. performance or scaling options
//   - Should be clear which options won't mark the resource for deletion/create another when changed
//   - 2 separate option types
//      - destroying options (if these options change, the resource will be recreated)
//      - in-place options (if these options change, the resource will be updated)
//        - would these be provider-specific?
// When do we say a resource has been removed?
// - If type changed
// - If subtype changed
// When do we say a resource is unchanged?
// - If order has changed

// Resources / procs / handlers automatically get UUID
// old resources get detached - not removed
// old procs must be stopped - can be removed
// old handlers must be stopped and removed
// separate command to manage detached resources (cli? but invoked as part of program)
// - mark for deletion, mark archive
// - command to archive resources -- leaves them detached, won't be marked for deletion

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

