namespace Farmer

/// Represents a name of an ARM resource
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
    member this.Map mapper = match this with ResourceName r -> ResourceName (mapper r)

type Location = Location of string member this.Value = match this with (Location l) -> l

/// Represents an expression used within an ARM template
type ArmExpression =
    | ArmExpression of string
    /// Gets the raw value of this expression.
    member this.Value = match this with ArmExpression e -> e
    /// Applies a mapping function that itself returns an expression, to this expression.
    member this.Bind mapper : ArmExpression = mapper this.Value
    /// Applies a mapping function to the expression.
    member this.Map mapper = this.Bind (mapper >> ArmExpression)
    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    member this.Eval() = sprintf "[%s]" this.Value

    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    static member Eval (expression:ArmExpression) = expression.Eval()
    static member Empty = ArmExpression ""

type SecureParameter =
    | SecureParameter of name:string
    member this.Value = match this with SecureParameter value -> value
    /// Gets an ARM expression reference to the password e.g. parameters('my-password')
    member this.AsArmRef = sprintf "parameters('%s')" this.Value |> ArmExpression

[<AutoOpen>]
module ArmExpression =
    /// A helper function used when building complex ARM expressions; lifts a literal string into a quoted ARM expression
    /// e.g. text becomes 'text'. This is useful for working with functions that can mix literal values and parameters.
    let literal = sprintf "'%s'" >> ArmExpression
    /// Generates an ARM expression for concatination.
    let concat values =
        values
        |> Seq.map(fun (r:ArmExpression) -> r.Value)
        |> String.concat ", "
        |> sprintf "concat(%s)"
        |> ArmExpression


namespace Farmer.Models

open Farmer

/// A ResourceRef represents a linked resource; typically this will be for two resources that have a relationship
/// such as AppInsights on WebApp. WebApps can automatically create and configure an AI instance for the webapp,
/// or configure the web app to an existing AI instance, or do nothing.
type ResourceRef =
    | External of ResourceName
    | AutomaticPlaceholder
    | AutomaticallyCreated of ResourceName
    member this.ResourceNameOpt = match this with External r | AutomaticallyCreated r -> Some r | AutomaticPlaceholder -> None
    member this.ResourceName = this.ResourceNameOpt |> Option.defaultValue ResourceName.Empty

namespace Farmer.Resources

open Farmer

/// The consistency policy of a CosmosDB database.
type ConsistencyPolicy = Eventual | ConsistentPrefix | Session | BoundedStaleness of maxStaleness:int * maxIntervalSeconds : int | Strong
/// The failover policy of a CosmosDB database.
type FailoverPolicy = NoFailover | AutoFailover of secondaryLocation:Location | MultiMaster of secondaryLocation:Location
/// The kind of index to use on a CosmoDB container.
type CosmosDbIndexKind = Hash | Range
/// The datatype for the key of index to use on a CosmoDB container.
type CosmosDbIndexDataType = Number | String
/// Whether a specific feature is active or not.
type FeatureFlag = Enabled | Disabled member this.AsBoolean = match this with Enabled -> true | Disabled -> false
/// The type of disk to use.
type DiskType = StandardSSD_LRS | Standard_LRS | Premium_LRS
/// Represents a disk in a VM.
type DiskInfo = { Size : int; DiskType : DiskType }
/// The type of extensions in a web app.
type WebAppExtensions = AppInsightsExtension

namespace Farmer.Models

open Farmer
open Farmer.Resources

type AppInsights =
    { Name : ResourceName
      Location : Location
      LinkedWebsite : ResourceName option }
type StorageContainerAccess =
    | Private
    | Container
    | Blob
type StorageAccount =
    { Name : ResourceName
      Location : Location
      Sku : string
      Containers : (string * StorageContainerAccess) list }
