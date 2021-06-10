module ProxyApp.AutoReplacedReference

open Kita.Core
open Kita.Domains.Routes
open Kita.Providers.Azure
open Kita.Providers.Azure.RunContext

type AppStatePlaceholder() = class end

type Placeholder() =
    interface AzureRunModule<AppStatePlaceholder> with
        member _.Provider = AzureProvider("","")
        member _.RunRouteState fn = fn RouteState.Empty

let runModule = Placeholder() :> AzureRunModule<_>
