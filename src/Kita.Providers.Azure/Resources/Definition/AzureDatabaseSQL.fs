namespace Kita.Providers.Azure.Resources.Definition

open Kita.Core
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Diagnostics

type AzureDbContextConfig =
    { connectionString : string
      newConnectionInterceptor : unit -> DbConnectionInterceptor
    }

type IAzureDatabaseSQL<'T when 'T :> DbContext> =
    inherit CloudResource
    abstract GetContext : unit -> 'T

/// Db model can be in a separate project (not required).
/// Generate migrations from that project, they'll be applied during provisioning.
/// Do not call dotnet ef database update.
/// Verify migrations before provisioning step.
type AzureDatabaseSQLProvider =
    abstract Provide<'T when 'T :> DbContext>
        : string * (AzureDbContextConfig -> 'T) -> IAzureDatabaseSQL<'T>

type AzureDatabaseSQL<'T when 'T :> DbContext>
    (
        serverName: string,
        createDbCtx: AzureDbContextConfig -> 'T
    ) =
    member _.Create (p: #AzureDatabaseSQLProvider) =
        p.Provide(serverName, createDbCtx)
