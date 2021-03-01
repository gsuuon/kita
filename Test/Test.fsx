#r "../bin/Release/netstandard2.0/Kita.dll"

open Kita.Test
open Kita.Operations

deploy "prod" <| program false
deploy "dev" <| program true

(*
Bind resource: Kita.Core.Resources.CloudLog
Routing: about
Bind inner infra: nested
Cloud task: Microsoft.FSharp.Control.FSharpAsync`1[Microsoft.FSharp.Core.Unit]
Routing: status
Bind value: Kita.Core.Resources.CloudLog
Bind resource: Kita.Core.Resources.Collections.CloudQueue`1[System.String]
Bind resource: Kita.Core.Resources.Collections.CloudQueue`1[System.String]
Bind resource: Kita.Core.Resources.Collections.CloudMap`2[System.Object,Microsof
t.FSharp.Core.FSharpOption`1[System.String]]
Bind resource: Kita.Core.Resources.CloudLog
Cloud task: Microsoft.FSharp.Control.FSharpAsync`1[Microsoft.FSharp.Core.Unit]
Routing: save
Routing: sign_in
Deploying prod
3 handlers, 1 resources from 2 blocks
{ resources = [Kita.Core.Resources.CloudTask]
  handlers =
            [("save", GET <fun:cloudMain@74-23>);
             ("save", POST <fun:cloudMain@82-29>);
             ("sign_in", POST <fun:cloudMain@88-34>)]
  names = ["main"; "procs"] }
Bind resource: Kita.Core.Resources.CloudLog
Routing: about
Bind inner infra: nested
Cloud task: Microsoft.FSharp.Control.FSharpAsync`1[Microsoft.FSharp.Core.Unit]
Routing: status
Bind value: Kita.Core.Resources.CloudLog
Routing: admin
Bind inner infra: nested
Bind resource: Kita.Core.Resources.Collections.CloudQueue`1[System.String]
Bind resource: Kita.Core.Resources.Collections.CloudQueue`1[System.String]
Bind resource: Kita.Core.Resources.Collections.CloudMap`2[System.Object,Microsof
t.FSharp.Core.FSharpOption`1[System.String]]
Bind resource: Kita.Core.Resources.CloudLog
Cloud task: Microsoft.FSharp.Control.FSharpAsync`1[Microsoft.FSharp.Core.Unit]
Routing: save
Routing: sign_in
Deploying dev
6 handlers, 2 resources from 4 blocks
{ resources = [Kita.Core.Resources.CloudTask; Kita.Core.Resources.CloudTask]
  handlers =
            [("about", GET <fun:cloudAbout@13-3>);
             ("status", GET <fun:cloudProcs@43-12>);
             ("admin", GET <fun:cloudDebug@22-3>);
             ("save", GET <fun:cloudMain@74-23>);
             ("save", POST <fun:cloudMain@82-29>);
             ("sign_in", POST <fun:cloudMain@88-34>)]
  names = ["main"; "procs"; "debug"; "about"] }
*)
