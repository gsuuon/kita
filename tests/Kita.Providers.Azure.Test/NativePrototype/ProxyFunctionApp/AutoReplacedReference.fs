module ProxyApp.AutoReplacedReference

open Kita.Core
open Kita.Domains.Routes

type Placeholder() =
    member _.Launch (withRouteState: RouteState -> 'a) =
        withRouteState RouteState.Empty
    member _.Run () = ()
        
let app = Placeholder()
