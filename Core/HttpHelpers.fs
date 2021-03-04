module Kita.Core.Http.Helpers

let asyncReturn x = async { return x }
let konst x _ = x

let ok body : RawResponse = { status = OK; body = body }
