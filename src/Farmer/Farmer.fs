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
      Sku : string }
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
    module WindowsOsVersion =
        let v2008R2SP1 = "2008-R2-SP1"
        let v2012Datacenter = "2012-Datacenter"
        let v2012R2Datacenter = "2012-R2-Datacenter"
        let v2016NanoServer = "2016-Nano-Server"
        let v2016DatacenterWithContainers = "2016-Datacenter-with-Containers"
        let v2016Datacenter = "2016-Datacenter"
        let v2019Datacenter = "2019-Datacenter"

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

type ArmTemplate =
    { Parameters : string list
      Outputs : (string * string) list
      Resources : SupportedResource list }