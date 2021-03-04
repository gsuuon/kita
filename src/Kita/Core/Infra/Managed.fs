namespace Kita.Core

open Kita.Core.Http
open Kita.Providers
open Kita.Resources

type Managed<'Config> =
    { resources: CloudResource list
      handlers: (string * MethodHandler) list
      names: string list
      config: 'Config }

module Managed =
    let inline empty<'Config
                        when 'Config :> Provider
                        and 'Config: (new : unit -> 'Config)> ()
        =
        { resources = []
          handlers = []
          names = []
          config = new 'Config() }

    let getName managed =
        match List.tryHead managed.names with
        | Some n -> n
        | None -> ""
