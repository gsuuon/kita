namespace Kita.Domains.Routes.Http

open System.Collections.Generic
// This is all placeholder stuff, will probably replace with existing library

type Status =
    | OK
    | NOTFOUND

type RawRequest =
    { mutable body: byte seq
      mutable queries: IDictionary<string, string list>
      mutable headers: IDictionary<string, string list>
      mutable cookies: IDictionary<string, string> }

type RawResponse = { body: string; status: Status }

type RawHandler = RawRequest -> Async<RawResponse>

type Request<'T> =
    { body: 'T
      queries: IDictionary<string, string list>
      headers: IDictionary<string, string list>
      cookies: IDictionary<string, string> }

type Response<'T> = { status: Status; body: string }

type Handler<'T, 'R> =
    Request<'T> -> Response<'R> -> Async<Request<'T> * Response<'R>>

type RequestAdapter<'T> = RawRequest -> Request<'T>
type ResponseAdapter<'T> = RawResponse -> Response<'T>

type HandlerAdapter<'T, 'R> =
    Handler<'T, 'R> -> RequestAdapter<'T> -> ResponseAdapter<'T> -> RawHandler

type MethodHandler =
    { route : string
      method : string
      handler : RawHandler }
