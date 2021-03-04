#r "nuget: FSharp.Quotations.Evaluator"

open FSharp.Quotations

let foo = ref 0

let f = <@ let f x y = !foo + x + y in f @>

let eval = Evaluator.QuotationEvaluator.Evaluate

let f' = eval f

let x =
    Evaluator.QuotationEvaluator.ToLinqExpression f

x |> printfn "linq expression: %A"

printfn "Free vars: %A" <| f.GetFreeVars()
printfn "%A" <| f' 1 2
