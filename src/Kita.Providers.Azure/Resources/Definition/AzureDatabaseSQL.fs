namespace Kita.Providers.Azure.Resources.Definition

open Kita.Core
open Microsoft.EntityFrameworkCore

type IAzureDatabaseSQL<'T when 'T :> DbContext> =
    abstract GetContext : unit -> 'T

type AzureDatabaseSQLProvider =
    abstract Provide<'T when 'T :> DbContext> : string -> IAzureDatabaseSQL<'T>

type AzureDatabaseSQL(serverName: string) =
    member _.Create (p: #AzureDatabaseSQLProvider) =
        p.Provide serverName
