[<AutoOpen>]
module Farmer.Arm.Search

open Farmer

type SearchService =
    { Name : ResourceName
      Location : Location
      Sku : string
      HostingMode : string
      ReplicaCount : int
      PartitionCount : int }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Search/searchServices"
               apiVersion = "2015-08-19"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                {| name = this.Sku |}
               properties =
                {| replicaCount = this.ReplicaCount
                   partitionCount = this.PartitionCount
                   hostingMode = this.HostingMode |}
            |} :> _
