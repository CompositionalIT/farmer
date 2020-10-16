namespace Farmer

open System

[<AutoOpen>]
module internal DuHelpers =
    let makeAll<'TUnion> =
        Reflection.FSharpType.GetUnionCases(typeof<'TUnion>)
        |> Array.map(fun t -> Reflection.FSharpValue.MakeUnion(t, null) :?> 'TUnion)
        |> Array.toList

[<AutoOpen>]
module LocationExtensions =
    type Location with
        static member EastAsia = Location "EastAsia"
        static member SoutheastAsia = Location "SoutheastAsia"
        static member CentralUS = Location "CentralUS"
        static member EastUS = Location "EastUS"
        static member EastUS2 = Location "EastUS2"
        static member WestUS = Location "WestUS"
        static member NorthCentralUS = Location "NorthCentralUS"
        static member SouthCentralUS = Location "SouthCentralUS"
        static member NorthEurope = Location "NorthEurope"
        static member WestEurope = Location "WestEurope"
        static member JapanWest = Location "JapanWest"
        static member JapanEast = Location "JapanEast"
        static member BrazilSouth = Location "BrazilSouth"
        static member AustraliaEast = Location "AustraliaEast"
        static member AustraliaSoutheast = Location "AustraliaSoutheast"
        static member SouthIndia = Location "SouthIndia"
        static member CentralIndia = Location "CentralIndia"
        static member WestIndia = Location "WestIndia"
        static member CanadaCentral = Location "CanadaCentral"
        static member CanadaEast = Location "CanadaEast"
        static member UKSouth = Location "UKSouth"
        static member UKWest = Location "UKWest"
        static member WestCentralUS = Location "WestCentralUS"
        static member WestUS2 = Location "WestUS2"
        static member KoreaCentral = Location "KoreaCentral"
        static member KoreaSouth = Location "KoreaSouth"
        static member FranceCentral = Location "FranceCentral"
        static member FranceSouth = Location "FranceSouth"
        static member AustraliaCentral = Location "AustraliaCentral"
        static member AustraliaCentral2 = Location "AustraliaCentral2"
        static member UAECentral = Location "UAECentral"
        static member UAENorth = Location "UAENorth"
        static member SouthAfricaNorth = Location "SouthAfricaNorth"
        static member SouthAfricaWest = Location "SouthAfricaWest"
        static member SwitzerlandNorth = Location "SwitzerlandNorth"
        static member SwitzerlandWest = Location "SwitzerlandWest"
        static member GermanyNorth = Location "GermanyNorth"
        static member GermanyWestCentral = Location "GermanyWestCentral"
        static member NorwayWest = Location "NorwayWest"
        static member NorwayEast = Location "NorwayEast"
        static member Global = Location "global"

type OS = Windows | Linux

type [<Measure>] Gb
type [<Measure>] Mb
type [<Measure>] Mbps
type [<Measure>] Days
type [<Measure>] VCores
type IsoDateTime =
    | IsoDateTime of string
    static member OfTimeSpan (d:TimeSpan) = d |> System.Xml.XmlConvert.ToString |> IsoDateTime
    member this.Value = match this with IsoDateTime value -> value
type TransmissionProtocol = TCP | UDP
type TlsVersion = Tls10 | Tls11 | Tls12
module Mb =
    let toBytes (mb:int<Mb>) = int64 mb * 1024L * 1024L
module Vm =
    type VMSize =
    | Basic_A0
    | Basic_A1
    | Basic_A2
    | Basic_A3
    | Basic_A4
    | Standard_A0
    | Standard_A1
    | Standard_A2
    | Standard_A3
    | Standard_A4
    | Standard_A5
    | Standard_A6
    | Standard_A7
    | Standard_A8
    | Standard_A9
    | Standard_A10
    | Standard_A11
    | Standard_A1_v2
    | Standard_A2_v2
    | Standard_A4_v2
    | Standard_A8_v2
    | Standard_A2m_v2
    | Standard_A4m_v2
    | Standard_A8m_v2
    | Standard_B1s
    | Standard_B1ms
    | Standard_B2s
    | Standard_B2ms
    | Standard_B4ms
    | Standard_B8ms
    | Standard_D1
    | Standard_D2
    | Standard_D3
    | Standard_D4
    | Standard_D11
    | Standard_D12
    | Standard_D13
    | Standard_D14
    | Standard_D1_v2
    | Standard_D2_v2
    | Standard_D3_v2
    | Standard_D4_v2
    | Standard_D5_v2
    | Standard_D2_v3
    | Standard_D4_v3
    | Standard_D8_v3
    | Standard_D16_v3
    | Standard_D32_v3
    | Standard_D64_v3
    | Standard_D2s_v3
    | Standard_D4s_v3
    | Standard_D8s_v3
    | Standard_D16s_v3
    | Standard_D32s_v3
    | Standard_D64s_v3
    | Standard_D11_v2
    | Standard_D12_v2
    | Standard_D13_v2
    | Standard_D14_v2
    | Standard_D15_v2
    | Standard_DS1
    | Standard_DS2
    | Standard_DS3
    | Standard_DS4
    | Standard_DS11
    | Standard_DS12
    | Standard_DS13
    | Standard_DS14
    | Standard_DS1_v2
    | Standard_DS2_v2
    | Standard_DS3_v2
    | Standard_DS4_v2
    | Standard_DS5_v2
    | Standard_DS11_v2
    | Standard_DS12_v2
    | Standard_DS13_v2
    | Standard_DS14_v2
    | Standard_DS15_v2
    | Standard_DS13_4_v2
    | Standard_DS13_2_v2
    | Standard_DS14_8_v2
    | Standard_DS14_4_v2
    | Standard_E2_v3_v3
    | Standard_E4_v3
    | Standard_E8_v3
    | Standard_E16_v3
    | Standard_E32_v3
    | Standard_E64_v3
    | Standard_E2s_v3
    | Standard_E4s_v3
    | Standard_E8s_v3
    | Standard_E16s_v3
    | Standard_E32s_v3
    | Standard_E64s_v3
    | Standard_E32_16_v3
    | Standard_E32_8s_v3
    | Standard_E64_32s_v3
    | Standard_E64_16s_v3
    | Standard_F1
    | Standard_F2
    | Standard_F4
    | Standard_F8
    | Standard_F16
    | Standard_F1s
    | Standard_F2s
    | Standard_F4s
    | Standard_F8s
    | Standard_F16s
    | Standard_F2s_v2
    | Standard_F4s_v2
    | Standard_F8s_v2
    | Standard_F16s_v2
    | Standard_F32s_v2
    | Standard_F64s_v2
    | Standard_F72s_v2
    | Standard_G1
    | Standard_G2
    | Standard_G3
    | Standard_G4
    | Standard_G5
    | Standard_GS1
    | Standard_GS2
    | Standard_GS3
    | Standard_GS4
    | Standard_GS5
    | Standard_GS4_8
    | Standard_GS4_4
    | Standard_GS5_16
    | Standard_GS5_8
    | Standard_H8
    | Standard_H16
    | Standard_H8m
    | Standard_H16m
    | Standard_H16r
    | Standard_H16mr
    | Standard_L4s
    | Standard_L8s
    | Standard_L16s
    | Standard_L32s
    | Standard_M64s
    | Standard_M64ms
    | Standard_M128s
    | Standard_M128ms
    | Standard_M64_32ms
    | Standard_M64_16ms
    | Standard_M128_64ms
    | Standard_M128_32ms
    | Standard_NC6
    | Standard_NC12
    | Standard_NC24
    | Standard_NC24r
    | Standard_NC6s_v2
    | Standard_NC12s_v2
    | Standard_NC24s_v2
    | Standard_NC24rs_v2
    | Standard_NC6s_v3
    | Standard_NC12s_v3
    | Standard_NC24s_v3
    | Standard_NC24rs_v3
    | Standard_ND6s
    | Standard_ND12s
    | Standard_ND24s
    | Standard_ND24rs
    | Standard_NV6
    | Standard_NV12
    | Standard_NV24
    | CustomImage of string
        member this.ArmValue = match this with CustomImage c -> c | _ -> this.ToString()
    type Offer = Offer of string member this.ArmValue = match this with Offer o -> o
    type Publisher = Publisher of string member this.ArmValue = match this with Publisher p -> p
    type VmImageSku = ImageSku of string member this.ArmValue = match this with ImageSku i -> i
    type ImageDefinition =
        { Offer : Offer
          Publisher : Publisher
          Sku : VmImageSku
          OS : OS }
    let makeVm os offer publisher sku = { Offer = Offer offer; Publisher = Publisher publisher; OS = os; Sku = ImageSku sku }
    let makeWindowsVm = makeVm Windows "WindowsServer" "MicrosoftWindowsServer"
    let makeLinuxVm = makeVm Linux

    let CentOS_75 = makeLinuxVm "CentOS" "OpenLogic" "7.5"
    let CoreOS_Stable = makeLinuxVm "CoreOS" "CoreOS" "Stable"
    let debian_10 = makeLinuxVm "debian-10" "Debian" "10"
    let openSUSE_423 = makeLinuxVm "openSUSE-Leap" "SUSE" "42.3"
    let RHEL_7RAW = makeLinuxVm "RHEL" "RedHat" "7-RAW"
    let SLES_15 = makeLinuxVm "SLES" "SUSE" "15"
    let UbuntuServer_1804LTS = makeLinuxVm "UbuntuServer" "Canonical" "18.04-LTS"

    let WindowsServer_2019Datacenter = makeWindowsVm "2019-Datacenter"
    let WindowsServer_2016Datacenter = makeWindowsVm "2016-Datacenter"
    let WindowsServer_2012R2Datacenter = makeWindowsVm "2012-R2-Datacenter"
    let WindowsServer_2012Datacenter = makeWindowsVm "2012-Datacenter"
    let WindowsServer_2008R2SP1 = makeWindowsVm "2008-R2-SP1"
    /// The type of disk to use.
    type DiskType =
    | StandardSSD_LRS
    | Standard_LRS
    | Premium_LRS
        member this.ArmValue = match this with x -> x.ToString()

    /// Represents a disk in a VM.
    type DiskInfo = { Size : int; DiskType : DiskType }

module internal Validation =
    let isNonEmpty entity s = if String.IsNullOrWhiteSpace s then Error (sprintf "%s cannot be empty" entity) else Ok()
    let notLongerThan max entity (s:string) = if s.Length > max then Error (sprintf "%s max length is %d, but here is %d ('%s')" entity max s.Length s) else Ok()
    let notShorterThan min entity (s:string) = if s.Length < min then Error (sprintf "%s min length is %d, but here is %d ('%s')" entity min s.Length s) else Ok()
    let lengthBetween min max entity (s:string) = s |> notLongerThan max entity |> Result.bind (fun _ -> s |> notShorterThan min entity)
    let containsOnly message predicate entity (s:string) = if s |> Seq.exists (predicate >> not) then Error (sprintf "%s can only contain %s ('%s')" entity message s) else Ok()
    let cannotContain message predicate entity (s:string) = if s |> Seq.exists predicate then Error (sprintf "%s do not allow %s ('%s')" entity message s) else Ok()
    let startsWith message predicate entity (s:string) = if not (predicate s.[0]) then Error (sprintf "%s must start with %s ('%s')" entity message s) else Ok()
    let endsWith message predicate entity (s:string) = if not (predicate s.[s.Length - 1]) then Error (sprintf "%s must end with %s ('%s')" entity message s) else Ok()
    let cannotStartWith message predicate entity (s:string) = if predicate s.[0] then Error (sprintf "%s cannot start with %s ('%s')" entity message s) else Ok()
    let cannotEndWith message predicate entity (s:string) = if predicate s.[s.Length - 1] then Error (sprintf "%s cannot end with %s ('%s')" entity message s) else Ok()
    let arb message predicate entity (s:string) = if predicate s then Error (sprintf "%s %s ('%s')" entity message s) else Ok()
    let (<+>) a b v = a v && b v
    let (<|>) a b v = a v || b v
    let lowercaseOnly = Char.IsLetter >> not <|> Char.IsLower

    let validate entity text rules =
        rules
        |> Seq.choose (fun v ->
            match v entity text with
            | Error m -> Some (Error m)
            | Ok _ -> None)
        |> Seq.tryHead
        |> Option.defaultValue (Ok text)

module Storage =
    open Validation
    type StorageAccountName =
        private | StorageAccountName of ResourceName
        static member Create name =
            [ isNonEmpty
              lengthBetween 3 24
              containsOnly "lowercase letters" lowercaseOnly
              containsOnly "alphanumeric characters" Char.IsLetterOrDigit ]
            |> validate "Storage account names" name
            |> Result.map (ResourceName >> StorageAccountName)

        static member Create (ResourceName name) = StorageAccountName.Create name
        member this.ResourceName = match this with StorageAccountName name -> name

    type StorageResourceName =
        private | StorageResourceName of ResourceName
        static member Create name =
            [ isNonEmpty
              lengthBetween 3 63
              startsWith "an alphanumeric character" Char.IsLetterOrDigit
              endsWith "an alphanumeric character" Char.IsLetterOrDigit
              containsOnly "letters, numbers, and the dash (-) character" (fun c -> Char.IsLetterOrDigit c || c = '-')
              containsOnly "lowercase letters" lowercaseOnly
              arb "do not allow consecutive dashes" (fun s -> s.Contains "--") ]
            |> validate "Storage resource names" name
            |> Result.map (ResourceName >> StorageResourceName)

        static member Create (ResourceName name) = StorageResourceName.Create name
        member this.ResourceName = match this with StorageResourceName name -> name

    type Sku =
        | Standard_LRS
        | Standard_GRS
        | Standard_RAGRS
        | Standard_ZRS
        | Standard_GZRS
        | Standard_RAGZRS
        | Premium_LRS
        | Premium_ZRS
        member this.ArmValue = this.ToString()

    type StorageContainerAccess =
    | Private
    | Container
    | Blob

    /// The type of action to take when defining a lifecycle policy.
    type LifecyclePolicyAction =
    | CoolAfter of int<Days>
    | ArchiveAfter of int<Days>
    | DeleteAfter of int<Days>
    | DeleteSnapshotAfter of int<Days>

    /// Represents no filters for a lifecycle rule
    let NoRuleFilters : string list = []

module WebApp =
    type WorkerSize = Small | Medium | Large | Serverless
    type Cors = AllOrigins | SpecificOrigins of origins : Uri list * allowCredentials : bool option
    type Sku =
        | Shared
        | Free
        | Basic of string
        | Standard of string
        | Premium of string
        | PremiumV2 of string
        | Isolated of string
        | Dynamic
        static member D1 = Shared
        static member F1 = Free
        static member B1 = Basic "B1"
        static member B2 = Basic "B2"
        static member B3 = Basic "B3"
        static member S1 = Standard "S1"
        static member S2 = Standard "S2"
        static member S3 = Standard "S3"
        static member P1 = Premium "P1"
        static member P2 = Premium "P2"
        static member P3 = Premium "P3"
        static member P1V2 = PremiumV2 "P1V2"
        static member P2V2 = PremiumV2 "P2V2"
        static member P3V2 = PremiumV2 "P3V2"
        static member I1 = Isolated "I1"
        static member I2 = Isolated "I2"
        static member I3 = Isolated "I3"
        static member Y1 = Dynamic
    type ConnectionStringKind = MySql | SQLServer | SQLAzure | Custom | NotificationHub | ServiceBus | EventHub | ApiHub | DocDb | RedisCache | PostgreSQL

module CognitiveServices =
    /// Type of SKU. See https://github.com/Azure/azure-quickstart-templates/tree/master/101-cognitive-services-translate
    type Sku =
        /// Free Tier
        | F0
        | S0
        | S1
        | S2
        | S3
        | S4

    type Kind =
        | AllInOne
        | AnomalyDetector
        | Bing_Autosuggest_v7 | Bing_CustomSearch | Bing_EntitySearch | Bing_Search_v7 | Bing_SpellCheck_v7
        | CognitiveServices
        | ComputerVision
        | ContentModerator
        | CustomVision_Prediction | CustomVision_Training
        | Face
        | FormRecognizer
        | ImmersiveReader
        | InkRecognizer
        | LUIS | LUIS_Authoring
        | Personalizer
        | QnAMaker
        | SpeakerRecognition
        | SpeechServices
        | TextAnalytics
        | TextTranslation

module ContainerRegistry =
    /// Container Registry SKU
    type Sku =
        | Basic
        | Standard
        | Premium

module Search =
    type HostingMode = Default | HighDensity
    /// The SKU of the search service you want to create. E.g. free or standard.
    type Sku =
        | Free
        | Basic
        | Standard
        | Standard2
        | Standard3 of HostingMode
        | StorageOptimisedL1
        | StorageOptimisedL2

module Sql =
    [<Measure>] type DTU

    type Gen5Series =
        | Gen5_2
        | Gen5_4
        | Gen5_6
        | Gen5_8
        | Gen5_10
        | Gen5_12
        | Gen5_14
        | Gen5_16
        | Gen5_18
        | Gen5_20
        | Gen5_24
        | Gen5_32
        | Gen5_40
        | Gen5_80
        member this.Name = Reflection.FSharpValue.GetUnionFields(this, typeof<Gen5Series>) |> fun (v,_) -> v.Name

    type FSeries =
        | Fsv2_8
        | Fsv2_10
        | Fsv2_12
        | Fsv2_14
        | Fsv2_16
        | Fsv2_18
        | Fsv2_20
        | Fsv2_24
        | Fsv2_32
        | Fsv2_36
        | Fsv2_72
        member this.Name = Reflection.FSharpValue.GetUnionFields(this, typeof<FSeries>) |> fun (v,_) -> v.Name

    type MSeries =
        | M_8
        | M_10
        | M_12
        | M_14
        | M_16
        | M_18
        | M_20
        | M_24
        | M_32
        | M_64
        | M_128
        member this.Name = Reflection.FSharpValue.GetUnionFields(this, typeof<MSeries>) |> fun (v,_) -> v.Name

    type VCoreSku =
        | MemoryIntensive of MSeries
        | CpuIntensive of FSeries
        | GeneralPurpose of Gen5Series
        | BusinessCritical of Gen5Series
        | Hyperscale of Gen5Series
        member this.Edition =
            match this with
            | GeneralPurpose _ | CpuIntensive _ -> "GeneralPurpose"
            | BusinessCritical _ | MemoryIntensive _ -> "BusinessCritical"
            | Hyperscale _ -> "Hyperscale"
         member this.Name =
            match this with
            | GeneralPurpose g -> "GP_" + g.Name
            | BusinessCritical b -> "BC_" + b.Name
            | Hyperscale h -> "HS_" + h.Name
            | MemoryIntensive m -> "BC_" + m.Name
            | CpuIntensive c -> "GP_" + c.Name

    type DtuSku =
        | Free
        | Basic
        | Standard of string
        | Premium of string
        static member S0 = Standard "S0"
        static member S1 = Standard "S1"
        static member S2 = Standard "S2"
        static member S3 = Standard "S3"
        static member S4 = Standard "S4"
        static member S6 = Standard "S6"
        static member S7 = Standard "S7"
        static member S9 = Standard "S9"
        static member S12 = Standard "S12"
        static member P1 = Premium "P1"
        static member P2 = Premium "P2"
        static member P4 = Premium "P4"
        static member P6 = Premium "P6"
        static member P11 = Premium "P11"
        static member P15 = Premium "P15"
        member this.Edition =
            match this with
            | Free -> "Free"
            | Basic -> "Basic"
            | Standard _ -> "Standard"
            | Premium _ -> "Premium"
         member this.Name =
            match this with
            | Free -> "Free"
            | Basic -> "Basic"
            | Standard s -> s
            | Premium p -> p

    type SqlLicense =
        | AzureHybridBenefit
        | LicenseRequired
        member this.ArmValue = match this with AzureHybridBenefit -> "BasePrice" | LicenseRequired -> "LicenseIncluded"
    type DbPurchaseModel =
        | DTU of DtuSku
        | VCore of VCoreSku * SqlLicense
        member this.Edition = match this with DTU d -> d.Edition | VCore (v, _) -> v.Edition
        member this.Name = match this with DTU d -> d.Name | VCore (v, _) -> v.Name

    type PoolSku =
        | BasicPool of int
        | StandardPool of int
        | PremiumPool of int
        static member Standard50 = StandardPool 50
        static member Standard100 = StandardPool 100
        static member Standard200 = StandardPool 200
        static member Standard300 = StandardPool 300
        static member Standard400 = StandardPool 400
        static member Standard800 = StandardPool 800
        static member Standard1200 = StandardPool 1200
        static member Standard1600 = StandardPool 1600
        static member Standard2000 = StandardPool 2000
        static member Standard2500 = StandardPool 2500
        static member Standard3000 = StandardPool 3000
        static member Premium125 = PremiumPool 125
        static member Premium250 = PremiumPool 250
        static member Premium500 = PremiumPool 500
        static member Premium1000 = PremiumPool 1000
        static member Premium1500 = PremiumPool 1500
        static member Premium2000 = PremiumPool 2000
        static member Premium2500 = PremiumPool 2500
        static member Premium3000 = PremiumPool 3000
        static member Premium3500 = PremiumPool 3500
        static member Premium4000 = PremiumPool 4000
        static member Basic50 = BasicPool 50
        static member Basic100 = BasicPool 100
        static member Basic200 = BasicPool 200
        static member Basic300 = BasicPool 300
        static member Basic400 = BasicPool 400
        static member Basic800 = BasicPool 800
        static member Basic1200 = BasicPool 1200
        static member Basic1600 = BasicPool 1600
        member this.Name =
            match this with
            | BasicPool _ -> "BasicPool"
            | StandardPool _ -> "StandardPool"
            | PremiumPool _ -> "PremiumPool"
        member this.Edition =
            match this with
            | BasicPool _ -> "Basic"
            | StandardPool _ -> "Standard"
            | PremiumPool _ -> "Premium"
        member this.Capacity =
            match this with
            | BasicPool c
            | StandardPool c
            | PremiumPool c ->
                c

module ManagedIdentity =
    open Farmer.CoreTypes

    /// A user assigned managed identity that can be associated with a resource.
    type UserAssignedIdentity =
        | UserAssignedIdentity of ResourceId
        member this.ResourceId = match this with UserAssignedIdentity rId -> rId
    /// Represents an identity that can be assigned to a resource or used for a permission claim.
    type ResourceIdentity =
        | SystemAssigned of ResourceId option
        | UserAssigned of UserAssignedIdentity list
        member this.PrincipalId =
            match this with
            | UserAssigned (identity :: _) ->
                let identityExpr = identity.ResourceId.ArmExpression.Value
                ArmExpression
                    .create(sprintf "reference(%s).principalId" identityExpr)
                    .WithOwner(identity.ResourceId)
            | SystemAssigned resourceId ->
                let identity = resourceId.Value.ArmExpression.Value
                ArmExpression
                    .create(sprintf "reference(%s, '2019-08-01', 'full').identity.principalId" identity)
                    .WithOwner(resourceId.Value)
            | UserAssigned [] ->
                failwith "No user assignments!"
            |> PrincipalId
module ContainerGroup =
    type PortAccess = PublicPort | InternalPort
    type RestartPolicy = NeverRestart | AlwaysRestart | RestartOnFailure
    /// Identity settings for a container group.
    type ContainerGroupIdentity = ResourceIdentity
    type IpAddressType =
        | PublicAddress
        | PublicAddressWithDns of DnsName:string
        | PrivateAddress
    /// A secret file which will be encoded as base64 and attached to a container group.
    type SecretFile = SecretFile of Name:string * Secret:byte array
    /// A container group volume.
    [<RequireQualifiedAccess>]
    type Volume =
        /// Mounts an empty directory on the container group.
        | EmptyDirectory
        /// Mounts an Azure File Share in the same resource group, performing a key lookup.
        | AzureFileShare of ShareName:ResourceName * StorageAccountName:Storage.StorageAccountName
        /// A git repo volume, clonable by public HTTPS access.
        | GitRepo of Repository:Uri * Directory:string option * Revision:string option
        /// Mounts a volume containing secret files.
        | Secret of SecretFile list

module ContainerService =
    type NetworkPlugin =
        | Kubenet
        | AzureCni
        member this.ArmValue =
            match this with
            | Kubenet -> "kubenet"
            | AzureCni -> "azure"

module Redis =
    type Sku = Basic | Standard | Premium

module EventHub =
    /// The SKU of the event hub instance.
    type EventHubSku =
        | Basic
        | Standard
        | Premium
    type InflateSetting = ManualInflate | AutoInflate of maxThroughput:int
    type AuthorizationRuleRight = Manage | Send | Listen

module KeyVault =
    type Bypass = AzureServices | NoTraffic
    type SoftDeletionMode = SoftDeleteWithPurgeProtection | SoftDeletionOnly
    type DefaultAction = Allow | Deny
    type Key = Encrypt | Decrypt | WrapKey | UnwrapKey | Sign | Verify | Get | List | Create | Update | Import | Delete | Backup | Restore | Recover | Purge static member All = makeAll<Key>
    type Secret = Get | List | Set | Delete | Backup | Restore | Recover | Purge static member All = makeAll<Secret> static member ReadSecrets = [ Get; List ]
    type Certificate = Get | List | Delete | Create | Import | Update | ManageContacts | GetIssuers | ListIssuers | SetIssuers | DeleteIssuers | ManageIssuers | Recover | Purge | Backup | Restore static member All = makeAll<Certificate>
    type Storage = Get | List | Delete | Set | Update | RegenerateKey | Recover | Purge | Backup | Restore | SetSas | ListSas | GetSas | DeleteSas static member All = makeAll<Storage>

    type Sku =
    | Standard
    | Premium
        member this.ArmValue =
            match this with
            | Standard -> "standard"
            | Premium -> "premium"
module ExpressRoute =
    type Tier = Standard | Premium
    type Family = UnlimitedData | MeteredData
    type PeeringType = AzurePrivatePeering | MicrosoftPeering member this.Value = this.ToString()

module VirtualNetworkGateway =
    type PrivateIpAllocationMethod = DynamicPrivateIp | StaticPrivateIp of System.Net.IPAddress
    [<RequireQualifiedAccess>]
    type ErGatewaySku =
        | Standard
        | HighPerformance
        | UltraPerformance
        | ErGw1AZ
        | ErGw2AZ
        | ErGw3AZ
        member this.ArmValue =
            match this with
            | Standard -> "Standard"
            | HighPerformance -> "HighPerformance"
            | UltraPerformance -> "UltraPerformance"
            | ErGw1AZ -> "ErGw1AZ"
            | ErGw2AZ -> "ErGw2AZ"
            | ErGw3AZ -> "ErGw3AZ"
    [<RequireQualifiedAccess>]
    type VpnGatewaySku =
        | Basic
        | VpnGw1
        | VpnGw1AZ
        | VpnGw2
        | VpnGw2AZ
        | VpnGw3
        | VpnGw3AZ
        | VpnGw4
        | VpnGw4AZ
        | VpnGw5
        | VpnGw5AZ
        member this.ArmValue =
            match this with
            | Basic -> "Basic"
            | VpnGw1 -> "VpnGw1"
            | VpnGw1AZ -> "VpnGw1AZ"
            | VpnGw2 -> "VpnGw2"
            | VpnGw2AZ -> "VpnGw2AZ"
            | VpnGw3 -> "VpnGw3"
            | VpnGw3AZ -> "VpnGw3AZ"
            | VpnGw4 -> "VpnGw4"
            | VpnGw4AZ -> "VpnGw4AZ"
            | VpnGw5 -> "VpnGw5"
            | VpnGw5AZ -> "VpnGw5AZ"
    [<RequireQualifiedAccess>]
    type VpnType =
        | PolicyBased
        | RouteBased
        member this.ArmValue =
            match this with
            | PolicyBased -> "PolicyBased"
            | RouteBased-> "RouteBased"
    [<RequireQualifiedAccess>]
    type GatewayType =
        | ExpressRoute of ErGatewaySku
        | Vpn of VpnGatewaySku
        member this.ArmValue =
            match this with
            | ExpressRoute _ -> "ExpressRoute"
            | Vpn _ -> "Vpn"
    [<RequireQualifiedAccess>]
    type ConnectionType =
        | ExpressRoute
        | IPsec
        | Vnet2Vnet
        member this.ArmValue =
            match this with
            | ExpressRoute _ -> "ExpressRoute"
            | IPsec -> "IPsec"
            | Vnet2Vnet -> "Vnet2Vnet"
module ServiceBus =
    type MessagingUnits = OneUnit | TwoUnits | FourUnits
    type Sku =
        | Basic
        | Standard
        | Premium of MessagingUnits
    type Rule =
        | SqlFilter of ResourceName * SqlExpression : string
        | CorrelationFilter of Name : ResourceName * CorrelationId : string option * Properties : Map<string, string>
        member this.Name =
            match this with
            | SqlFilter (name, _)
            | CorrelationFilter(name, _, _) -> name
        static member CreateCorrelationFilter (name, properties, ?correlationId) =
            CorrelationFilter (ResourceName name, correlationId, Map properties)
        static member CreateSqlFilter (name, expression) =
            SqlFilter (ResourceName name, expression)

module CosmosDb =
    /// The consistency policy of a CosmosDB account.
    type ConsistencyPolicy = Eventual | ConsistentPrefix | Session | BoundedStaleness of maxStaleness:int * maxIntervalSeconds : int | Strong
    /// The failover policy of a CosmosDB account.
    type FailoverPolicy = NoFailover | AutoFailover of secondaryLocation:Location | MultiMaster of secondaryLocation:Location
    /// The kind of index to use on a CosmoDB container.
    type IndexKind = Hash | Range
    /// The datatype for the key of index to use on a CosmoDB container.
    type IndexDataType = Number | String
    /// A request unit.
    [<Measure>]
    type RU

module PostgreSQL =
    type Sku =
        | Basic
        | GeneralPurpose
        | MemoryOptimized
        member this.Name =
            match this with
            | Basic -> "B"
            | GeneralPurpose -> "GP"
            | MemoryOptimized -> "MO"
    type Version = VS_9_5 | VS_9_6 | VS_10 | VS_11

module IotHub =
    type Sku = F1 | B1 | B2 | B3 | S1 | S2 | S3
    type Policy =
        | IotHubOwner | Service | Device | RegistryRead | RegistryReadWrite
        member this.Index =
            match this with
            | IotHubOwner -> 0
            | Service -> 1
            | Device -> 2
            | RegistryRead -> 3
            | RegistryReadWrite -> 4

module Maps =
    type Sku = S0 | S1

module SignalR =
    type Sku = Free | Standard

module DataLake =
    type Sku =
    | Consumption
    | Commitment_1TB
    | Commitment_10TB
    | Commitment_100TB
    | Commitment_500TB
    | Commitment_1PB
    | Commitment_5PB

/// A network represented by an IP address and CIDR prefix.
type public IPAddressCidr =
    { Address : System.Net.IPAddress
      Prefix : int }

/// Functions for IP networks and CIDR notation.
module IPAddressCidr =
    let parse (s:string) : IPAddressCidr =
        match s.Split([|'/'|], System.StringSplitOptions.RemoveEmptyEntries) with
        [| ip; prefix |] ->
            { Address = System.Net.IPAddress.Parse (ip.Trim ())
              Prefix = int prefix }
        | _ -> raise (System.ArgumentOutOfRangeException "Malformed CIDR, expecting an IP and prefix separated by '/'")
    let safeParse (s:string) : Result<IPAddressCidr, System.Exception> =
        try parse s |> Ok
        with ex -> Error ex
    let format (cidr:IPAddressCidr) = sprintf "%O/%d" cidr.Address cidr.Prefix
    /// Gets uint32 representation of an IP address.
    let private num (ip:System.Net.IPAddress) =
        ip.GetAddressBytes() |> Array.rev |> fun bytes -> BitConverter.ToUInt32 (bytes, 0)
    /// Gets IP address from uint32 representations
    let private ofNum (num:uint32) =
        num |> BitConverter.GetBytes |> Array.rev |> System.Net.IPAddress
    let private ipRangeNums (cidr:IPAddressCidr) =
        let ipNumber = cidr.Address |> num
        let mask = 0xffffffffu <<< (32 - cidr.Prefix)
        ipNumber &&& mask, ipNumber ||| (mask ^^^ 0xffffffffu)
    /// Indicates if one CIDR block can fit entirely within another CIDR block
    let contains (inner:IPAddressCidr) (outer:IPAddressCidr) =
        // outer |> IPAddressCidr.contains inner
        let innerStart, innerFinish = ipRangeNums inner
        let outerStart, outerFinish = ipRangeNums outer
        outerStart <= innerStart && outerFinish >= innerFinish
    /// Calculates a range of IP addresses from an CIDR block.
    let ipRange (cidr:IPAddressCidr) =
        let first, last = ipRangeNums cidr
        first |> ofNum, last |> ofNum
    /// Sequence of IP addresses for a CIDR block.
    let addresses (cidr:IPAddressCidr) =
        let first, last = ipRangeNums cidr
        seq { for i in first..last do ofNum i }
    /// Carve a subnet out of an address space.
    let carveAddressSpace (addressSpace:IPAddressCidr) (subnetSizes:int list) =
        let addressSpaceStart, addressSpaceEnd = addressSpace |> ipRangeNums
        let mutable startAddress = addressSpaceStart |> ofNum
        let mutable index = 0
        seq {
            for size in subnetSizes do
                index <- index + 1
                let cidr = { Address = startAddress; Prefix = size }
                let first, last = cidr |> ipRangeNums
                let overlapping = first < (startAddress |> num)
                let last, cidr =
                    if overlapping then
                        let cidr = { Address = ofNum (last + 1u); Prefix = size }
                        let _, last = cidr |> ipRangeNums
                        last, cidr
                    else
                        last, cidr
                if last <= addressSpaceEnd then
                    startAddress <- (last + 1u) |> ofNum
                    cidr
                else
                    raise (IndexOutOfRangeException (sprintf "Unable to create subnet %d of /%d" index size))
        }
    /// The first two addresses are the network address and gateway address
    /// so not assignable.
    let assignable (cidr:IPAddressCidr) =
        if cidr.Prefix < 31 then // only has 2 addresses
            cidr |> addresses |> Seq.skip 2
        else
            Seq.empty

module NetworkSecurity =
    type Operation =
    | Allow
    | Deny
    module Operation =
        let ArmValue = function
        | Allow -> "Allow"
        | Deny -> "Deny"
    type Operation with
        member this.ArmValue = this |> Operation.ArmValue

    /// Network protocol supported in network security group rules.
    type NetworkProtocol =
    /// Any protocol
    | AnyProtocol
    /// Transmission Control Protocol
    | TCP
    /// User Datagram Protocol
    | UDP
    /// Internet Control Message Protocol
    | ICMP
    /// Authentication Header (IPSec)
    | AH
    /// Encapsulating Security Payload (IPSec)
    | ESP
    module NetworkProtocol =
        let ArmValue = function
            | AnyProtocol -> "*"
            | TCP -> "Tcp"
            | UDP -> "Udp"
            | ICMP -> "Icmp"
            | AH -> "Ah"
            | ESP -> "Esp"
    type NetworkProtocol with
        member this.ArmValue = this |> NetworkProtocol.ArmValue

    type Port =
        | Port of uint16
        | Range of First:uint16 * Last:uint16
        | AnyPort
        member this.ArmValue =
            match this with
            | Port num -> num |> string
            | Range (first,last) -> sprintf "%d-%d" first last
            | AnyPort -> "*"
    module Port =
        let ArmValue (port:Port) = port.ArmValue

    type Endpoint =
        | Host of Net.IPAddress
        | Network of IPAddressCidr
        | Tag of string
        | AnyEndpoint
        member this.ArmValue =
            match this with
            | Host ip -> string ip
            | Network cidr -> cidr |> IPAddressCidr.format
            | Tag tag -> tag
            | AnyEndpoint -> "*"
    module Endpoint =
        let ArmValue (endpoint:Endpoint) = endpoint.ArmValue

    type NetworkService = NetworkService of name:string * Port

    type TrafficDirection = Inbound | Outbound
    module TrafficDirection =
        let ArmValue = function | Inbound -> "Inbound" | Outbound -> "Outbound"
    type TrafficDirection with
        member this.ArmValue = this |> TrafficDirection.ArmValue

module PublicIpAddress =
    type AllocationMethod =
        | Dynamic
        | Static
        member this.ArmValue =
            match this with
            | Dynamic -> "Dynamic"
            | Static -> "Static"
    type Sku =
        | Basic
        | Standard
        member this.ArmValue =
            match this with
            | Basic -> "Basic"
            | Standard -> "Standard"

module Cdn =
    type Sku =
    | Custom_Verizon
    | Premium_Verizon
    | Premium_ChinaCdn
    | Standard_Akamai
    | Standard_ChinaCdn
    | Standard_Microsoft
    | Standard_Verizon

    type QueryStringCachingBehaviour =
    | IgnoreQueryString
    | BypassCaching
    | UseQueryString
    | NotSet

    type OptimizationType =
    | GeneralWebDelivery
    | GeneralMediaStreaming
    | VideoOnDemandMediaStreaming
    | LargeFileDownload
    | DynamicSiteAcceleration

module EventGrid =
    type EventGridEvent = EventGridEvent of string member this.Value = match this with EventGridEvent s -> s

/// Built in Azure roles (https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)
module Roles =
    type RoleID = RoleID of string

    let AcrPush = RoleID "8311e382-0749-4cb8-b61a-304f252e45ec"
    let APIManagementServiceContributor = RoleID "312a565d-c81f-4fd8-895a-4e21e48d571c"
    let AcrPull = RoleID "7f951dda-4ed3-4680-a7ca-43fe172d538d"
    let AcrImageSigner = RoleID "6cef56e8-d556-48e5-a04f-b8e64114680f"
    let AcrDelete = RoleID "c2f4ef07-c644-48eb-af81-4b1b4947fb11"
    let AcrQuarantineReader = RoleID "cdda3590-29a3-44f6-95f2-9f980659eb04"
    let AcrQuarantineWriter = RoleID "c8d4ff99-41c3-41a8-9f60-21dfdad59608"
    let APIManagementServiceOperatorRole = RoleID "e022efe7-f5ba-4159-bbe4-b44f577e9b61"
    let APIManagementServiceReaderRole = RoleID "71522526-b88f-4d52-b57f-d31fc3546d0d"
    let ApplicationInsightsComponentContributor = RoleID "ae349356-3a1b-4a5e-921d-050484c6347e"
    let ApplicationInsightsSnapshotDebugger = RoleID "08954f03-6346-4c2e-81c0-ec3a5cfae23b"
    let AttestationReader = RoleID "fd1bd22b-8476-40bc-a0bc-69b95687b9f3"
    let AutomationJobOperator = RoleID "4fe576fe-1146-4730-92eb-48519fa6bf9f"
    let AutomationRunbookOperator = RoleID "5fb5aef8-1081-4b8e-bb16-9d5d0385bab5"
    let AutomationOperator = RoleID "d3881f73-407a-4167-8283-e981cbba0404"
    let AvereContributor = RoleID "4f8fab4f-1852-4a58-a46a-8eaf358af14a"
    let AvereOperator = RoleID "c025889f-8102-4ebf-b32c-fc0c6f0c6bd9"
    let AzureKubernetesServiceClusterAdminRole = RoleID "0ab0b1a8-8aac-4efd-b8c2-3ee1fb270be8"
    let AzureKubernetesServiceClusterUserRole = RoleID "4abbcc35-e782-43d8-92c5-2d3f1bd2253f"
    let AzureMapsDataReader = RoleID "423170ca-a8f6-4b0f-8487-9e4eb8f49bfa"
    let AzureStackRegistrationOwner = RoleID "6f12a6df-dd06-4f3e-bcb1-ce8be600526a"
    let BackupContributor = RoleID "5e467623-bb1f-42f4-a55d-6e525e11384b"
    let BillingReader = RoleID "fa23ad8b-c56e-40d8-ac0c-ce449e1d2c64"
    let BackupOperator = RoleID "00c29273-979b-4161-815c-10b084fb9324"
    let BackupReader = RoleID "a795c7a0-d4a2-40c1-ae25-d81f01202912"
    let BlockchainMemberNodeAccess = RoleID "31a002a1-acaf-453e-8a5b-297c9ca1ea24"
    let BizTalkContributor = RoleID "5e3c6656-6cfa-4708-81fe-0de47ac73342"
    let CDNEndpointContributor = RoleID "426e0c7f-0c7e-4658-b36f-ff54d6c29b45"
    let CDNEndpointReader = RoleID "871e35f6-b5c1-49cc-a043-bde969a0f2cd"
    let CDNProfileContributor = RoleID "ec156ff8-a8d1-4d15-830c-5b80698ca432"
    let CDNProfileReader = RoleID "8f96442b-4075-438f-813d-ad51ab4019af"
    let ClassicNetworkContributor = RoleID "b34d265f-36f7-4a0d-a4d4-e158ca92e90f"
    let ClassicStorageAccountContributor = RoleID "86e8f5dc-a6e9-4c67-9d15-de283e8eac25"
    let ClassicStorageAccountKeyOperatorServiceRole = RoleID "985d6b00-f706-48f5-a6fe-d0ca12fb668d"
    let ClearDBMySQLDBContributor = RoleID "9106cda0-8a86-4e81-b686-29a22c54effe"
    let ClassicVirtualMachineContributor = RoleID "d73bb868-a0df-4d4d-bd69-98a00b01fccb"
    let CognitiveServicesUser = RoleID "a97b65f3-24c7-4388-baec-2e87135dc908"
    let CognitiveServicesDataReader = RoleID "b59867f0-fa02-499b-be73-45a86b5b3e1c"
    let CognitiveServicesContributor = RoleID "25fbc0a9-bd7c-42a3-aa1a-3b75d497ee68"
    let CosmosBackupOperator = RoleID "db7b14f2-5adf-42da-9f96-f2ee17bab5cb"
    let Contributor = RoleID "b24988ac-6180-42a0-ab88-20f7382dd24c"
    let CosmosDBAccountReaderRole = RoleID "fbdf93bf-df7d-467e-a4d2-9458aa1360c8"
    let CostManagementContributor = RoleID "434105ed-43f6-45c7-a02f-909b2ba83430"
    let CostManagementReader = RoleID "72fafb9e-0641-4937-9268-a91bfd8191a3"
    let DataBoxContributor = RoleID "add466c9-e687-43fc-8d98-dfcf8d720be5"
    let DataBoxReader = RoleID "028f4ed7-e2a9-465e-a8f4-9c0ffdfdc027"
    let DataFactoryContributor = RoleID "673868aa-7521-48a0-acc6-0f60742d39f5"
    let DataPurger = RoleID "150f5e0c-0603-4f03-8c7f-cf70034c4e90"
    let DataLakeAnalyticsDeveloper = RoleID "47b7735b-770e-4598-a7da-8b91488b4c88"
    let DevTestLabsUser = RoleID "76283e04-6283-4c54-8f91-bcf1374a3c64"
    let DocumentDBAccountContributor = RoleID "5bd9cd88-fe45-4216-938b-f97437e15450"
    let DNSZoneContributor = RoleID "befefa01-2a29-4197-83a8-272ff33ce314"
    let EventGridEventSubscriptionContributor = RoleID "428e0ff0-5e57-4d9c-a221-2c70d0e0a443"
    let EventGridEventSubscriptionReader = RoleID "2414bbcf-6497-4faf-8c65-045460748405"
    let GraphOwner = RoleID "b60367af-1334-4454-b71e-769d9a4f83d9"
    let HDInsightDomainServicesContributor = RoleID "8d8d5a11-05d3-4bda-a417-a08778121c7c"
    let IntelligentSystemsAccountContributor = RoleID "03a6d094-3444-4b3d-88af-7477090a9e5e"
    let KeyVaultContributor = RoleID "f25e0fa2-a7c8-4377-a976-54943a77a395"
    let KnowledgeConsumer = RoleID "ee361c5d-f7b5-4119-b4b6-892157c8f64c"
    let LabCreator = RoleID "b97fb8bc-a8b2-4522-a38b-dd33c7e65ead"
    let LogAnalyticsReader = RoleID "73c42c96-874c-492b-b04d-ab87d138a893"
    let LogAnalyticsContributor = RoleID "92aaf0da-9dab-42b6-94a3-d43ce8d16293"
    let LogicAppOperator = RoleID "515c2055-d9d4-4321-b1b9-bd0c9a0f79fe"
    let LogicAppContributor = RoleID "87a39d53-fc1b-424a-814c-f7e04687dc9e"
    let ManagedApplicationOperatorRole = RoleID "c7393b34-138c-406f-901b-d8cf2b17e6ae"
    let ManagedApplicationsReader = RoleID "b9331d33-8a36-4f8c-b097-4f54124fdb44"
    let ManagedIdentityOperator = RoleID "f1a07417-d97a-45cb-824c-7a7467783830"
    let ManagedIdentityContributor = RoleID "e40ec5ca-96e0-45a2-b4ff-59039f2c2b59"
    let ManagementGroupContributor = RoleID "5d58bcaf-24a5-4b20-bdb6-eed9f69fbe4c"
    let ManagementGroupReader = RoleID "ac63b705-f282-497d-ac71-919bf39d939d"
    let MonitoringMetricsPublisher = RoleID "3913510d-42f4-4e42-8a64-420c390055eb"
    let MonitoringReader = RoleID "43d0d8ad-25c7-4714-9337-8ba259a9fe05"
    let NetworkContributor = RoleID "4d97b98b-1d4f-4787-a291-c67834d212e7"
    let MonitoringContributor = RoleID "749f88d5-cbae-40b8-bcfc-e573ddc772fa"
    let NewRelicAPMAccountContributor = RoleID "5d28c62d-5b37-4476-8438-e587778df237"
    let Owner = RoleID "8e3af657-a8ff-443c-a75c-2fe8c4bcb635"
    let Reader = RoleID "acdd72a7-3385-48ef-bd42-f606fba81ae7"
    let RedisCacheContributor = RoleID "e0f68234-74aa-48ed-b826-c38b57376e17"
    let ReaderAndDataAccess = RoleID "c12c1c16-33a1-487b-954d-41c89c60f349"
    let ResourcePolicyContributor = RoleID "36243c78-bf99-498c-9df9-86d9f8d28608"
    let SchedulerJobCollectionsContributor = RoleID "188a0f2f-5c9e-469b-ae67-2aa5ce574b94"
    let SearchServiceContributor = RoleID "7ca78c08-252a-4471-8644-bb5ff32d4ba0"
    let SecurityAdmin = RoleID "fb1c8493-542b-48eb-b624-b4c8fea62acd"
    let SecurityManager = RoleID "e3d13bf0-dd5a-482e-ba6b-9b8433878d10"
    let SecurityReader = RoleID "39bc4728-0917-49c7-9d2c-d95423bc2eb4"
    let SpatialAnchorsAccountContributor = RoleID "8bbe83f1-e2a6-4df7-8cb4-4e04d4e5c827"
    let SiteRecoveryContributor = RoleID "6670b86e-a3f7-4917-ac9b-5d6ab1be4567"
    let SiteRecoveryOperator = RoleID "494ae006-db33-4328-bf46-533a6560a3ca"
    let SpatialAnchorsAccountReader = RoleID "5d51204f-eb77-4b1c-b86a-2ec626c49413"
    let SiteRecoveryReader = RoleID "dbaa88c4-0c30-4179-9fb3-46319faa6149"
    let SpatialAnchorsAccountOwner = RoleID "70bbe301-9835-447d-afdd-19eb3167307c"
    let SQLManagedInstanceContributor = RoleID "4939a1f6-9ae0-4e48-a1e0-f2cbe897382d"
    let SQLDBContributor = RoleID "9b7fa17d-e63e-47b0-bb0a-15c516ac86ec"
    let SQLSecurityManager = RoleID "056cd41c-7e88-42e1-933e-88ba6a50c9c3"
    let StorageAccountContributor = RoleID "17d1049b-9a84-46fb-8f53-869881c3d3ab"
    let SQLServerContributor = RoleID "6d8ee4ec-f05a-4a1d-8b00-a9b17e38b437"
    let StorageAccountKeyOperatorServiceRole = RoleID "81a9662b-bebf-436f-a333-f67b29880f12"
    let StorageBlobDataContributor = RoleID "ba92f5b4-2d11-453d-a403-e96b0029c9fe"
    let StorageBlobDataOwner = RoleID "b7e6dc6d-f1e8-4753-8033-0f276bb0955b"
    let StorageBlobDataReader = RoleID "2a2b9908-6ea1-4ae2-8e65-a410df84e7d1"
    let StorageQueueDataContributor = RoleID "974c5e8b-45b9-4653-ba55-5f855dd0fb88"
    let StorageQueueDataMessageProcessor = RoleID "8a0f0c08-91a1-4084-bc3d-661d67233fed"
    let StorageQueueDataMessageSender = RoleID "c6a89b2d-59bc-44d0-9896-0f6e12d7b80a"
    let StorageQueueDataReader = RoleID "19e7f393-937e-4f77-808e-94535e297925"
    let SupportRequestContributor = RoleID "cfd33db0-3dd1-45e3-aa9d-cdbdf3b6f24e"
    let TrafficManagerContributor = RoleID "a4b10055-b0c7-44c2-b00f-c7b5b3550cf7"
    let VirtualMachineAdministratorLogin = RoleID "1c0163c0-47e6-4577-8991-ea5c82e286e4"
    let UserAccessAdministrator = RoleID "18d7d88d-d35e-4fb5-a5c3-7773c20a72d9"
    let VirtualMachineUserLogin = RoleID "fb879df8-f326-4884-b1cf-06f3ad86be52"
    let VirtualMachineContributor = RoleID "9980e02c-c2be-4d73-94e8-173b1dc7cf3c"
    let WebPlanContributor = RoleID "2cc479cb-7b4d-49a8-b449-8c00fd0f0a4b"
    let WebsiteContributor = RoleID "de139f84-1756-47ae-9be6-808fbbe84772"
    let AzureServiceBusDataOwner = RoleID "090c5cfd-751d-490a-894a-3ce6f1109419"
    let AzureEventHubsDataOwner = RoleID "f526a384-b230-433a-b45c-95f59c4a2dec"
    let AttestationContributor = RoleID "bbf86eb8-f7b4-4cce-96e4-18cddf81d86e"
    let HDInsightClusterOperator = RoleID "61ed4efc-fab3-44fd-b111-e24485cc132a"
    let CosmosDBOperator = RoleID "230815da-be43-4aae-9cb4-875f7bd000aa"
    let HybridServerResourceAdministrator = RoleID "48b40c6e-82e0-4eb3-90d5-19e40f49b624"
    let HybridServerOnboarding = RoleID "5d1e5ee4-7c68-4a71-ac8b-0739630a3dfb"
    let AzureEventHubsDataReceiver = RoleID "a638d3c7-ab3a-418d-83e6-5f17a39d4fde"
    let AzureEventHubsDataSender = RoleID "2b629674-e913-4c01-ae53-ef4638d8f975"
    let AzureServiceBusDataReceiver = RoleID "4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0"
    let AzureServiceBusDataSender = RoleID "69a216fc-b8fb-44d8-bc22-1f3c2cd27a39"
    let StorageFileDataSMBShareReader = RoleID "aba4ae5f-2193-4029-9191-0cb91df5e314"
    let StorageFileDataSMBShareContributor = RoleID "0c867c2a-1d8c-454a-a3db-ab2ea1bdc8bb"
    let PrivateDNSZoneContributor = RoleID "b12aa53e-6015-4669-85d0-8515ebb3ae7f"
    let StorageBlobDelegator = RoleID "db58b8e5-c6ad-4a2a-8342-4190687cbf4a"
    let DesktopVirtualizationUser = RoleID "1d18fff3-a72a-46b5-b4a9-0b38a3cd7e63"
    let StorageFileDataSMBShareElevatedContributor = RoleID "a7264617-510b-434b-a828-9731dc254ea7"
    let BlueprintContributor = RoleID "41077137-e803-4205-871c-5a86e6a753b4"
    let BlueprintOperator = RoleID "437d2ced-4a38-4302-8479-ed2bcb43d090"
    let AzureSentinelContributor = RoleID "ab8e14d6-4a74-4a29-9ba8-549422addade"
    let AzureSentinelResponder = RoleID "3e150937-b8fe-4cfb-8069-0eaf05ecd056"
    let AzureSentinelReader = RoleID "8d289c81-5878-46d4-8554-54e1e3d8b5cb"
    let WorkbookReader = RoleID "b279062a-9be3-42a0-92ae-8b3cf002ec4d"
    let WorkbookContributor = RoleID "e8ddcd69-c73f-4f9f-9844-4100522f16ad"
    let PolicyInsightsDataWriter = RoleID "66bb4e9e-b016-4a94-8249-4c0511c2be84"
    let SignalRAccessKeyReader = RoleID "04165923-9d83-45d5-8227-78b77b0a687e"
    let SignalRContributor = RoleID "8cf5e20a-e4b2-4e9d-b3a1-5ceb692c2761"
    let AzureConnectedMachineOnboarding = RoleID "b64e21ea-ac4e-4cdf-9dc9-5b892992bee7"
    let AzureConnectedMachineResourceAdministrator = RoleID "cd570a14-e51a-42ad-bac8-bafd67325302"
    let ManagedServicesRegistrationAssignmentDeleteRole = RoleID "91c1777a-f3dc-4fae-b103-61d183457e46"
    let AppConfigurationDataOwner = RoleID "5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b"
    let AppConfigurationDataReader = RoleID "516239f1-63e1-4d78-a4de-a74fb236a071"
    let KubernetesClusterAzureArcOnboarding = RoleID "34e09817-6cbe-4d01-b1a2-e0eac5743d41"
    let ExperimentationContributor = RoleID "7f646f1b-fa08-80eb-a22b-edd6ce5c915c"
    let CognitiveServicesQnAMakerReader = RoleID "466ccd10-b268-4a11-b098-b4849f024126"
    let CognitiveServicesQnAMakerEditor = RoleID "f4cc2bf9-21be-47a1-bdf1-5c5804381025"
    let ExperimentationAdministrator = RoleID "7f646f1b-fa08-80eb-a33b-edd6ce5c915c"
    let RemoteRenderingAdministrator = RoleID "3df8b902-2a6f-47c7-8cc5-360e9b272a7e"
    let RemoteRenderingClient = RoleID "d39065c4-c120-43c9-ab0a-63eed9795f0a"
    let ManagedApplicationContributorRole = RoleID "641177b8-a67a-45b9-a033-47bc880bb21e"
    let SecurityAssessmentContributor = RoleID "612c2aa1-cb24-443b-ac28-3ab7272de6f5"
    let TagContributor = RoleID "4a9ae827-6dc8-4573-8ac7-8239d42aa03f"
    let IntegrationServiceEnvironmentDeveloper = RoleID "c7aa55d3-1abb-444a-a5ca-5e51e485d6ec"
    let IntegrationServiceEnvironmentContributor = RoleID "a41e2c5b-bd99-4a07-88f4-9bf657a760b8"
    let MarketplaceAdmin = RoleID "dd920d6d-f481-47f1-b461-f338c46b2d9f"
    let AzureKubernetesServiceContributorRole = RoleID "ed7f3fbd-7b88-4dd4-9017-9adb7ce333f8"
    let AzureDigitalTwinsReader = RoleID "d57506d4-4c8d-48b1-8587-93c323f6a5a3"
    let AzureDigitalTwinsOwner = RoleID "bcd981a7-7f74-457b-83e1-cceb9e632ffe"
    let HierarchySettingsAdministrator = RoleID "350f8d15-c687-4448-8ae1-157740a3936d"
    let FHIRDataContributor = RoleID "5a1fc7df-4bf1-4951-a576-89034ee01acd"
    let FHIRDataExporter = RoleID "3db33094-8700-4567-8da5-1501d4e7e843"
    let FHIRDataReader = RoleID "4c8d0bbc-75d3-4935-991f-5f3c56d81508"
    let FHIRDataWriter = RoleID "3f88fce4-5892-4214-ae73-ba5294559913"
    let ExperimentationReader = RoleID "49632ef5-d9ac-41f4-b8e7-bbe587fa74a1"
    let ObjectUnderstandingAccountOwner = RoleID "4dd61c23-6743-42fe-a388-d8bdd41cb745"
    let AzureMapsDataContributor = RoleID "8f5e0ce6-4f7b-4dcf-bddf-e6f48634a204"
    let CognitiveServicesCustomVisionContributor = RoleID "c1ff6cc2-c111-46fe-8896-e0ef812ad9f3"
    let CognitiveServicesCustomVisionDeployment = RoleID "5c4089e1-6d96-4d2f-b296-c1bc7137275f"
    let CognitiveServicesCustomVisionLabeler = RoleID "88424f51-ebe7-446f-bc41-7fa16989e96c"
    let CognitiveServicesCustomVisionReader = RoleID "93586559-c37d-4a6b-ba08-b9f0940c2d73"
    let CognitiveServicesCustomVisionTrainer = RoleID "0a5ae4ab-0d65-4eeb-be61-29fc9b54394b"
    let KeyVaultAdministrator = RoleID "00482a5a-887f-4fb3-b363-3b7fe8e74483"
    let KeyVaultCryptoOfficer = RoleID "14b46e9e-c2b7-41b4-b07b-48a6ebf60603"
    let KeyVaultCryptoUser = RoleID "12338af0-0e69-4776-bea7-57ae8d297424"
    let KeyVaultSecretsOfficer = RoleID "b86a8fe4-44ce-4948-aee5-eccb2c155cd7"
    let KeyVaultSecretsUser = RoleID "4633458b-17de-408a-b874-0445c86b69e6"
    let KeyVaultCertificatesOfficer = RoleID "a4417e6f-fecd-4de8-b567-7b0420556985"
    let KeyVaultReader = RoleID "21090545-7ca7-4776-b22c-e363652d74d2"
    let KeyVaultCryptoServiceEncryption = RoleID "e147488a-f6f5-4113-8e2d-b22465e65bf6"
    let AzureArcKubernetesViewer = RoleID "63f0a09d-1495-4db4-a681-037d84835eb4"
    let AzureArcKubernetesWriter = RoleID "5b999177-9696-4545-85c7-50de3797e5a1"
    let AzureArcKubernetesClusterAdmin = RoleID "8393591c-06b9-48a2-a542-1bd6b377f6a2"
    let AzureArcKubernetesAdmin = RoleID "dffb1e0c-446f-4dde-a09f-99eb5cc68b96"
    let AzureKubernetesServiceRBACClusterAdmin = RoleID "b1ff04bb-8a4e-4dc4-8eb5-8693973ce19b"
    let AzureKubernetesServiceRBACAdmin = RoleID "3498e952-d568-435e-9b2c-8d77e338d7f7"
    let AzureKubernetesServiceRBACReader = RoleID "7f6c6a51-bcf8-42ba-9220-52d62157d7db"
    let AzureKubernetesServiceRBACWriter = RoleID "a7ffa36f-339b-4b5c-8bdf-e2c188b2c0eb"
    let ServicesHubOperator = RoleID "82200a5b-e217-47a5-b665-6d8765ee745b"
    let ObjectUnderstandingAccountReader = RoleID "d18777c0-1514-4662-8490-608db7d334b6"
    let AzureArcEnabledKubernetesClusterUserRole = RoleID "00493d72-78f6-4148-b6c5-d3ce8e4799dd"
    let SignalRAppServer = RoleID "420fcaa2-552c-430f-98ca-3264be4806c7"
    let SignalRServerlessContributor = RoleID "fd53cd77-2268-407a-8f46-7e7863d0f521"
    let CollaborativeDataContributor = RoleID "daa9e50b-21df-454c-94a6-a8050adab352"
    let DeviceUpdateReader = RoleID "e9dba6fb-3d52-4cf0-bce3-f06ce71b9e0f"
    let DeviceUpdateAdministrator = RoleID "02ca0879-e8e4-47a5-a61e-5c618b76e64a"
    let DeviceUpdateContentAdministrator = RoleID "0378884a-3af5-44ab-8323-f5b22f9f3c98"
    let DeviceUpdateDeploymentsAdministrator = RoleID "e4237640-0e3d-4a46-8fda-70bc94856432"
    let DeviceUpdateDeploymentsReader = RoleID "49e2f5d2-7741-4835-8efa-19e1fe35e47f"
    let DeviceUpdateContentReader = RoleID "d1ee9a80-8b14-47f0-bdc2-f4a351625a7b"
    let CognitiveServicesMetricsAdvisorAdministrator = RoleID "cb43c632-a144-4ec5-977c-e80c4affc34a"
    let CognitiveServicesMetricsAdvisorUser = RoleID "3b20f47b-3825-43cb-8114-4bd2201156a8"
    let SchemaRegistryReader = RoleID "2c56ea50-c6b3-40a6-83c0-9d98858bc7d2"
    let SchemaRegistryContributor = RoleID "5dffeca3-4936-4216-b2bc-10343a5abb25"
    let AgFoodPlatformServiceReader = RoleID "7ec7ccdc-f61e-41fe-9aaf-980df0a44eba"
    let AgFoodPlatformServiceContributor = RoleID "8508508a-4469-4e45-963b-2518ee0bb728"
    let AgFoodPlatformServiceAdmin = RoleID "f8da80de-1ff9-4747-ad80-a19b7f6079e3"
    let ManagedHSMcontributor = RoleID "18500a29-7fe2-46b2-a342-b16a415e101d"
    let SignalRServiceReader = RoleID "ddde6b66-c0df-4114-a159-3618637b3035"
    let SignalRServiceOwner = RoleID "7e4f1700-ea5a-4f59-8f37-079cfe29dce3"

module Dns =
    type DnsZoneType = Public | Private
    type DnsRecordType =
        | A of TargetResource : ResourceName option * ARecords : string list
        | AAAA of TargetResource : ResourceName option * AaaaRecords : string list
        | CName of TargetResource : ResourceName option * CNameRecord : string option
        | NS of NsRecords : string list
        | PTR of PtrRecords : string list
        | TXT of TxtRecords : string list
        | MX of {| Preference : int; Exchange : string |} list