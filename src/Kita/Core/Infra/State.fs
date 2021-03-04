namespace Kita.Core

open Kita.Resources

type State<'a, 'b, 'c> = State of (Managed<'b> -> 'a * Managed<'c>)
type Resource<'T when 'T :> CloudResource> = Resource of 'T

[<AutoOpen>]
module State =
    let addResource (resource: #CloudResource) state =
        { state with
              resources = resource :> CloudResource :: state.resources }

    let getResources = State(fun s -> s.resources, s)

    let addRoutes pathHandlers state =
        { state with
              handlers = state.handlers @ pathHandlers }

    let addName name state =
        { state with
              names = name :: state.names }

    let ret x = State(fun s -> x, s)

    /// Returns type of stateA
    let combine stateA stateB =
        { stateA with
              handlers = stateA.handlers @ stateB.handlers
              resources = stateA.resources @ stateB.resources
              names = stateA.names @ stateB.names }

    let convert state =
        combine <| Managed.empty<'T> () <| state
