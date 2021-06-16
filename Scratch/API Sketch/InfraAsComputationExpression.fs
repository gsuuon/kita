open Kita

let infra = Infra Default.Azure
// Default.Aws, Default.Gcp, Default.Local
(*
let azureDev =
  { Default.Azure with
     name = "dev"
     deploy = "func host start" }
*)

let cloudMain = infra {
  let! pendingQueue = CloudQueue<string>()
    // CloudQueue is intermediate object
    // Defaults.Azure determines how its filled out to InfraObject<Queue<string>>
  let! readyQueue = CloudQueue<string> (Persist.ByName "readyQ")
    // By default creates new queue resource every execution, just like how a normal program creates new objects each execution.
    // To persist, need use constructor overload with persist options
  
  pendingQueue.ReceiveItem.Add
  <| fun item ->
       nlog $"Got item {item}"
       
  do! CloudTask <| async {
    while true do
      let! msgs = pendingQueue.Dequeue(30)
      do! readyQueue.EnqueueAll msgs
      Async.Sleep 10000
  }

  let! saves = CloudMap<string, byte[]>()
  
  route "Save" [
  // https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions#custom-operations

    GET authenticate
    GET <| fun req res -> async {
      
      match! saves.TryFind req.auth.sid with
        // Need to compose if req type should be sequentially dependent
        // or Async compose, ie
        // GET <|
        // authenticate
        // >>! fun aReq res -> ...
      | Some bytes ->
        res.body <- bytes
        res.status <- OK
      | None ->
        res.status <- NOTFOUND
    }
    POST authenticate
    POST <| fun (req: Req<byte[]>) res -> async {
      do! saves.[req.auth.sid] <- req.body
      do! pendingSaves.Enqueue req.auth.sid
      res.status <- OK
    }
}
(*
Executing this generates artifact files in run directory .kita folder. The resources state, persisted resources are to be pulled from there.
Configuration in directory. Eg .kita/Debug/persist.fsx, .kita/Release/persist.fsx
*)

let teardown () =
  Kita.Teardown cloudMain

[<EntryPoint>]
let main argv =
  Kita.Deploy cloudMain
  
  0





