#r "../bin/Release/netstandard2.0/Kita.dll"

open Kita.Test
open Kita.Operations
open Kita.Test.Examples

deploy "prod" <| program false
deploy "dev" <| program true
