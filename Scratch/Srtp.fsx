// from https://github.com/fsharp/fslang-suggestions/issues/641

// I can specify a group of SRTP constraints like this:
type DuckLike< ^a when ^a : (member Walk: unit -> unit)
               and ^a : (member Quack: unit -> unit)
               and ^a : (new: unit -> ^a)
               > = ^a

type Duck() =
    member _.Float() = printfn "Duck floating"

type DaffyDuck() =
    inherit Duck()
    member _.Walk() = printfn "Daffy Walking"
    member _.Quack() = printfn "Daffy Quacking"
    member _.Gloat() =  printfn "Daffy Gloating"

type DonaldDuck() =
    inherit Duck()
    member _.Walk() = printfn "Donald Walking"
    member _.Quack() = printfn "Donald Quacking"
    member _.Yell() = printfn "Donald Yelling"

let inline doDuckLikeThings (duck: ^a DuckLike) =
    (^a : (member Walk : unit -> unit) (duck))
    (^a : (member Quack : unit -> unit) (duck))

type DuckMarker< ^a when ^a : (member Walk: unit -> unit)
               and ^a : (member Quack: unit -> unit)
               and ^a : (new: unit -> ^a) >() = class end

let inline createDuckLike< 'T, 'a when 'T :> DuckMarker<'a> > () = new ^a()
(* type DuckContainer< 'T, 'a when 'T :> DuckMarker< 'a> > = { *)
(*     // T_T why does the above work but this no.. *)
(*     foo : string *)
(*     duck : ^T *)
(* } *)

type DuckContainer2<'T> =
  { foo : string
    duck : 'T }
    (* static member Empty () = *) // doesn't work
    (*   { foo = "" *)
    (*     duck = createDuckLike<_, 'T>() } *)

let inline createDuckContainer2< 'T, 'a when 'T :> DuckMarker<'a> > () =
  { foo = ""
    duck = new 'a() }

let inline createDuckContainer<'T>() =
    createDuckContainer2<_, 'T>()

let doDuckThings (duck: #Duck) =
    duck.Float()

    (* doDuckLikeThings duck *)
        // First callsite of doDuckThings will constrain duck due to this line
        // Works if we inline this as well

let main () =
    let daffy = DaffyDuck()
    let donald = DonaldDuck()

    let daffy2 = createDuckLike<_, DaffyDuck>()

    doDuckLikeThings daffy
    doDuckLikeThings donald
    doDuckLikeThings daffy2

    daffy.Gloat()
    donald.Yell()
    daffy2.Quack()

    doDuckThings(daffy)
    doDuckThings(donald)
    doDuckThings(daffy2)

    let duckContainer2 = createDuckContainer2<_, DaffyDuck>()

    duckContainer2.duck.Quack()

main()
