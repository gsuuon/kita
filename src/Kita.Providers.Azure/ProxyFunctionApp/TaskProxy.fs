namespace ProxyApp.TaskProxy

open Kita.Providers.Azure
open Kita.Providers.Azure.RunContext

open Microsoft.Extensions.Logging
open Microsoft.Azure.Functions.Worker

module TaskProxy =
    let runModule = ProxyApp.AutoReplacedReference.runModule :> AzureRunModule<_>

    let tasks = runModule.Provider.CloudTasks

    let injectLog (lg: ILogger) =
        let logInjecter = runModule.Provider :> InjectableLogger

        logInjecter.SetLogger
            { new Kita.Resources.Logger with
                member _.Info x = lg.LogInformation x
                member _.Warn x = lg.LogWarning x
                member _.Error x = lg.LogError x
            }

    // GeneratedTasks
