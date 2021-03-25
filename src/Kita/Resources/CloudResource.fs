namespace Kita.Resources

open Kita.Providers

type Binder = unit

type CloudResource =
    abstract member CBind : Binder -> unit
    abstract member ReportDesiredState : Provider -> unit
    abstract member BeginActivation : Provider -> unit

module Cfg =
    type Persist =
        | ByName of string
        | ByPosition

module Ops =
    type Conf =
        class
        end

    let inline attach< ^C, ^R when ^R: (member Attach : ^C -> unit) and ^C :> Provider>
        (
            resource: ^R,
            config: ^C
        )
        =
        (^R: (member Attach : ^C -> unit) (resource, config))
