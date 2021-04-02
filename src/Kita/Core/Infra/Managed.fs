namespace Kita.Core

open Kita.Core.Http
open Kita.Providers
open Kita.Resources

type Managed<'Provider> =
    { resources: CloudResource list
      handlers: MethodHandler list
      name: string
      provider: 'Provider
      nested : Map<string, Managed<Provider>> }

module Managed =
    let inline empty<'Provider
                        when 'Provider :> Provider
                        and 'Provider: (new : unit -> 'Provider)> ()
        =
        { resources = []
          handlers = []
          name = ""
          provider = new 'Provider()
          nested = Map.empty }
