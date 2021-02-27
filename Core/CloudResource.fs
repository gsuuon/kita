namespace Kita.Core

open Kita.Core.Providers

type Binder = unit

type CloudResource =
    abstract member CBind : Binder -> unit
    abstract member ReportDesiredState : Config -> unit
    abstract member BeginActivation : Config -> unit

module CloudOption =
    type Persist =
        | ByName of string
        | ByPosition
