namespace Kita.Providers.Azure.RunContext

open Kita.Core
open Kita.Providers.Azure
open Kita.Domains.Routes

type AzureRunModule<'U> =
    abstract Provider : AzureProvider
    abstract RunRouteState : (RouteState -> 'T) -> 'T

type AzureRunModuleAttribute(name: string) =
    inherit System.Attribute()

    member Name = name

