namespace Kita.Core

open Kita.Core.Http
open Kita.Core.Resources

type Managed =
  { resources : CloudResource list
    handlers : (string * MethodHandler) list
    names : string list }
    static member Empty =
      { resources = []
        handlers = []
        names = []}

