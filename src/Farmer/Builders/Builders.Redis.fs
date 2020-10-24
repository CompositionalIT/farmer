[<AutoOpen>]
module Farmer.Builders.Redis

open Farmer
open Farmer.CoreTypes
open Farmer.Redis
open Farmer.Arm.Cache

let internal buildRedisKey (resourceId:ResourceId) =
    let resourceId = resourceId.WithType redis
    let expr =
        sprintf
            "concat('%s.redis.cache.windows.net,abortConnect=false,ssl=true,password=', listKeys('%s', '2015-08-01').primaryKey)"
                resourceId.Name.Value
                resourceId.Name.Value
    ArmExpression.create(expr, resourceId)

type RedisConfig =
    { Name : ResourceName
      Sku : Sku
      Capacity : int
      RedisConfiguration : Map<string, string>
      NonSslEnabled : bool option
      ShardCount : int option
      MinimumTlsVersion : TlsVersion option
      Tags: Map<string,string> }
    member this.Key = buildRedisKey (ResourceId.create this.Name)
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku =
                {| Sku = this.Sku
                   Capacity = this.Capacity |}
              RedisConfiguration = this.RedisConfiguration
              NonSslEnabled = this.NonSslEnabled
              ShardCount = this.ShardCount
              MinimumTlsVersion = this.MinimumTlsVersion
              Tags = this.Tags }
        ]

type RedisBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Sku = Basic
          Capacity = 1
          RedisConfiguration = Map.empty
          NonSslEnabled = None
          ShardCount = None
          MinimumTlsVersion = None
          Tags = Map.empty }
    member __.Run (state:RedisConfig) =
        { state with
            Capacity =
                match state with
                | { Sku = (Basic | Standard) } when state.Capacity > 6 -> 6
                | { Sku = Premium } when state.Capacity > 4 -> 4
                | { Sku = (Basic | Standard) } when state.Capacity < 0 -> 0
                | { Sku = Premium } when state.Capacity < 1 -> 1
                | _ -> state.Capacity
            ShardCount =
                match state with
                | { Sku = Premium; ShardCount = Some shards } when shards > 10 -> Some 10
                | { Sku = Premium; ShardCount = shards } -> shards
                | _ -> None
        }
    /// Sets the name of the Redis instance.
    [<CustomOperation "name">]
    member __.Name(state:RedisConfig, name) = { state with Name = name }
    member this.Name(state:RedisConfig, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the Redis instance.
    [<CustomOperation "sku">]
    member __.Sku(state:RedisConfig, sku) = { state with Sku = sku }
    /// Sets the capacity of the Redis instance.
    [<CustomOperation "capacity">]
    member __.Capacity(state:RedisConfig, capacity) =
        { state with Capacity = capacity }
    /// Adds a custom setting to the Redis configuration
    [<CustomOperation "setting">]
    member __.AddSetting(state:RedisConfig, key, value) = { state with RedisConfiguration = state.RedisConfiguration.Add(key, value) }
    member this.AddSetting(state:RedisConfig, key, value:int) = this.AddSetting(state, key, string value)
    /// Adds a list of custom settings in the form "key" "value" to the Redis configuration.
    [<CustomOperation "settings">]
    member __.AddSettings(state:RedisConfig, settings: (string*int) list) =
        settings
        |> List.fold (fun state (key,value) -> __.AddSetting(state, key, value)) state
    /// Specifies whether the non-ssl Redis server port (6379) is enabled.
    [<CustomOperation "enable_non_ssl_port">]
    member __.EnableNonSsl(state:RedisConfig) = { state with NonSslEnabled = Some true }
    [<CustomOperation "shard_count">]
    member __.ShardCount(state:RedisConfig, shardCount) = { state with ShardCount = Some shardCount }
    [<CustomOperation "minimum_tls_version">]
    member __.MinimumTlsVersion(state:RedisConfig, tlsVersion) = { state with MinimumTlsVersion = Some tlsVersion }
    [<CustomOperation "add_tags">]
    member _.Tags(state:RedisConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:RedisConfig, key, value) = this.Tags(state, [ (key,value) ])

let redis = RedisBuilder()
