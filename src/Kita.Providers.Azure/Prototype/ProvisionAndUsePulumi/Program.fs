module ProvisionPulumiAuto.Program

open ProvisionPulumiAuto.BasicQueue

[<EntryPoint>]
let main _argv =
    deployAndSendMessage()

    0
