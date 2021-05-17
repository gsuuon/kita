namespace Kita.Domains.Procs

open Kita.Core
open Kita.Domains

type ProcState = { procs : List<unit -> unit> }

module ProcState =
    let addProc proc (procState: ProcState) =
        { procState with procs = proc :: procState.procs }

type ProcBlockBuilder<'P, 'U when 'P :> Provider>(userDomain)
    =
    inherit DomainBuilder<'P, 'U, ProcState>()

    [<CustomOperation("proc", MaintainsVariableSpaceUsingBind=true)>]
    member inline _.Proc
        (
            ctx,
                [<ProjectionParameter>]
            getProc
        ) =
        fun s ->
            let proc = getProc ctx

            s
            |> UserDomain.update<'P, 'U, ProcState>
                userDomain
                (ProcState.addProc proc)
