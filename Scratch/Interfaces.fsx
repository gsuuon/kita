type Bar = interface end
    
type Foo =
    abstract member Attach : #Bar -> string

type BarA() =
    interface Bar

type FooA() =
    member inline _.AttachBarA (v: BarA) =
        "Attached BarA"

    member inline _.Attach (v: Bar) =
        "Attached Bar"

    interface Foo with
        member this.Attach (v: #Bar) =
            match v with
            | :? BarA -> 
                this.AttachBarA v
            | _ ->
                this.Attach v

let inline attach (foos: #Foo list) bar =
    let inline callAttach
                bar
                (foo: #Foo)
                =
                foo.Attach bar
    foos
    |> List.map (callAttach bar)
    |> List.iter (printfn "%s")

attach [FooA()] (BarA())

let fooA = FooA()

fooA.Attach (BarA() :> Bar)

(fooA :> Foo).Attach(BarA())
