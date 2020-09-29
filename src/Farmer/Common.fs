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

module ContainerGroup =
    type PortAccess = PublicPort | InternalPort
    type RestartPolicy = NeverRestart | AlwaysRestart | RestartOnFailure
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
    module General =
        let Contributor = RoleID "b24988ac-6180-42a0-ab88-20f7382dd24c"
        let Owner = RoleID "8e3af657-a8ff-443c-a75c-2fe8c4bcb635"
        let Reader = RoleID "acdd72a7-3385-48ef-bd42-f606fba81ae7"
        let UserAccessAdministrator = RoleID "18d7d88d-d35e-4fb5-a5c3-7773c20a72d9"
    module Networking =
        let DnsZoneContributor = RoleID "befefa01-2a29-4197-83a8-272ff33ce314"
        let NetworkContributor = RoleID "4d97b98b-1d4f-4787-a291-c67834d212e7"
        let PrivateDnsZoneContributor = RoleID "b12aa53e-6015-4669-85d0-8515ebb3ae7f"
        let TrafficManagerContributor = RoleID "a4b10055-b0c7-44c2-b00f-c7b5b3550cf7"

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