namespace Farmer

type ResourceName =
    | ResourceName of string
    static member Empty = ResourceName ""
    member this.Value =
        let (ResourceName path) = this
        path
    member this.IfEmpty fallbackValue =
        match this with
        | r when r = ResourceName.Empty -> ResourceName fallbackValue
        | r -> r

type ConsistencyPolicy = Eventual | ConsistentPrefix | Session | BoundedStaleness of maxStaleness:int * maxIntervalSeconds : int | Strong
type FailoverPolicy = NoFailover | AutoFailover of secondaryLocation:string | MultiMaster of secondaryLocation:string
type CosmosDbIndexKind = Hash | Range
type CosmosDbIndexDataType = Number | String
type SecureParameter = SecureParameter of name:string
type FeatureFlag = Enabled | Disabled

namespace Farmer.Internal

open Farmer

type WebAppExtensions = AppInsightsExtension
type AppInsights =
    { Name : ResourceName 
      Location : string
      LinkedWebsite: ResourceName }
type StorageAccount =
    { Name : ResourceName 
      Location : string
      Sku : string }
type WebApp =
    { Name : ResourceName 
      ServerFarm : ResourceName
      Location : string
      AppSettings : List<string * string>
      Extensions : WebAppExtensions Set
      Dependencies : ResourceName list
      Kind : string option }
type ServerFarm =
    { Name : ResourceName 
      Location : string
      Sku: string
      WorkerSize : string
      IsDynamic : bool
      Tier : string
      WorkerCount : int }
type CosmosDbContainer =
    { Name : ResourceName
      Account : ResourceName
      Database : ResourceName
      PartitionKey :
        {| Paths : string list
           Kind : CosmosDbIndexKind |}
      IndexingPolicy :
        {| IncludedPaths :
            {| Path : string
               Indexes :
                {| Kind : CosmosDbIndexKind
                   DataType : CosmosDbIndexDataType |} list
            |} list
           ExcludedPaths : string list
        |}
    }

type SqlAzure =
  { ServerName : ResourceName
    Location : string
    AdministratorLogin : string
    AdministratorLoginPassword : SecureParameter
    DbName : ResourceName
    DbEdition : string
    DbCollation : string
    DbObjective : string
    TransparentDataEncryption : FeatureFlag
    FirewallRules : {| Name : string; Start : System.Net.IPAddress; End : System.Net.IPAddress |} list }


type CosmosDbSql =
    { Name : ResourceName
      Account : ResourceName
      Throughput : string }
type CosmosDbAccount =
    { Name : ResourceName
      Location : string
      ConsistencyPolicy : ConsistencyPolicy
      WriteModel : FailoverPolicy }


namespace Farmer
open Farmer.Internal
type SupportedResource =
  | CosmosAccount of CosmosDbAccount | CosmosSqlDb of CosmosDbSql | CosmosContainer of CosmosDbContainer
  | ServerFarm of ServerFarm | WebApp of WebApp
  | SqlServer of SqlAzure
  | StorageAccount of StorageAccount
  | AppInsights of AppInsights

type ArmTemplate =
    { Parameters : string list
      Outputs : (string * string) list
      Resources : SupportedResource list }