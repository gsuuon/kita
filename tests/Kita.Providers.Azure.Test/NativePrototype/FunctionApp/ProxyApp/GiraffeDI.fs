namespace GiraffeProxyApp

open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Giraffe

type GiraffeDI() =
    inherit FunctionsStartup()
    
    override _.Configure(builder: IFunctionsHostBuilder) =
        builder.Services.AddGiraffe() |> ignore

[<assembly: FunctionsStartup(typeof<GiraffeDI>)>]
do ()
