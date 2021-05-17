module Kita.Domains.Routes.HttpHelpers

open Kita.Domains.Routes

let asyncReturn x = async { return x }
let konst x _ = x

let ok body : RawResponse = { status = OK; body = body }

[<AutoOpen>]
module Handlers =
    let asHandler method handler = fun route ->
        { route = route
          handler = handler
          method = method }

    let get = asHandler "get"
    let post = asHandler "post"

    let canonMethod (methodString: string) =
        methodString.ToLower()
