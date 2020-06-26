[<AutoOpen>]
module Farmer.Builders.Search

open Farmer
open Farmer.CoreTypes
open Farmer.Search
open Farmer.Helpers
open Farmer.Arm.Search

type SearchConfig =
    { Name : ResourceName
      Sku : Sku
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
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              ReplicaCount = this.Replicas
              PartitionCount = this.Partitions }
        ]

type SearchBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Sku = Standard
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

let search = SearchBuilder()
