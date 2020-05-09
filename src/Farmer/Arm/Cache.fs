[<AutoOpen>]
module Farmer.Arm.Cache

open Farmer

type Redis =
    { Name : ResourceName
      Sku :
        {| Name : string
           Family : char
           Capacity : int |}
      RedisConfiguration : Map<string, string>
      NonSslEnabled : bool option
      ShardCount : int option
      MinimumTlsVersion : string option }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.ToArmObject location =
            {| ``type`` = "Microsoft.Cache/Redis"
               apiVersion = "2018-03-01"
               name = this.Name.Value
               location = location.ArmValue
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
