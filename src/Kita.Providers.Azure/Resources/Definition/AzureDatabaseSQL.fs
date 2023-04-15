namespace Kita.Providers.Azure.Resources.Definition

open Kita.Core
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Diagnostics

type IAzureDatabaseSQL<'T when 'T :> DbContext> =
    inherit CloudResource
    /// Might be IDisposable, make sure to `use` if so
    abstract GetContext : unit -> 'T

/// Db model can be in a separate project (not required).
/// Generate migrations from that project, they'll be applied during provisioning.
/// Do not call dotnet ef database update.
/// Verify migrations before provisioning step.
type AzureDatabaseSQLProvider =
    abstract Provide<'T when 'T :> DbContext>
        : string * (DbContextOptions -> 'T) -> IAzureDatabaseSQL<'T>

// NOTE
// createDbCtx param will warn at ctor site if it's
// an IDisposable type's constructor. Leaving this
// as a reminder to `use` the result of GetContext
// TODO
// Preferably this warning pushed to the
// GetContext() call site.
type AzureDatabaseSQL<'T when 'T :> DbContext>
    (
        serverName: string,
        createDbCtx: DbContextOptions -> 'T
    ) =
    member _.Create (p: #AzureDatabaseSQLProvider) =
        p.Provide(serverName, createDbCtx)
