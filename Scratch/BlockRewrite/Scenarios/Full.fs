module BlockRewrite.Scenarios.Full

open BlockRewrite

type FullProvider() =
    interface Provider with
        member _.Launch () = ()
        member _.Run () = ()
