namespace Kita.Providers.Azure.Resources.Provision

open System.Collections.Generic
open FSharp.Control.Tasks

open Microsoft.Azure.Management.ResourceManager.Fluent.Core
open Microsoft.Azure.Management.Redis.Fluent
open StackExchange.Redis

open Kita.Utility
open Kita.Providers.Azure
open Kita.Resources.Utility
open Kita.Resources.Collections
open Kita.Providers.Azure.Activation
open Kita.Providers.Azure.Resources.Utility

[<AutoOpen>]
module AzureCloudCacheUtility =
    let conStringVarName name =
        $"Kita_Azure_RedisCache_{name}" |> canonEnvVarName

[<AutoOpen>]
module AzureCloudCacheProvision =
    let provision
        (rgName: string)
        (location: string)
        (name: string)
        = task {

        let! redisCache =
            AzurePreviousApi.Credential.azure.RedisCaches
                .Define(name)
                .WithRegion(location)
                .WithExistingResourceGroup(rgName)
                .WithBasicSku()
                .CreateAsync()

        let primaryKey = redisCache.Keys.PrimaryKey
        let url = redisCache.HostName
        let sslPort = redisCache.SslPort
        let connectionString =
            $"{url}:{sslPort},password={primaryKey},ssl=true"

        return Some (conStringVarName name, connectionString)

        }

module RedisClientCache =
    let cache = Dictionary<string, ConnectionMultiplexer>()
        // TODO should be concurrent dictionary

    let get conString =
        match cache.TryGetValue conString with
        | true, client ->
            client
        | false, _ ->
            let client = ConnectionMultiplexer.Connect(conString)
            cache.Add(conString, client)
            client

type AzureCloudCache<'K, 'V>
    (
        redisClient: Waiter<ConnectionMultiplexer>,
        serializer: Serializer<string>
    ) =

    let getDb = async {
        let! client = redisClient.GetAsync
        return client.GetDatabase()
    }

    new (
        name: string,
        requestProvision,
        location,
        ?serializer) =
        let serializer = defaultArg serializer Serializer.json
        
        requestProvision
        <| fun rgName (_saName: string) -> provision rgName location name

        let redisClient =
            produceWithEnv
            <| conStringVarName name
            <| fun conString -> RedisClientCache.get conString

        AzureCloudCache(redisClient, serializer)


    interface ICloudMap<'K, 'V> with
        member _.TryFind x = async {
            let! db = getDb
            let key = serializer.Serialize x
            
            let! result =
                db.StringGetAsync(RedisKey key) |> Async.AwaitTask

            if result.IsNull then
                return None
            else
                return
                    string result 
                    |> serializer.Deserialize
                    |> Some
        }

        member _.Set (k, v) = async {
            let! db = getDb

            let key = serializer.Serialize k |> RedisKey
            let value = serializer.Serialize v |> RedisValue

            let! _wasSet =
                db.StringSetAsync(key, value)
                |> Async.AwaitTask

            return ()
        }
