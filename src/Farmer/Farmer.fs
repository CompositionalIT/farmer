namespace Farmer

type ResourceName =
    | ResourceName of string
    member this.Value =
        let (ResourceName path) = this
        path
type ConsistencyPolicy = Eventual | ConsistentPrefix | Session | BoundedStaleness of maxStaleness:int * maxIntervalSeconds : int | Strong
type FailoverPolicy = NoFailover | AutoFailover of secondaryLocation:string | MultiMaster of secondaryLocation:string
type CosmosDbIndexKind = Hash | Range
type CosmosDbIndexDataType = Number | String
type SecureParameter = SecureParameter of name:string
type FeatureFlag = Enabled | Disabled

namespace Farmer.Internal

open Farmer

/// A type of ARM resource e.g. Microsoft.Web/serverfarms
type ResourceType =
    | ResourceType of path:string
    member this.Value =
        let (ResourceType path) = this
        path

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

module ResourceType =
    let ServerFarm = ResourceType "Microsoft.Web/serverfarms"
    let WebSite = ResourceType "Microsoft.Web/sites"
    let CosmosDb = ResourceType "Microsoft.DocumentDB/databaseAccounts"
    let CosmosDbSql = ResourceType "Microsoft.DocumentDB/databaseAccounts/apis/databases"
    let CosmosDbSqlContainer = ResourceType "Microsoft.DocumentDb/databaseAccounts/apis/databases/containers"
    let SqlAzure = ResourceType "Microsoft.Sql/servers"
    let StorageAccount = ResourceType "Microsoft.Storage/storageAccounts"
    let AppInsights = ResourceType "Microsoft.Insights/components"

namespace Farmer

type ArmTemplate =
    { Parameters : string list
      Variables : (string * string) list
      Outputs : (string * string) list
      Resources : obj list }