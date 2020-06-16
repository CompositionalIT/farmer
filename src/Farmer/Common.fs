namespace Farmer

open System

type Location =
    | Location of string
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
    member this.ArmValue = match this with Location location -> location.ToLower()

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
          Sku : VmImageSku }
    let makeVm offer publisher sku = { Offer = Offer offer; Publisher = Publisher publisher; Sku = ImageSku sku }
    let makeWindowsVm = makeVm "WindowsServer" "MicrosoftWindowsServer"

    let CentOS_75 = makeVm "CentOS" "OpenLogic" "7.5"
    let CoreOS_Stable = makeVm "CoreOS" "CoreOS" "Stable"
    let debian_10 = makeVm "debian-10" "Debian" "10"
    let openSUSE_423 = makeVm "openSUSE-Leap" "SUSE" "42.3"
    let RHEL_7RAW = makeVm "RHEL" "RedHat" "7-RAW"
    let SLES_15 = makeVm "SLES" "SUSE" "15"
    let UbuntuServer_1804LTS = makeVm "UbuntuServer" "Canonical" "18.04-LTS"
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

module Storage =
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

module WebApp =
    type WorkerSize = Small | Medium | Large | Serverless
    type Cors = AllOrigins | SpecificOrigins of Uri list
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

module CognitiveServices =
    /// Type of SKU. See https://github.com/Azure/azure-quickstart-templates/tree/master/101-cognitive-services-translate
    type Sku =
        /// Free Tier
        | F0
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
    [<Measure>]
    type DTU
    type DbSku =
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
        static member S12 =Standard "S12"
        static member P1 = Premium "P1"
        static member P2 = Premium "P2"
        static member P4 = Premium "P4"
        static member P6 = Premium "P6"
        static member P11 = Premium "P11"
        static member P15 = Premium "P15"
        member this.Edition =
            match this with
            | Basic -> "Basic"
            | Free -> "Free"
            | Standard _ -> "Standard"
            | Premium _ -> "Premium"
         member this.Name =
            match this with
            | Basic -> "Basic"
            | Free -> "Free"
            | Standard s -> s
            | Premium p -> p
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
    type RestartPolicy = Never | Always | OnFailure
    type IpAddressType = PublicAddress | PrivateAddress

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

[<AutoOpen>]
module internal DuHelpers =
    let makeAll<'TUnion> =
        Reflection.FSharpType.GetUnionCases(typeof<'TUnion>)
        |> Array.map(fun t -> Reflection.FSharpValue.MakeUnion(t, null) :?> 'TUnion)
        |> Array.toList

module KeyVault =
    type Bypass = AzureServices | NoTraffic
    type SoftDeletionMode = SoftDeleteWithPurgeProtection | SoftDeletionOnly
    type DefaultAction = Allow | Deny
    type Key = Encrypt | Decrypt | WrapKey | UnwrapKey | Sign | Verify | Get | List | Create | Update | Import | Delete | Backup | Restore | Recover | Purge static member All = makeAll<Key>
    type Secret = Get | List | Set | Delete | Backup | Restore | Recover | Purge static member All = makeAll<Secret>
    type Certificate = Get | List | Delete | Create | Import | Update | ManageContacts | GetIssuers | ListIssuers | SetIssuers | DeleteIssuers | ManageIssuers | Recover | Purge | Backup | Restore static member All = makeAll<Certificate>
    type Storage = Get | List | Delete | Set | Update | RegenerateKey | Recover | Purge | Backup | Restore | SetSas | ListSas | GetSas | DeleteSas static member All = makeAll<Storage>

    [<RequireQualifiedAccess>]
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

module ServiceBus =
    type MessagingUnits = OneUnit | TwoUnits | FourUnits
    type Sku =
        | Basic
        | Standard
        | Premium of MessagingUnits

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
