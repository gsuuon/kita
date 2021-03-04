#I "bin/Release/netcoreapp3.1/"
#r "Kita"
#r "Kita.Test"
#r "Kita.Providers"
#r "Kita.Providers.Azure"
#r "Kita.Providers.Local"

open Kita.Operations
open Kita.Test.Examples

printfn "PROD --"
deploy "prod" <| program false

printfn "\n\nDEV --"
deploy "dev" <| program true
