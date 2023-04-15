namespace Kita.Providers.Azure.RunContext

open Kita.Core
open Kita.Providers.Azure
open Kita.Domains.Routes
open Kita.Compile.Reflect

type AzureRunModule<'U> =
    abstract Provider : AzureProvider
    abstract RunRouteState : (RouteState -> 'T) -> 'T
    abstract RunAuthedRouteState : (RouteState -> 'T) -> 'T
