[<AutoOpen>]
module Farmer.Resources.Redis

open Farmer
open Farmer.Models

type TlsVersion = Tls10 | Tls11 | Tls12
[<RequireQualifiedAccess>]
type RedisSku = Basic | Standard | Premium

type RedisConfig =
    { Name : ResourceName
      Sku : RedisSku
      Capacity : int
      RedisConfiguration : Map<string, string>
      NonSslEnabled : bool option
      ShardCount : int option
      MinimumTlsVersion : TlsVersion option }

type RedisBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = RedisSku.Basic
          Capacity = 0
          RedisConfiguration = Map.empty
          NonSslEnabled = None
          ShardCount = None
          MinimumTlsVersion = None }
    /// Sets the name of the Azure Search instance.
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
    /// Enables non-SSL port access to Redis
    [<CustomOperation "enable_non_ssl_port">]
    member _.EnableNonSsl(state:RedisConfig) = { state with NonSslEnabled = Some true }

module Converters =
    open Farmer.Models

    let redis location (redis:RedisConfig) : Redis =
        { Name = redis.Name
          Location = location
          Sku =
            {| Name = string RedisSku.Basic
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