namespace Kita.Providers.Azure.Compile

open Kita.Compile

/// Use attribute on type implementing AzureRunModule interface
/// Consumed during project generation to specify which type
/// manages the run module
type AzureRunModuleForAttribute(name: string) =
    inherit System.Attribute()

    member val Name = name

module Reflect =
    let findAzureRunModule name =
        Reflect.findType <| fun mi ->
            mi.GetCustomAttributes true
            |> Array.exists (fun attr ->
                attr.GetType() = typeof<AzureRunModuleForAttribute> &&
                let azureRunModule = attr :?> AzureRunModuleForAttribute
                azureRunModule.Name = name
                )
