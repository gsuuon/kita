#r "nuget: Farmer, 1.4.0"

open Farmer
open Farmer.Builders

let myStorage = storageAccount {
    name "ane32ru4q098qrt340u9p"
    (* add_table "hi" *)

    add_queues [
        "pending"
        "saving"
        "bloop"
    ]
    add_queues [
        "pending"
        "saving"
        "bloop"
    ]
}

let myWebApp = webApp {
    name "myfirstwebapp9u8r34qqr3p9u48"
    setting "storageKey" myStorage.Key
}

let deployment = arm {
    location Location.WestUS
    add_resource myStorage
    add_resource myWebApp

    output "storageConnString" myStorage.Key
}

let outputs = deployment |> Deploy.execute "myResourceGroup" Deploy.NoParameters

printfn "Done: %s" outputs.["storageConnString"]
