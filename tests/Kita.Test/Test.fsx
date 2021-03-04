#load "ReferenceKita.fsx"

open Kita.Operations
open Kita.Test.Examples

printfn "PROD --"
deploy "prod" <| program false

printfn "\n\nDEV --"
deploy "dev" <| program true
