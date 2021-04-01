module Kita.Core.Http.Helpers

let asyncReturn x = async { return x }
let konst x _ = x

let ok body : RawResponse = { status = OK; body = body }

module Handlers =
    let asHandler method handler = fun route ->
        { route = route
          handler = handler
          method = method }

    let get = asHandler "get"
    let post = asHandler "post"
