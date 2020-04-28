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
type FailoverPolicy = NoFailover | AutoFailover of secondaryLocation:Locations | MultiMaster of secondaryLocation:Locations
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

namespace Farmer.Models

open Farmer
open Farmer.Resources

type ResourceReplacement<'T> =
  | NewResource of 'T
  | MergedResource of old:'T * replacement:'T
  | CouldNotLocate of ResourceName
  | NotSet

type AppInsights =
    { Name : ResourceName
      Location : Locations
      LinkedWebsite : ResourceName option }
type StorageContainerAccess =
    | Private
    | Container
    | Blob
type StorageAccount =
    { Name : ResourceName
      Location : Locations
      Sku : Sku
      Containers : (string * StorageContainerAccess) list }

type Redis =
    { Name : ResourceName
      Location : Locations
      Sku :
        {| Name : string
           Family : char
           Capacity : int |}
      RedisConfiguration : Map<string, string>
      NonSslEnabled : bool option
      ShardCount : int option
      MinimumTlsVersion : string option }

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
    type ContainerProtocol = TCP | UDP
    [<RequireQualifiedAccess>]
    type ContainerPort =
        { Protocol : ContainerProtocol
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
          Location : Locations
          ContainerInstances : ContainerInstance list
          OsType : ContainerGroupOsType
          RestartPolicy : ContainerGroupRestartPolicy
          IpAddress : ContainerGroupIpAddress }

open ContainerGroups
open System

type WebApp =
    { Name : ResourceName
      ServicePlan : ResourceName
      Location : Locations
      AppSettings : List<string * string>
      AlwaysOn : bool
      HTTPSOnly : bool
      Dependencies : ResourceName list
      Kind : string
      LinuxFxVersion : string option
      AppCommandLine : string option
      NetFrameworkVersion : string option
      JavaVersion : string option
      JavaContainer : string option
      JavaContainerVersion : string option
      PhpVersion : string option
      PythonVersion : string option
      Metadata : List<string * string>
      ZipDeployPath : string option
      Parameters : SecureParameter list }
type ServerFarm =
    { Name : ResourceName
      Location : Locations
      Sku: string
      WorkerSize : string
      IsDynamic : bool
      Kind : string option
      Tier : string
      WorkerCount : int
      IsLinux : bool }
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
    Location : Locations
    Credentials : {| Username : string; Password : SecureParameter |}
    Databases :
        {| Name : ResourceName
           Edition : string
           Collation : string
           Objective : string
           TransparentDataEncryption : FeatureFlag |} list
    FirewallRules :
        {| Name : string
           Start : System.Net.IPAddress
           End : System.Net.IPAddress |} list
  }

type CosmosDbSql =
    { Name : ResourceName
      Account : ResourceName
      Throughput : string }
type CosmosDbAccount =
    { Name : ResourceName
      Location : Locations
      ConsistencyPolicy : ConsistencyPolicy
      WriteModel : FailoverPolicy
      PublicNetworkAccess : FeatureFlag
      FreeTier : bool }

type Search =
    { Name : ResourceName
      Location : Locations
      Sku : string
      HostingMode : string
      ReplicaCount : int
      PartitionCount : int }

type EventHubNamespace =
  { Name : ResourceName
    Location : Locations
    Sku : {| Name : string; Tier : string; Capacity : int |}
    ZoneRedundant : bool option
    IsAutoInflateEnabled : bool option
    MaxThroughputUnits : int option
    KafkaEnabled : bool option }

type EventHub =
  { Name : ResourceName
    Location : Locations
    MessageRetentionDays : int option
    Partitions : int
    Dependencies : ResourceName list }

type EventHubConsumerGroup =
  { Name : ResourceName
    Location : Locations
    Dependencies : ResourceName list }
type EventHubAuthorizationRule =
  { Name : ResourceName
    Location : Locations
    Dependencies : ResourceName list
    Rights : string list }
module VM =
    type PublicIpAddress =
        { Name : ResourceName
          Location : Locations
          DomainNameLabel : string option }
    type VirtualNetwork =
        { Name : ResourceName
          Location : Locations
          AddressSpacePrefixes : string list
          Subnets : {| Name : ResourceName; Prefix : string |} list }
    type NetworkInterface =
        { Name : ResourceName
          Location : Locations
          IpConfigs :
            {| SubnetName : ResourceName
               PublicIpName : ResourceName |} list
          VirtualNetwork : ResourceName }
    type VirtualMachine =
        { Name : ResourceName
          Location : Locations
          StorageAccount : ResourceName option
          Size : VMSize
          Credentials : {| Username : string; Password : SecureParameter |}
          Image : ImageDefinition
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
      Location : Locations
      ContentType : string option
      Enabled : bool Nullable
      ActivationDate : int Nullable
      ExpirationDate : int Nullable
      Dependencies : ResourceName list }
type KeyVault =
    { Name : ResourceName
      Location : Locations
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

open VM

type CognitiveServices =
  { Name : ResourceName
    Location : Locations
    Sku : string
    Kind : string }

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
    | EventHub of EventHub | EventHubNamespace of EventHubNamespace | ConsumerGroup of EventHubConsumerGroup | EventHubAuthRule of EventHubAuthorizationRule
    | RedisCache of Redis
    | CognitiveService of CognitiveServices
    member this.ResourceName =
        match this with
        | AppInsights x -> x.Name
        | CosmosAccount x -> x.Name | CosmosSqlDb x -> x.Name | CosmosContainer x -> x.Name
        | ServerFarm x -> x.Name | WebApp x -> x.Name
        | SqlServer x -> x.ServerName
        | StorageAccount x -> x.Name
        | ContainerGroup x -> x.Name
        | Ip x -> x.Name | Vnet x -> x.Name | Nic x -> x.Name | Vm x -> x.Name
        | AzureSearch x -> x.Name
        | KeyVault x -> x.Name | KeyVaultSecret x -> x.Name
        | EventHub x -> x.Name | EventHubNamespace x -> x.Name | ConsumerGroup x -> x.Name | EventHubAuthRule x -> x.Name
        | RedisCache r -> r.Name
        | CognitiveService c -> c.Name

namespace Farmer
open Farmer.Models

type ArmTemplate =
    { Parameters : SecureParameter list
      Outputs : (string * string) list
      Resources : SupportedResource list }