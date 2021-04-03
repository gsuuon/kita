namespace Kita.Core

open Kita.Core

type Kita() =
    static member DefaultGroupName = "myappgroup"
    static member DefaultLocation = "eastus"

    static member Run (app: IBlock<_>) =
        let attached = Managed.empty() |> app.Attach

        attached.provider.Launch
            ( Kita.DefaultGroupName
            , Kita.DefaultLocation
            )

        ()
