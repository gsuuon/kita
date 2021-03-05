
let y = 0

let myfn x =
    let st = System.Diagnostics.StackTrace(true)

    printfn "%A" <| x + y

    st

let callFn fn x =
    fn x


let st = callFn myfn 0