module ContainerGroups =
    [<RequireQualifiedAccess>]
    type ContainerGroupOsType =
        | Windows
        | Linux
    [<RequireQualifiedAccess>]
    type ContainerGroupRestartPolicy =
        | Never
        | Always
        | OnFailure
    [<RequireQualifiedAccess>]
    type ContainerGroupIpAddressType =
        | PublicAddress
        | PrivateAddress
    [<RequireQualifiedAccess>]
    type ContainerPort =
        { Protocol : System.Net.Sockets.ProtocolType
          Port : uint16 }
    [<RequireQualifiedAccess>]
    type ContainerGroupIpAddress =
        { Type : ContainerGroupIpAddressType
          Ports : ContainerPort list }
    /// Gigabytes
    [<RequireQualifiedAccess>]
    type [<Measure>] Gb
    [<RequireQualifiedAccess>]
    type ContainerResourceRequest =
        { Cpu : int
          Memory : float<Gb> }
    [<RequireQualifiedAccess>]
    type ContainerInstance =
        { Name : ResourceName
          Image : string
          Ports : uint16 list
          Resources : ContainerResourceRequest }
    [<RequireQualifiedAccess>]
    type ContainerGroup =
        { Name : ResourceName
          Location : Location
          ContainerInstances : ContainerInstance list
          OsType : ContainerGroupOsType
          RestartPolicy : ContainerGroupRestartPolicy
          IpAddress : ContainerGroupIpAddress }

open ContainerGroups
open System

type WebApp =
    { Name : ResourceName
      ServerFarm : ResourceName
      Location : Location
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
      Location : Location
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
    Location : Location
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
      Location : Location
      ConsistencyPolicy : ConsistencyPolicy
      WriteModel : FailoverPolicy }

type Search =
    { Name : ResourceName
      Location : Location
      Sku : string
      HostingMode : string
      ReplicaCount : int
      PartitionCount : int }

module VM =
    type PublicIpAddress =
        { Name : ResourceName
          Location : Location
          DomainNameLabel : string option }
    type VirtualNetwork =
        { Name : ResourceName
          Location : Location
          AddressSpacePrefixes : string list
          Subnets : {| Name : ResourceName; Prefix : string |} list }
    type NetworkInterface =
        { Name : ResourceName
          Location : Location
          IpConfigs :
            {| SubnetName : ResourceName
               PublicIpName : ResourceName |} list
          VirtualNetwork : ResourceName }
    type VirtualMachine =
        { Name : ResourceName
          Location : Location
          StorageAccount : ResourceName option
          Size : string
          Credentials : {| Username : string; Password : SecureParameter |}
          Image : {| Publisher : string; Offer : string; Sku : string |}
          OsDisk : DiskInfo
          DataDisks : DiskInfo list
          NetworkInterfaceName : ResourceName }
type SecretValue =
    | ParameterSecret of SecureParameter
    | ExpressionSecret of ArmExpression
    member this.Value =
        match this with
        | ParameterSecret secureParameter -> secureParameter.AsArmRef.Eval()
        | ExpressionSecret armExpression -> armExpression.Eval()

type KeyVaultSecret =
    { Name : ResourceName
      Value : SecretValue
      ParentKeyVault : ResourceName
      Location : Location
      ContentType : string option
      Enabled : bool Nullable
      ActivationDate : int Nullable
      ExpirationDate : int Nullable
      Dependencies : ResourceName list }
type KeyVault =
    { Name : ResourceName
      Location : Location
      TenantId : string
      Sku : string
      Uri : string option
      EnabledForDeployment : bool option
      EnabledForDiskEncryption : bool option
      EnabledForTemplateDeployment : bool option
      EnableSoftDelete : bool option
      CreateMode : string option
      EnablePurgeProtection : bool option
      AccessPolicies :
        {| ObjectId : string
           ApplicationId : string option
           Permissions :
            {| Keys : string array
               Secrets : string array
               Certificates : string array
               Storage : string array |}
        |} array
      DefaultAction : string option
      Bypass: string option
      IpRules : string list
      VnetRules : string list }

