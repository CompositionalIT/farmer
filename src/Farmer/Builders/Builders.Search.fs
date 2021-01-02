[<AutoOpen>]
module Farmer.Builders.Search

open Farmer
open Farmer.Search
open Farmer.Helpers
open Farmer.Arm.Search

type SearchConfig =
    { Name : ResourceName
      Sku : Sku
      Replicas : int
      Partitions : int
      Tags: Map<string,string>  }
    /// Gets an ARM expression for the admin key of the search instance.
    member this.AdminKey =
        let expr = sprintf "listAdminKeys('Microsoft.Search/searchServices/%s', '2015-08-19').primaryKey" this.Name.Value
        ArmExpression.create(expr, this.ResourceId)
    /// Gets an ARM expression for the query key of the search instance.
    member this.QueryKey =
        let expr = sprintf "listQueryKeys('Microsoft.Search/searchServices/%s', '2015-08-19').value[0].key" this.Name.Value
        ArmExpression.create(expr, this.ResourceId)
    member this.ResourceId = searchServices.resourceId this.Name
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              ReplicaCount = this.Replicas
              PartitionCount = this.Partitions
              Tags = this.Tags  }
        ]

type SearchBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Sku = Standard
          Replicas = 1
          Partitions = 1
          Tags = Map.empty  }
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
    interface ITaggable<SearchConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let search = SearchBuilder()
