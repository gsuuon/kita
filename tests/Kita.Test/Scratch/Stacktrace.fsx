
let y = 0

let myfn x =
    let st = System.Diagnostics.StackTrace(true)

    printfn "%A" <| x + y

    st

let callFn fn x =
    fn x


let st = callFn myfn 0

#if PRETEND_THIS_IS_IN_INFRA_BIND
let inBindMemberOfInfra =
    let getTrace n = 
        let st = System.Diagnostics.StackTrace true
        let frame = st.GetFrame n

        sprintf "%A: %A"
        <| frame.GetMethod().DeclaringType.
            // "Kita.Test.Examples+cloudAbout@14-1"
            // Good enough for closures
        <| frame.GetFileLineNumber()

    print s "Handled by" (getTrace 2)
#endif
