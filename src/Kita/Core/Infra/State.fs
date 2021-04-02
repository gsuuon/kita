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

    let addNested child parent =
        { parent with
            nested = parent.nested.Add (child.name, child) }

    let setName name managed =
        { managed with name = name }

    let ret x = State(fun s -> x, s)
