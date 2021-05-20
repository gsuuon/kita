namespace Kita.Domains.Procs

open Kita.Core
open Kita.Domains

type ProcState = { procs : List<unit -> unit> }

module ProcState =
    let addProc proc (procState: ProcState) =
        { procState with procs = proc :: procState.procs }

type ProcBlock<'U>(userDomain)
    =
    inherit DomainBuilder<'U, ProcState>(userDomain)

    [<CustomOperation("proc", MaintainsVariableSpaceUsingBind=true)>]
    member inline this.Proc
        (
            ctx,
                [<ProjectionParameter>]
            getProc
        ) =
        fun s ->
            let proc = getProc ctx

            s
            |> UserDomain.update<'P, 'U, ProcState>
                this.UserDomain
                (ProcState.addProc proc)
