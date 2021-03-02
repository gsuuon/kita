type MyType(name: string) =
    member inline _.Name = name

let t = MyType("hi")

printfn "%A" t.Name
