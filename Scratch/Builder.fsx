type Request = { content: string }

let someFunction x = printfn "Got %A" x

type ScratchBuilder() =
    member _.Bind(m, f) = f m
    member _.For(m, f) = f m
    member _.Return x = x
    member _.Yield x = x
    (* member _.Combine (ma, mb) = mb *)
    (* member _.Delay f = f() *)
    (* member _.Run f = f() *)

    [<CustomOperation("test", MaintainsVariableSpace = true)>] // MaintainsVariableSpace keeps variables usable after the operator - the return type is the same as the input type
    member _.Test(ctx, [<ProjectionParameter>] a) =
        // ProjectionParameter attribute modifies the passed-in argument to be a function that takes the
        // first arg in order to bind cexpr scope variables
        // I assumed ctx here was M<'T> but it must be something else, since here M<'T> is just 'T
        // I think ctx is a tuple of all variables in the cexpr scope, the generated function grabs the necessary values from it
        // to pull into the expression scope
        someFunction <| (a ctx) ()

        ctx

    [<CustomOperation("test2", MaintainsVariableSpace = true)>]
    member _.Test2(ctx, [<ProjectionParameter>] a) =
        someFunction <| a ctx

        ctx

// NOTE CustomOperation overloaded methods are coming:
// https://github.com/fsharp/fslang-design/blob/master/preview/FS-1056-allow-custom-operation-overloads.md

// More info about the the attribute properties starting page 71 of the F# language spec
// https://fsharp.org/specs/language-spec/4.1/FSharpSpec-4.1-latest.pdf

let scratch = ScratchBuilder()

scratch {
    let! x = 1

    let reqA = { content = "hi" }
    let reqB = { content = "bye" }

    test (fun _ -> reqA.content)
    test (fun _ -> reqB.content)

    test2 x

    test (fun _ -> reqA.content + reqB.content)
    let y = 0
    return y
}
