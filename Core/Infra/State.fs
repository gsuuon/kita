namespace Kita.Core

open Kita.Core.Resources

type State<'a> = | State of (Managed -> 'a * Managed)
type Resource<'T when 'T :> CloudResource> = | Resource of 'T

[<AutoOpen>]
module State =
    let addResource (resource: #CloudResource) state =
        { state with
            resources = resource :> CloudResource :: state.resources }
    let getResources = State (fun s -> s.resources, s)

    let addRoutes pathHandlers state =
        { state with handlers = state.handlers @ pathHandlers }

    let addName name state =
        { state with names = name :: state.names }

    let ret x = State (fun s -> x, s)
