module ProxyApp.AutoReplacedReference

open Kita.Core
open Kita.Domains.Routes

let placeholder withRouteState = withRouteState RouteState.Empty

let appLauncher = placeholder
