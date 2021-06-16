open BlockRewrite

[<EntryPoint>]
let main argv =
    (* Scenarios.Basic.SimpleScenario.go() |> ignore *)
    Scenarios.Basic.NestedScenario.SameProviderScenario.go() |> ignore
    (* Scenarios.Basic.NestedScenario.SameProviderResourcePassScenario.go() |> ignore *)
    (* Scenarios.Basic.NestedScenario.DifferentProvidersScenario.go() |> ignore *)
    0
