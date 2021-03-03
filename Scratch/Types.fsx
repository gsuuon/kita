type Base() =
    member _.DoThing() = ()

let inline baseOp< ^T when ^T :> Base > (b: ^T) =
    b.DoThing()

type GenericConstraint< 'T when 'T :> Base>(ob: 'T) =
    // Compiles
    member val Ob = ob
    member inline x.Fn () =
        x.Ob |> ignore


type SRTPCnst_SRTPFn< ^T when ^T :> Base>(ob: ^T) =
    // Doesnt
    member inline x.Fn () =
        baseOp ob


(* type GenCnst_SRTPFn< 'T when 'T :> Base>(ob: 'T) = // signature incompat *)
(*     // Doesnt *)
(*     member inline x.Fn () = *)
(*         baseOp x.Ob *)
(*     member val Ob = ob // The declared type parameter 'T' cannot be used here since the type parameter cannot be resolved at compile time *)