type QueryStringCacheBehavior = IgnoreQueryString | BypassCaching | UseQueryString | NotSet
type GeoFilterAction = Block | Allow
type CountryCode = CountryCode of string member this.Value = match this with CountryCode x -> x
type RulePriority = Always | Custom of order:int * conditions:string array
type CdnSku = Standard_Verizon | Premium_Verizon | Custom_Verizon | Standard_Akamai | Standard_ChinaCdn | Standard_Microsoft | Premium_ChinaCdn
type CdnCustomDomain = { Name : ResourceName; HostName : string }    
type CdnEndpoint =
    { Name : ResourceName
      OriginHostHeader : string option
      OriginPath : string option
      ContentTypesToCompress : string array
      IsCompressionEnabled : bool option
      IsHttpAllowed : bool option
      IsHttpsAllowed : bool option
      QueryStringCachingBehavior : QueryStringCacheBehavior option
      OptimizationPath : string option
      ProbePath : string option
      GeoFilters :
        {| RelativePath : string
           Action : GeoFilterAction
           CountryCodes: CountryCode array
        |} array
      /// The order in which the rules are applied for the endpoint. Possible values {0,1,2,3,………}. A rule with a lesser order will be applied before a rule with a greater order. Rule with order 0 is a special rule. It does not require any condition and actions listed in it will always be applied.
      DeliveryPolicy :
        {| Description : string option
           Rules : {| Name : string option; Order : int; Conditions : string array; Actions : string array |} array
        |} option
      Origins :
        {| Name : string
           HostName : string
           HttpPort : int option
           HttpsPort : int option
        |} array
      CustomDomains : CdnCustomDomain array
    }
type CdnProfile =
    { Name : ResourceName
      Sku : CdnSku
      Endpoint : CdnEndpoint }        

open VM

type SupportedResource =
    | CosmosAccount of CosmosDbAccount | CosmosSqlDb of CosmosDbSql | CosmosContainer of CosmosDbContainer
    | ServerFarm of ServerFarm | WebApp of WebApp
    | SqlServer of SqlAzure
    | StorageAccount of StorageAccount
    | ContainerGroup of ContainerGroup
    | AppInsights of AppInsights
    | Ip of PublicIpAddress | Vnet of VirtualNetwork | Nic of NetworkInterface | Vm of VirtualMachine
    | AzureSearch of Search
    | KeyVault of KeyVault | KeyVaultSecret of KeyVaultSecret
    | CdnProfile of CdnProfile
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
        | ContainerGroup x -> x.Name
        | Ip x -> x.Name
        | Vnet x -> x.Name
        | Nic x -> x.Name
        | Vm x -> x.Name
        | AzureSearch x -> x.Name
        | KeyVault x -> x.Name
        | KeyVaultSecret x -> x.Name
        | CdnProfile x -> x.Name

namespace Farmer
open Farmer.Models

[<AutoOpen>]
module Locations =
    let EastAsia = Location "eastasia"
    let SoutheastAsia = Location "southeastasia"
    let CentralUS = Location "centralus"
    let EastUS = Location "eastus"
    let EastUS2 = Location "eastus2"
    let WestUS = Location "westus"
    let NorthCentralUS = Location "northcentralus"
    let SouthCentralUS = Location "southcentralus"
    let NorthEurope = Location "northeurope"
    let WestEurope = Location "westeurope"
    let JapanWest = Location "japanwest"
    let JapanEast = Location "japaneast"
    let BrazilSouth = Location "brazilsouth"
    let AustraliaEast = Location "australiaeast"
    let AustraliaSoutheast = Location "australiasoutheast"
    let SouthIndia = Location "southindia"
    let CentralIndia = Location "centralindia"
    let WestIndia = Location "westindia"

type ArmTemplate =
    { Parameters : SecureParameter list
      Outputs : (string * string) list
      Resources : SupportedResource list }