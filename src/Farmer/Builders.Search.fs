[<AutoOpen>]
module Farmer.Resources.Search

open Farmer.Helpers
open Farmer

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

module Converters =
    open Farmer.Models
    let search location (search:SearchConfig) =
        { Name = search.Name
          Location = location
          Sku =
            match search.Sku with
            | SearchSku.Free -> "free"
            | SearchSku.Basic -> "basic"
            | SearchSku.Standard -> "standard"
            | SearchSku.Standard2 -> "standard2"
            | SearchSku.Standard3 _ -> "standard3"
            | SearchSku.StorageOptimisedL1 -> "storage_optimized_l1"
            | SearchSku.StorageOptimisedL2 -> "storage_optimized_l2"
          ReplicaCount = search.Replicas
          PartitionCount = search.Partitions
          HostingMode =
            match search.Sku with
            | SearchSku.Standard3 HighDensity -> "highDensity"
            | _ -> "default"
          }
    module Outputters =
        let search (search:Search) = {|
            ``type`` = "Microsoft.Search/searchServices"
            apiVersion = "2015-08-19"
            name = search.Name.Value
            location = search.Location.ArmValue
            sku =
             {| name = search.Sku |}
            properties =
             {| replicaCount = search.ReplicaCount
                partitionCount = search.PartitionCount
                hostingMode = search.HostingMode |}
        |}

open Farmer.Models
type ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config:SearchConfig) =
        { state with Resources = AzureSearch (Converters.search state.Location config) :: state.Resources }
    member this.AddResources (state, configs) = addResources<SearchConfig> this.AddResource state configs

let search = SearchBuilder()
