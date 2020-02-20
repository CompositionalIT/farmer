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
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = RedisSku.Basic
          Capacity = 0
          RedisConfiguration = Map.empty
          NonSslEnabled = None
          ShardCount = None
          MinimumTlsVersion = None }
    member _.Run (state:RedisConfig) =
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
    member _.Name(state:RedisConfig, name) = { state with Name = name }
    member this.Name(state:RedisConfig, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the Redis instance.
    [<CustomOperation "sku">]
    member _.Sku(state:RedisConfig, sku) = { state with Sku = sku }
    /// Sets the capacity of the Redis instance.
    [<CustomOperation "capacity">]
    member _.Capacity(state:RedisConfig, capacity) =
        { state with Capacity = capacity }
    /// Adds a custom setting to the Redis configuration
    [<CustomOperation "setting">]
    member _.AddSetting(state:RedisConfig, key, value) = { state with RedisConfiguration = state.RedisConfiguration.Add(key, value) }
    member this.AddSetting(state:RedisConfig, key, value:int) = this.AddSetting(state, key, string value)
    /// Specifies whether the non-ssl Redis server port (6379) is enabled.
    [<CustomOperation "enable_non_ssl_port">]
    member _.EnableNonSsl(state:RedisConfig) = { state with NonSslEnabled = Some true }

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

let redis = RedisBuilder()