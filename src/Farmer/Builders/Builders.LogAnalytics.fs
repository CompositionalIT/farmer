[<AutoOpen>]
module Farmer.Builders.LogAnalytics
open Farmer
open Farmer.LogAnalytics
open Farmer.Arm.LogAnalytics
type  WorkSpaceconfig =
    { Name: ResourceName
      Sku: Sku
      retentionInDays: int option
      publicNetworkAccessForIngestion: string option
      publicNetworkAccessForQuery: string option
      Tags: Map<string,string>
      }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location =
            [ { Name = this.Name
                Location = location
                Sku = this.Sku
                retentionInDays = 
                    match this.Sku, this.retentionInDays with
                    |Standard, Some 30 -> Some 30
                    |Standard, Some _ -> failwithf "the retention Days for Standard must be 30."
                    |Premium, Some 365 -> Some 365
                    |Premium, Some _ -> failwithf "the retention Days for Premium must be 365."
                    |Free, None -> None
                    |Free, _ -> failwithf "Remove the retentionInDays element If you specify a pricing tier of Free,."
                    |(Standalone|PerNode|PerGB2018), Some value  when value < 30 || value > 730 -> failwithf "the retention Days for  PerNode,PerGB2018 and Standalone  must be between 30 and 730" 
                    |_, Some value -> Some value
                    |_, None -> None
                publicNetworkAccessForIngestion = this.publicNetworkAccessForIngestion
                publicNetworkAccessForQuery = this.publicNetworkAccessForQuery
                Tags=this.Tags
                } ]
type  WorkSpaceBuilder() =
    /// Required - creates default "starting" values
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = PerGB2018
          retentionInDays = None
          publicNetworkAccessForIngestion = None
          publicNetworkAccessForQuery = None
          Tags= Map.empty
          }

    [<CustomOperation "name">]
    /// Sets the name of the WorkSpace 
    member _.Name(state: WorkSpaceconfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "sku">]
    /// Sets the name of the SKU/Tier for the Log Analytics .
    member _.Sku(state: WorkSpaceconfig,sku) = { state with Sku = sku }

    [<CustomOperation "retentionInDays">]
    ///The workspace data retention in days. -1 means Unlimited retention for the Unlimited Sku. 730 days is the maximum allowed for all other Skus /Standard and Premium pricing tiers which have fixed data retention of 30 and 365 days respectively..
    member _.RetentionInDays(state: WorkSpaceconfig, retentionInDays) =
        { state with
              retentionInDays = Some retentionInDays }

    [<CustomOperation "publicNetworkAccessForIngestion">]
    /// The network access type for accessing Log Analytics ingestion. - Enabled or Disabled
    member _.PublicNetworkAccessForIngestion(state: WorkSpaceconfig) =
        { state with
              publicNetworkAccessForIngestion = Some"Enabled"}

    [<CustomOperation "publicNetworkAccessForQuery">]
    /// The network access type for accessing Log Analytics query. - Enabled or Disabled
    member _.PublicNetworkAccessForQuery(state: WorkSpaceconfig) =
        { state with
              publicNetworkAccessForQuery = Some "Enabled"}
    [<CustomOperation "add_tags">]
      member _.Tags(state:WorkSpaceconfig, pairs) =
          { state with
              Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
      member this.Tag(state:WorkSpaceconfig, key, value) = this.Tags(state, [ (key,value) ])

let logAnalytics =  WorkSpaceBuilder()


