[<AutoOpen>]
module Farmer.Arm.Search

open Farmer
open Farmer.CoreTypes
open Farmer.Search

let searchServices = ResourceType "Microsoft.Search/searchServices"

type SearchService =
    { Name : ResourceName
      Location : Location
      Sku :Sku
      ReplicaCount : int
      PartitionCount : int
      Tags: Map<string,string>  }
    member this.HostingMode =
        match this.Sku with
        | Standard3 HighDensity -> "highDensity"
        | _ -> "default"
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = searchServices.ArmValue
               apiVersion = "2015-08-19"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                {| name =
                    match this.Sku with
                    | Free -> "free"
                    | Basic -> "basic"
                    | Standard -> "standard"
                    | Standard2 -> "standard2"
                    | Standard3 _ -> "standard3"
                    | StorageOptimisedL1 -> "storage_optimized_l1"
                    | StorageOptimisedL2 -> "storage_optimized_l2" |}
               properties =
                {| replicaCount = this.ReplicaCount
                   partitionCount = this.PartitionCount
                   hostingMode = this.HostingMode |}
               tags = this.Tags
            |} :> _
