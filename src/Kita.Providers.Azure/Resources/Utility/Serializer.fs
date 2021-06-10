namespace Kita.Providers.Azure.Resources.Utility

open Kita.Resources.Utility
open System.Text.Json

module Serializer =
    let json =
        { new Serializer<string> with
            member _.Serialize x =
                JsonSerializer.Serialize x
            member _.Deserialize x =
                JsonSerializer.Deserialize<'T> x 
        }
