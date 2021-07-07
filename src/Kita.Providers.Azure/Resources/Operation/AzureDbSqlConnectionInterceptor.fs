namespace Kita.Providers.Azure.Resources.Operation

open System
open System.Threading
open System.Threading.Tasks
open System.Data.Common
open FSharp.Control.Tasks

open Microsoft.Data.SqlClient
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Diagnostics

open Azure.Core
open Azure.Identity

module AzureIdentityToken =
    /// Reads block on write lock
    /// Reads don't block on other reads
    /// Writes block on other writes
    /// Writes block on reads
    /// Will error if recursively called
    type ManyReadOneWriteItem<'T>(?item) =
        let mutable item : 'T option = item
        let lock = new System.Threading.ReaderWriterLockSlim()

        member _.Get () =
            lock.EnterReadLock()
            try
                item
            finally
                lock.ExitReadLock()

        member  _.Set v = 
            lock.EnterWriteLock()
            try
                item <- Some v
            finally
                lock.ExitWriteLock()

        member  _.TryWithWriteLock (hadLock, noLock, ?timeout) =
            let timeout = defaultArg timeout 0
            if lock.TryEnterWriteLock(timeout) then
                try
                    hadLock <| fun v -> item <- v
                finally
                    lock.ExitWriteLock()
            else
                noLock ()
            
        member  _.Clear () =
            lock.EnterWriteLock()
            try
                item <- None
            finally
                lock.ExitWriteLock()

    let credential =
        new ChainedTokenCredential(
            new ManagedIdentityCredential(),
            new EnvironmentCredential()
        )
            
    module TokenCache =
        let private scopes = [|"https://database.windows.net/.default"|]
        let private cachedToken = ManyReadOneWriteItem<AccessToken>()

        let private requestAndCacheToken ctok =
            task {
                let! token = 
                    credential.GetTokenAsync
                        ( new TokenRequestContext (scopes)
                        , ctok
                        )

                cachedToken.Set token

                return token
            }

        let private maybeRequestFromServer =
            let existingRequest = ManyReadOneWriteItem<Task<AccessToken>>()
            let scheduleWork
                (time: DateTimeOffset)
                (work: unit -> Task<'t>)
                =
                task {
                    let refreshDelay =
                        let expiresIn = time - DateTimeOffset.UtcNow
                        max expiresIn TimeSpan.Zero
                        
                    do! Task.Delay refreshDelay
                    let! _ = work()
                    ()
                }
                |> ignore // hot tasks

            let rec doRequest ctok =
                task {
                    let refreshBufferMinutes = 10.0
                        // minutes before expiry to refresh token

                    let! token = requestAndCacheToken ctok

                    scheduleWork
                    <| token.ExpiresOn.AddMinutes(-refreshBufferMinutes)
                    <| maybeRequest ctok

                    scheduleWork
                    <| token.ExpiresOn
                    <| fun () -> task {
                        match cachedToken.Get() with
                        | Some token ->
                            if token.ExpiresOn >= DateTimeOffset.UtcNow
                            then cachedToken.Clear()
                        | None ->
                            ()
                    }

                    existingRequest.Clear()

                    return token
                }
                
            and maybeRequest ctok () : Task<AccessToken> =
                match existingRequest.Get()  with
                | Some request ->
                    request
                | None ->
                    existingRequest.TryWithWriteLock
                        ( fun set ->

                            let request = doRequest ctok
                            set (Some request)
                            request

                        , fun () ->

                            maybeRequest ctok ()
                        )

            maybeRequest

        // Perf sanity checks
        let mutable retrievals = 0
        let mutable timeTaken = 0L

        let time fn =
            let sw = Diagnostics.Stopwatch.StartNew()
            let res = fn ()
            sw.Stop()
            res, sw.ElapsedMilliseconds

        let retrieveToken ctok =
            let getCachedOrRequest _ =
                match cachedToken.Get() with
                | Some token ->
                    Task.FromResult token
                | None ->
                    let tok, _elapsed = time <| fun _ -> maybeRequestFromServer ctok ()
                    tok

            let token, elapsed = 
                time getCachedOrRequest

            Interlocked.Add(&retrievals, 1) |> ignore
            Interlocked.Add(&timeTaken, elapsed) |> ignore

            token

type AzureConnectionInterceptor() =
    inherit DbConnectionInterceptor()

    member _.BaseConnectionOpeningAsync (x,y,z,g) =
        base.ConnectionOpeningAsync(x,y,z,g)
        // Gets around calling `base` in computation expression error
        // A protected member is called or 'base' is being used. This is only allowed in the direct implementation of members since they could escape their object scope. [405: typecheck]

    override this.ConnectionOpeningAsync
        (
            connection: DbConnection,
            eventData: ConnectionEventData,
            result: InterceptionResult,
            cancellationToken: CancellationToken
        ) =
        printfn "Intercepting connection open"
        vtask {
            match connection with
            | :? SqlConnection as sqlCon ->
                printfn "Getting access token"

                try
                    let! accessToken = AzureIdentityToken.TokenCache.retrieveToken cancellationToken
                    printfn "Got access token"
                    sqlCon.AccessToken <- accessToken.Token
                with
                | :? System.InvalidOperationException ->
                    // Just try again, if server was scaled down login will fail once
                    // TODO better retry logic
                    printfn "Connection failed, retrying in 5 seconds"
                    do! Task.Delay 5000
                    let! accessToken = AzureIdentityToken.TokenCache.retrieveToken cancellationToken
                    printfn "Got access token"
                    sqlCon.AccessToken <- accessToken.Token
                    
            | _ ->
                printfn "Connection wasn't sql connection?"
                printfn "Was: %s" (connection.GetType().FullName)
                printfn "Expected: %s" (typeof<SqlConnection>.FullName)
                ()

            return! this.BaseConnectionOpeningAsync(connection, eventData,result,cancellationToken)
        }
    
    override this.ConnectionOpening
        (
            connection: DbConnection,
            eventData: ConnectionEventData,
            result: InterceptionResult
        ) =
        let getTokenTask =
            this.ConnectionOpeningAsync(
                connection,
                eventData,
                result,
                CancellationToken.None).AsTask()

        getTokenTask.Wait()
        getTokenTask.Result
            
