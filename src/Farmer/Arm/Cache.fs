[<AutoOpen>]
module Farmer.Arm.Cache

open Farmer
open Farmer.CoreTypes
open Farmer.Redis

let redis = ResourceType ("Microsoft.Cache/Redis", "2018-03-01")

type Redis =
    { Name : ResourceName
      Location : Location
      Sku : {| Sku : Sku; Capacity : int |}
      RedisConfiguration : Map<string, string>
      NonSslEnabled : bool option
      ShardCount : int option
      MinimumTlsVersion : TlsVersion option
      Tags: Map<string,string> }
      member this.Family =
        match this.Sku.Sku with
        | Basic | Standard -> 'C'
        | Premium -> 'P'

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| redis.Create(this.Name, this.Location, tags = this.Tags) with
                 properties =
                      {| sku =
                          {| name = string this.Sku.Sku
                             family = this.Family
                             capacity = this.Sku.Capacity
                          |}
                         enableNonSslPort = this.NonSslEnabled |> Option.toNullable
                         shardCount = this.ShardCount |> Option.toNullable
                         minimumTlsVersion =
                           this.MinimumTlsVersion
                           |> Option.map(function
                               | Tls10 -> "1.0"
                               | Tls11 -> "1.1"
                               | Tls12 -> "1.2")
                           |> Option.toObj
                         redisConfiguration = this.RedisConfiguration
                     |}
            |} :> _
