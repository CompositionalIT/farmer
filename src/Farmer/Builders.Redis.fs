[<AutoOpen>]
module Farmer.Resources.Redis

open Farmer

type TlsVersion = Tls10 | Tls11 | Tls12
[<RequireQualifiedAccess>]
type RedisSku = Basic | Standard | Premium

let internal buildRedisKey (ResourceName name) =
    sprintf
        "concat('%s.redis.cache.windows.net,abortConnect=false,ssl=true,password=', listKeys('%s', '2015-08-01').primaryKey)"
            name
            name
    |> ArmExpression

type Redis =
    { Name : ResourceName
      Location : Location
      Sku :
        {| Name : string
           Family : char
           Capacity : int |}
      RedisConfiguration : Map<string, string>
      NonSslEnabled : bool option
      ShardCount : int option
      MinimumTlsVersion : string option }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| ``type`` = "Microsoft.Cache/Redis"
               apiVersion = "2018-03-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               properties =
                   {| sku =
                       {| name = this.Sku.Name
                          family = this.Sku.Family
                          capacity = this.Sku.Capacity
                       |}
                      enableNonSslPort = this.NonSslEnabled |> Option.toNullable
                      shardCount = this.ShardCount |> Option.toNullable
                      minimumTlsVersion = this.MinimumTlsVersion |> Option.toObj
                      redisConfiguration = this.RedisConfiguration
                   |}
            |} :> _

type RedisConfig =
    { Name : ResourceName
      Sku : RedisSku
      Capacity : int
      RedisConfiguration : Map<string, string>
      NonSslEnabled : bool option
      ShardCount : int option
      MinimumTlsVersion : TlsVersion option }
    member this.Key = buildRedisKey this.Name
    interface IResourceBuilder with
        member this.BuildResources location _ = [
            NewResource
                { Name = this.Name
                  Location = location
                  Sku =
                    {| Name = string this.Sku
                       Family =
                        match this.Sku with
                        | RedisSku.Basic | RedisSku.Standard -> 'C'
                        | RedisSku.Premium -> 'P'
                       Capacity = this.Capacity |}
                  RedisConfiguration = this.RedisConfiguration
                  NonSslEnabled = this.NonSslEnabled
                  ShardCount = this.ShardCount
                  MinimumTlsVersion =
                    this.MinimumTlsVersion
                    |> Option.map(function
                    | Tls10 -> "1.0"
                    | Tls11 -> "1.1"
                    | Tls12 -> "1.2")
                }
        ]

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

let redis = RedisBuilder()
