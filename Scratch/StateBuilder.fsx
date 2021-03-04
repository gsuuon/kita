type OtherType = { foo: int }

type State<'state, 'a> = State of ('state -> 'a * 'state)

[<AutoOpen>]
module State =
    let run (State f) s = f s
    let ret x = State(fun s -> x, s)

    type Builder() =
        member inline _.Zero() =
            State(fun s -> Unchecked.defaultof<_>, s)

        member _.Bind(State m: State<int, int>, f: int -> State<int, int>) =
            State
                (fun s ->
                    let (x, s') = m s
                    let (State m') = f x
                    printfn "int bind"
                    m' s')

        member _.Bind(State m, f) =
            State
                (fun s ->
                    let (x, s') = m s
                    let (State m') = f x
                    printfn "normal bind"
                    m' s')

        member _.Bind(o: OtherType, f) =
            State
                (fun s ->
                    let (State m) = ret o
                    let (x, s') = m s
                    let (State m') = f x
                    printfn "other bind"
                    m' s')

        member _.Return(x) = ret x

        member _.Delay f =
            State
                (fun s ->
                    let (State f) = f ()
                    f s)

        member _.Run(State m) =
            fun s ->
                let (_x, s) = m s
                s

        member _.ReturnFrom(State m) = m

        member _.Yield x = ret x

        [<CustomOperation("custom", MaintainsVariableSpaceUsingBind = true)>]
        member _.Custom(State runner, [<ProjectionParameter>] argA) =
            State
                (fun s ->
                    let (ctx, s) = runner s
                    let myArgA = argA ctx
                    printfn "Got arga: %A" myArgA
                    ctx, s)

    let state = Builder()

    let getState = State(fun s -> s, s)

let y =
    0
    |> (state {
            let! x = getState
            let! y = ret "hi"
            let! f = { foo = 0 }
            printfn "%i" (x + 1)
            printfn "%s" (y)
            printfn "%A" f
            custom "hi"
        })
