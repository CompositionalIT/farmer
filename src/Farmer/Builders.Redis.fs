[<AutoOpen>]
module Farmer.Resources.Redis

open Farmer
open Farmer.Models

type TlsVersion = Tls10 | Tls11 | Tls12
[<RequireQualifiedAccess>]
type RedisSku = Basic | Standard | Premium

let internal buildRedisKey (ResourceName name) =
    sprintf
        "concat('%s.redis.cache.windows.net,abortConnect=false,ssl=true,password=', listKeys('%s', '2015-08-01').primaryKey)"
            name
            name
    |> ArmExpression

type RedisConfig =
    { Name : ResourceName
      Sku : RedisSku
      Capacity : int
      RedisConfiguration : Map<string, string>
      NonSslEnabled : bool option
      ShardCount : int option
      MinimumTlsVersion : TlsVersion option }
    member this.Key = buildRedisKey this.Name

type RedisBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Sku = RedisSku.Basic
          Capacity = 1
          RedisConfiguration = Map.empty
          NonSslEnabled = None
          ShardCount = None
          MinimumTlsVersion = None }
    member __.Run (state:RedisConfig) =
        { state with
            Capacity =
                match state with
                | { Sku = (RedisSku.Basic | RedisSku.Standard) } when state.Capacity > 6 -> 6
                | { Sku = RedisSku.Premium } when state.Capacity > 4 -> 4
                | { Sku = (RedisSku.Basic | RedisSku.Standard) } when state.Capacity < 0 -> 0
                | { Sku = RedisSku.Premium } when state.Capacity < 1 -> 1
                | _ -> state.Capacity
            ShardCount =
                match state with
                | { Sku = RedisSku.Premium; ShardCount = Some shards } when shards > 10 -> Some 10
                | { Sku = RedisSku.Premium; ShardCount = shards } -> shards
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

module Converters =
    open Farmer.Models

    let redis location (redis:RedisConfig) : Redis =
        { Name = redis.Name
          Location = location
          Sku =
            {| Name = string redis.Sku
               Family =
                match redis.Sku with
                | RedisSku.Basic | RedisSku.Standard -> 'C'
                | RedisSku.Premium -> 'P'
               Capacity = redis.Capacity |}
          RedisConfiguration = redis.RedisConfiguration
          NonSslEnabled = redis.NonSslEnabled
          ShardCount = redis.ShardCount
          MinimumTlsVersion =
            redis.MinimumTlsVersion
            |> Option.map(function
            | Tls10 -> "1.0"
            | Tls11 -> "1.1"
            | Tls12 -> "1.2")
        }

    module Outputters =
        let redisCache (redis:Redis) = {|
            ``type`` = "Microsoft.Cache/Redis"
            apiVersion = "2018-03-01"
            name = redis.Name.Value
            location = redis.Location.Value
            properties =
                {| sku =
                    {| name = redis.Sku.Name
                       family = redis.Sku.Family
                       capacity = redis.Sku.Capacity
                    |}
                   enableNonSslPort = redis.NonSslEnabled |> Option.toNullable
                   shardCount = redis.ShardCount |> Option.toNullable
                   minimumTlsVersion = redis.MinimumTlsVersion |> Option.toObj
                   redisConfiguration = redis.RedisConfiguration
                |}
        |}

type ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config:RedisConfig) =
        let redis = Converters.redis state.Location config
        { state with Resources = RedisCache redis :: state.Resources }
    member this.AddResources (state, configs) = addResources<RedisConfig> this.AddResource state configs

let redis = RedisBuilder()
