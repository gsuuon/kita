namespace Kita.Core.Resource

open Kita.Core.Providers

type Binder = unit

type CloudResource =
    abstract member CBind : Binder -> unit
    abstract member ReportDesiredState : Config -> unit
    abstract member BeginActivation : Config -> unit

module Cfg =
    type Persist =
        | ByName of string
        | ByPosition

module Ops =
    type Conf = class end
    
    let inline deploy< ^C, ^R when ^R : (member Deploy : ^C -> unit) and ^C :> Config> (resource: ^R, config: ^C)
        =
        ( ^R : (member Deploy: ^C -> unit) (resource, config) )
