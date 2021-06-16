type Foo =
    abstract member Hi : unit -> unit

type FooA() =
    interface Foo with
        member _.Hi () = ()

type FooB() =
    interface Foo with
        member _.Hi () = ()

type Coll = {
    items : Map<string, Foo -> Foo>
}

type FooFn =
    abstract member Exec : Foo -> Foo

let fnA (f: FooA) =
    f

let fnB (f: FooB) =
    f

let cast (f: #Foo -> #Foo) =
    { new FooFn with
        member _.Exec x = f x :> Foo }

let casted = cast fnA

// ?? Cast fnA : FooA -> FooA to Foo -> Foo
// Doesnt seem like it should be possible

(* let main (c: Coll) = *)
(*     c.items *)
(*     |> Map.add "a" fnA *)
(*     |> Map.add "b" fnB *)

printfn "Compiled"
