[<AutoOpen>]
module Farmer.Arm.Cache

open Farmer
open Farmer.Redis

let redis = ResourceType("Microsoft.Cache/Redis", "2018-03-01")

type Redis = {
    Name: ResourceName
    Location: Location
    Sku: {| Sku: Sku; Capacity: int |}
    RedisConfiguration: Map<string, string>
    NonSslEnabled: bool option
    ShardCount: int option
    MinimumTlsVersion: TlsVersion option
    Tags: Map<string, string>
} with

    member this.Family =
        match this.Sku.Sku with
        | Basic
        | Standard -> 'C'
        | Premium -> 'P'

    interface IArmResource with
        member this.ResourceId = redis.resourceId this.Name

        member this.JsonModel = {|
            redis.Create(this.Name, this.Location, tags = this.Tags) with
                properties = {|
                    sku = {|
                        name = string this.Sku.Sku
                        family = this.Family
                        capacity = this.Sku.Capacity
                    |}
                    enableNonSslPort = this.NonSslEnabled |> Option.toNullable
                    shardCount = this.ShardCount |> Option.toNullable
                    minimumTlsVersion =
                        // TLS 1.3 is supported, but it can only currently enforce TLS 1.2 as the minimum version.
                        // Reference: https://learn.microsoft.com/azure/redis/tls-configuration#tls-13-support
                        this.MinimumTlsVersion
                        |> Option.map (function
                            | Tls12 -> "1.2"
                            | Tls13 -> "1.2") // TLS 1.3 enforcement not yet supported
                        |> Option.toObj
                    redisConfiguration = this.RedisConfiguration
                |}
        |}