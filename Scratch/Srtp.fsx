// from https://github.com/fsharp/fslang-suggestions/issues/641

// I can specify a group of SRTP constraints like this:
type DuckLike< ^a when ^a: (member Walk: unit -> unit)
               and ^a: (member Quack: unit -> unit) > = ^a

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

let doDuckThings (duck: #Duck) =
    duck.Float()

    (* doDuckLikeThings duck *)
        // First callsite of doDuckThings will constrain duck due to this line
        // Works if we inline this as well

let main () =
    let daffy = DaffyDuck()
    let donald = DonaldDuck()

    doDuckLikeThings daffy
    doDuckLikeThings donald

    daffy.Gloat()
    donald.Yell()

    doDuckThings(daffy)
    doDuckThings(donald)

main()
