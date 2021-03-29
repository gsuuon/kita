namespace ControllerProj // Facilitates handover
// Should edit accessor project to point to the correct item

open FSharp.Reflection
open FSharp.Reflection.FSharpReflectionExtensions

module Control =
    open System

    let replaceInAccessor (typ: Type) =
        ()

    let doThing _ = ()
        
    let accept fn =
        <@ fn @> |> doThing
            // basically equivalent to GetType() + obj ref
