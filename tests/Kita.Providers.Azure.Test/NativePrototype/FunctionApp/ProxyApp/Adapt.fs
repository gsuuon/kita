module ProxyApp.Adapt

open System.Net
open System.Web
open System.IO
open System.IO.Pipelines
open Microsoft.Azure.Functions.Worker.Http
open Kita.Core.Http
open FSharp.Control.Tasks

let inRequest (req: HttpRequestData) = task {
    let pr = PipeReader.Create(req.Body)
    let! result = pr.ReadAsync()
    
    let queries =
        let queries' = HttpUtility.ParseQueryString req.Url.Query

        queries'.AllKeys
        |> Seq.map (fun key ->
            let values = queries'.GetValues key
            key, values |> Array.toList
            )
        |> dict

    let headers =
        let headers' = req.Headers
        headers'
        |> Seq.map (fun (KeyValue(k, v)) -> k, v |> Seq.toList)
        |> dict

    let cookies =
        let cookies' = req.Cookies
        cookies'
        |> Seq.map (fun x -> x.Name, x.Value)
        |> dict

    return
        { body = result.Buffer
          queries = queries
          headers = headers
          cookies = cookies
        }
    }

let outResponse
    (req: HttpRequestData)
    (res: RawResponse)
        : HttpResponseData
    =
    let response =
        match res.status with
        | OK -> HttpStatusCode.OK
        | NOTFOUND -> HttpStatusCode.NotFound
        |> req.CreateResponse

    response.WriteString res.body
    response
    
