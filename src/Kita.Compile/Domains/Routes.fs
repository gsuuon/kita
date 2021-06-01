namespace Kita.Compile.Domains.Routes

open System
open Kita.Compile.Reflect

[<AttributeUsage(AttributeTargets.Method, AllowMultiple=false)>]
type RoutesEntrypoint(name: string) =
    inherit Attribute()
    member val Name = name

module Reflect =
    let findRoutesEntry name =
        findMethod <| fun mi ->
            mi.GetCustomAttributes true
            |> Array.exists (fun attr ->
                attr.GetType() = typeof<RoutesEntrypoint> &&
                let routesEntrypoint = attr :?> RoutesEntrypoint
                routesEntrypoint.Name = name
                )
