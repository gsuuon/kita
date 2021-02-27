namespace Kita.Core.Http

type Status<'T> =
    | OK of body : 'T
    | NOTFOUND

type RawStatus =
    | OK
    | NOTFOUND

type RawRequest = {
    body : string
    headers : string list
    cookies : string list
}

type RawResponse = {
    body : string
    status : RawStatus
}

type RawHandler = RawRequest -> Async<RawResponse>

type Request<'T> = {
    body : 'T
    headers : string list
    cookies : string list
}

type Response<'T> = {
    status : Status<'T>
    body : string
}

type Handler<'T, 'R> =
    Request<'T> -> Response<'R> -> Async<Request<'T> * Response<'R>>

type RequestAdapter<'T> = RawRequest -> Request<'T>
type ResponseAdapter<'T> = RawResponse -> Response<'T>
type HandlerAdapter<'T,'R> = Handler<'T, 'R> -> RequestAdapter<'T> -> ResponseAdapter<'T> -> RawHandler

type MethodHandler =
    | GET of RawHandler
    | POST of RawHandler
