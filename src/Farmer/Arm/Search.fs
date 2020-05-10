[<AutoOpen>]
module Farmer.Arm.Search

open Farmer

type SearchService =
    { Name : ResourceName
      Location : Location
      Sku : SearchSku
      ReplicaCount : int
      PartitionCount : int }
    member this.HostingMode =
        match this.Sku with
        | SearchSku.Standard3 HighDensity -> "highDensity"
        | _ -> "default"
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonValue =
            {| ``type`` = "Microsoft.Search/searchServices"
               apiVersion = "2015-08-19"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                {| name =
                    match this.Sku with
                    | SearchSku.Free -> "free"
                    | SearchSku.Basic -> "basic"
                    | SearchSku.Standard -> "standard"
                    | SearchSku.Standard2 -> "standard2"
                    | SearchSku.Standard3 _ -> "standard3"
                    | SearchSku.StorageOptimisedL1 -> "storage_optimized_l1"
                    | SearchSku.StorageOptimisedL2 -> "storage_optimized_l2" |}
               properties =
                {| replicaCount = this.ReplicaCount
                   partitionCount = this.PartitionCount
                   hostingMode = this.HostingMode |}
            |} :> _
