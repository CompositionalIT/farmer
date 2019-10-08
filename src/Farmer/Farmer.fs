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

/// A ResourceRef represents a linked resource; typically this will be for two resources that have a relationship
/// such as AppInsights on WebApp. WebApps can automatically create and configure an AI instance for the webapp,
/// or configure the web app to an existing AI instance, or do nothing.
type ResourceRef =
    | External of ResourceName
    | AutomaticPlaceholder
    | AutomaticallyCreated of ResourceName
    member this.ResourceNameOpt = match this with External r | AutomaticallyCreated r -> Some r | AutomaticPlaceholder -> None
    member this.ResourceName = this.ResourceNameOpt |> Option.defaultValue ResourceName.Empty

type ConsistencyPolicy = Eventual | ConsistentPrefix | Session | BoundedStaleness of maxStaleness:int * maxIntervalSeconds : int | Strong
type FailoverPolicy = NoFailover | AutoFailover of secondaryLocation:string | MultiMaster of secondaryLocation:string
type CosmosDbIndexKind = Hash | Range
type CosmosDbIndexDataType = Number | String
type SecureParameter =
    | SecureParameter of name:string
    member this.AsArmRef =
        let (SecureParameter value) = this
        sprintf "[parameters('%s')]" value
type FeatureFlag = Enabled | Disabled
type DiskType = StandardSSD_LRS | Standard_LRS | Premium_LRS
type DiskInfo = { Size : int; DiskType : DiskType }

namespace Farmer.Internal

open Farmer

type WebAppExtensions = AppInsightsExtension
type AppInsights =
    { Name : ResourceName 
      Location : string
      LinkedWebsite : ResourceName option }
type StorageAccount =
    { Name : ResourceName 
      Location : string
      Sku : string
      Containers : (string * string) list }
type WebApp =
    { Name : ResourceName 
      ServerFarm : ResourceName
      Location : string
      AppSettings : List<string * string>
      Extensions : WebAppExtensions Set
      AlwaysOn : bool
      Dependencies : ResourceName list
      Kind : string
      LinuxFxVersion : string option
      NetFrameworkVersion : string option
      JavaVersion : string option
      JavaContainer : string option
      JavaContainerVersion : string option
      PhpVersion : string option
      PythonVersion : string option
      Metadata : List<string * string> }
type ServerFarm =
    { Name : ResourceName 
      Location : string
      Sku: string
      WorkerSize : string
      IsDynamic : bool
      Kind : string option
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
    Credentials : {| Username : string; Password : SecureParameter |}
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

type Search =
    { Name : ResourceName
      Location : string
      Sku : string
      HostingMode : string
      ReplicaCount : int
      PartitionCount : int }
module VM =
    type PublicIpAddress =
        { Name : ResourceName
          Location : string
          DomainNameLabel : string option }
    type VirtualNetwork =
        { Name : ResourceName
          Location : string
          AddressSpacePrefixes : string list
          Subnets : {| Name : ResourceName; Prefix : string |} list }
    type NetworkInterface =
        { Name : ResourceName
          Location : string
          IpConfigs :
            {| SubnetName : ResourceName
               PublicIpName : ResourceName |} list
          VirtualNetwork : ResourceName }
    type VirtualMachine =
        { Name : ResourceName
          Location : string
          StorageAccount : ResourceName option
          Size : string
          Credentials : {| Username : string; Password : SecureParameter |}
          Image : {| Publisher : string; Offer : string; Sku : string |}
          OsDisk : DiskInfo
          DataDisks : DiskInfo list
          NetworkInterfaceName : ResourceName }






namespace Farmer
open Farmer.Internal
open Farmer.Internal.VM

type SupportedResource =
    | CosmosAccount of CosmosDbAccount | CosmosSqlDb of CosmosDbSql | CosmosContainer of CosmosDbContainer
    | ServerFarm of ServerFarm | WebApp of WebApp
    | SqlServer of SqlAzure
    | StorageAccount of StorageAccount
    | AppInsights of AppInsights
    | Ip of PublicIpAddress | Vnet of VirtualNetwork | Nic of NetworkInterface | Vm of VirtualMachine
    | AzureSearch of Search
    member this.ResourceName =
        match this with
        | AppInsights x -> x.Name
        | CosmosAccount x -> x.Name
        | CosmosSqlDb x -> x.Name
        | CosmosContainer x -> x.Name
        | ServerFarm x -> x.Name
        | WebApp x -> x.Name
        | SqlServer x -> x.DbName
        | StorageAccount x -> x.Name
        | Ip x -> x.Name
        | Vnet x -> x.Name
        | Nic x -> x.Name
        | Vm x -> x.Name
        | AzureSearch x -> x.Name

[<AutoOpen>]
module Locations =
    let EastAsia = "eastasia"
    let SoutheastAsia = "southeastasia"
    let CentralUS = "centralus"
    let EastUS = "eastus"
    let EastUS2 = "eastus2"
    let WestUS = "westus"
    let NorthCentralUS = "northcentralus"
    let SouthCentralUS = "southcentralus"
    let NorthEurope = "northeurope"
    let WestEurope = "westeurope"
    let JapanWest = "japanwest"
    let JapanEast = "japaneast"
    let BrazilSouth = "brazilsouth"
    let AustraliaEast = "australiaeast"
    let AustraliaSoutheast = "australiasoutheast"
    let SouthIndia = "southindia"
    let CentralIndia = "centralindia"
    let WestIndia = "westindia"

type ArmTemplate =
    { Parameters : string list
      Outputs : (string * string) list
      Resources : SupportedResource list }