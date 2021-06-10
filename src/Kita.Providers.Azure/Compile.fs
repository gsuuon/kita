namespace Kita.Providers.Azure.Compile

open Kita.Compile

/// Use attribute on type implementing AzureRunModule interface
/// Consumed during project generation to specify which type
/// manages the run module
type AzureRunModuleAttribute(name: string) =
    inherit System.Attribute()

    member val Name = name

module Reflect =
    let findAzureRunModule name =
        Reflect.findType <| fun mi ->
            mi.GetCustomAttributes true
            |> Array.exists (fun attr ->
                attr.GetType() = typeof<AzureRunModuleAttribute> &&
                let azureRunModule = attr :?> AzureRunModuleAttribute
                azureRunModule.Name = name
                )
