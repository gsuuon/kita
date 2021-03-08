namespace Kita.Core.Http
// This is all placeholder stuff, will probably replace with existing library

type Status =
    | OK
    | NOTFOUND

type RawRequest =
    { mutable body: string
      mutable queries: System.Collections.Generic.IDictionary<string, string>
      mutable headers: string list
      mutable cookies: string list }

type RawResponse = { body: string; status: Status }

type RawHandler = RawRequest -> Async<RawResponse>

type Request<'T> =
    { body: 'T
      queries: System.Collections.Generic.IDictionary<string, string>
      headers: string list
      cookies: string list }

type Response<'T> = { status: Status; body: string }

type Handler<'T, 'R> =
    Request<'T> -> Response<'R> -> Async<Request<'T> * Response<'R>>

type RequestAdapter<'T> = RawRequest -> Request<'T>
type ResponseAdapter<'T> = RawResponse -> Response<'T>

type HandlerAdapter<'T, 'R> =
    Handler<'T, 'R> -> RequestAdapter<'T> -> ResponseAdapter<'T> -> RawHandler

type MethodHandler =
    | GET of RawHandler
    | POST of RawHandler
