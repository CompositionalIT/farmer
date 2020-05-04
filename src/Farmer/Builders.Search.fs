[<AutoOpen>]
module Farmer.Resources.Search

open Farmer
open Farmer.Helpers
open Arm.Search

type HostingMode = Default | HighDensity
[<RequireQualifiedAccess>]
/// The SKU of the search service you want to create. E.g. free or standard.
type SearchSku =
    | Free
    | Basic
    | Standard
    | Standard2
    | Standard3 of HostingMode
    | StorageOptimisedL1
    | StorageOptimisedL2

type SearchConfig =
    { Name : ResourceName
      Sku : SearchSku
      Replicas : int
      Partitions : int }
    /// Gets an ARM expression for the admin key of the search instance.
    member this.AdminKey =
        sprintf "listAdminKeys('Microsoft.Search/searchServices/%s', '2015-08-19').primaryKey" this.Name.Value
        |> ArmExpression
    /// Gets an ARM expression for the query key of the search instance.
    member this.QueryKey =
        sprintf "listQueryKeys('Microsoft.Search/searchServices/%s', '2015-08-19').value[0].key" this.Name.Value
        |> ArmExpression
    interface IResourceBuilder with
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              Sku =
                match this.Sku with
                | SearchSku.Free -> "free"
                | SearchSku.Basic -> "basic"
                | SearchSku.Standard -> "standard"
                | SearchSku.Standard2 -> "standard2"
                | SearchSku.Standard3 _ -> "standard3"
                | SearchSku.StorageOptimisedL1 -> "storage_optimized_l1"
                | SearchSku.StorageOptimisedL2 -> "storage_optimized_l2"
              ReplicaCount = this.Replicas
              PartitionCount = this.Partitions
              HostingMode =
                match this.Sku with
                | SearchSku.Standard3 HighDensity -> "highDensity"
                | _ -> "default"
              }
        ]

type SearchBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Sku = SearchSku.Standard
          Replicas = 1
          Partitions = 1 }
    member __.Run(state:SearchConfig) =
        { state with Name = state.Name |> sanitiseSearch |> ResourceName }
    /// Sets the name of the Azure Search instance.
    [<CustomOperation "name">]
    member __.Name(state:SearchConfig, name) = { state with Name = name }
    member this.Name(state:SearchConfig, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the Azure Search instance.
    [<CustomOperation "sku">]
    member __.Sku(state:SearchConfig, sku) = { state with Sku = sku }
    /// Sets the replica count of the Azure Search instance.
    [<CustomOperation "replicas">]
    member __.ReplicaCount(state:SearchConfig, replicas:int) = { state with Replicas = replicas }
    /// Sets the number of partitions of the Azure Search instance.
    [<CustomOperation "partitions">]
    member __.PartitionCount(state:SearchConfig, partitions:int) = { state with Partitions = partitions }

open WebApp
type WebAppBuilder with
    member this.DependsOn(state:WebAppConfig, search:SearchConfig) =
        this.DependsOn(state, search.Name)
type FunctionsBuilder with
    member this.DependsOn(state:FunctionsConfig, search:SearchConfig) =
        this.DependsOn(state, search.Name)

let search = SearchBuilder()
