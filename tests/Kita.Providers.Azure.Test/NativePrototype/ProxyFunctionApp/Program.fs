open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Azure.Functions.Worker.Configuration

[<EntryPoint>]
let main argv =
    let host = new HostBuilder()

    host.ConfigureFunctionsWorkerDefaults()
        .Build()
        .Run()

    0
