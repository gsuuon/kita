namespace Kita.Core

open Kita.Core.Http
open Kita.Providers
open Kita.Resources

type Managed<'Provider> =
    { resources: CloudResource list
      handlers: MethodHandler list
      names: string list
      provider: 'Provider }

module Managed =
    let inline empty<'Provider
                        when 'Provider :> Provider
                        and 'Provider: (new : unit -> 'Provider)> ()
        =
        { resources = []
          handlers = []
          names = []
          provider = new 'Provider() }

    let getName managed =
        match List.tryHead managed.names with
        | Some n -> n
        | None -> ""
