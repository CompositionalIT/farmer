namespace Farmer

open System

type NonEmptyList<'T> =
    private
    | NonEmptyList of List<'T>

    /// Unwraps the inner List contents.
    member this.Value =
        match this with
        | NonEmptyList list -> list

module NonEmptyList =
    let create list =
        match list with
        | [] -> raiseFarmer "This list must always have at least one item in it."
        | list -> NonEmptyList list

[<AutoOpen>]
module internal DuHelpers =
    let makeAll<'TUnion> =
        Reflection.FSharpType.GetUnionCases(typeof<'TUnion>)
        |> Array.map (fun t -> Reflection.FSharpValue.MakeUnion(t, null) :?> 'TUnion)
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

[<AutoOpen>]
module DataLocationExtensions =
    type DataLocation with

        static member AsiaPacific = DataLocation "Asia Pacific"
        static member Australia = DataLocation "Australia"
        static member Europe = DataLocation "Europe"
        static member UnitedKingdom = DataLocation "United Kingdom"
        static member UnitedStates = DataLocation "United States"


type OS =
    | Windows
    | Linux

[<Measure>]
type Gb

[<Measure>]
type Mb

[<Measure>]
type Kb

[<Measure>]
type Mbps

[<Measure>]
type Seconds

[<Measure>]
type Minutes

[<Measure>]
type Hours

[<Measure>]
type Days

[<Measure>]
type VCores

type IsoDateTime =
    | IsoDateTime of string

    static member OfTimeSpan(d: TimeSpan) =
        d |> System.Xml.XmlConvert.ToString |> IsoDateTime

    member this.Value =
        match this with
        | IsoDateTime value -> value

type TransmissionProtocol =
    | TCP
    | UDP

type TlsVersion =
    | Tls10
    | Tls11
    | Tls12

/// Represents an environment variable that can be set, typically on Docker container services.
type EnvVar =
    /// Use for non-secret environment variables. These will be stored in cleartext in the ARM template.
    | EnvValue of string
    /// Use for secret environment variables. These will be provided as secure parameters to the ARM template.
    | SecureEnvValue of SecureParameter
    /// Use for secret environment variables that get their value from an ARM Expression. These will be an ARM expression in the template, but value used in a secure context.
    | SecureEnvExpression of ArmExpression

    static member create (name: string) (value: string) = name, EnvValue value

    static member createSecure (name: string) (paramName: string) =
        name, SecureEnvValue(SecureParameter paramName)

    static member createSecureExpression (name: string) (armExpression: ArmExpression) =
        name, SecureEnvExpression armExpression

module Mb =
    let toBytes (mb: int<Mb>) = int64 mb * 1024L * 1024L

module Route =
    type HopType =
        | VirtualAppliance of System.Net.IPAddress option
        | Internet
        | Nothing
        | VirtualNetworkGateway
        | VnetLocal

        member x.ArmValue =
            match x with
            | VirtualAppliance _ -> "VirtualAppliance"
            | Internet -> "Internet"
            | Nothing -> "None"
            | VirtualNetworkGateway -> "VirtualNetworkGateway"
            | VnetLocal -> "VnetLocal"

module Vm =
    type VMSize =
        | Basic_A0
        | Basic_A1
        | Basic_A2
        | Basic_A3
        | Basic_A4
        | Premium_P
        | Premium_P1
        | Premium_P10
        | Premium_P15
        | Premium_P2
        | Premium_P20
        | Premium_P3
        | Premium_P30
        | Premium_P4
        | Premium_P40
        | Premium_P50
        | Premium_P6
        | Premium_P60
        | Premium_P70
        | Premium_P80
        | Standard_A0
        | Standard_A1
        | Standard_A1_v2
        | Standard_A10
        | Standard_A11
        | Standard_A2
        | Standard_A2_v2
        | Standard_A2m_v2
        | Standard_A3
        | Standard_A4
        | Standard_A4_v2
        | Standard_A4m_v2
        | Standard_A5
        | Standard_A6
        | Standard_A7
        | Standard_A8
        | Standard_A8_v2
        | Standard_A8m_v2
        | Standard_A9
        | Standard_B12ms
        | Standard_B16ms
        | Standard_B1ls
        | Standard_B1ms
        | Standard_B1s
        | Standard_B20ms
        | Standard_B2ms
        | Standard_B2s
        | Standard_B4ms
        | Standard_B8ms
        | Standard_D1
        | Standard_D1_v2
        | Standard_D11
        | Standard_D11_v2
        | Standard_D12
        | Standard_D12_v2
        | Standard_D13
        | Standard_D13_v2
        | Standard_D14
        | Standard_D14_v2
        | Standard_D15_v2
        | Standard_D16_v3
        | Standard_D16_v4
        | Standard_D16_v5
        | Standard_D16a_v4
        | Standard_D16ads_v5
        | Standard_D16as_v4
        | Standard_D16as_v5
        | Standard_D16d_v4
        | Standard_D16d_v5
        | Standard_D16ds_v4
        | Standard_D16ds_v5
        | Standard_D16s_v3
        | Standard_D16s_v4
        | Standard_D16s_v5
        | Standard_D2
        | Standard_D2_v2
        | Standard_D2_v3
        | Standard_D2_v4
        | Standard_D2_v5
        | Standard_D2a_v4
        | Standard_D2ads_v5
        | Standard_D2as_v4
        | Standard_D2as_v5
        | Standard_D2d_v4
        | Standard_D2d_v5
        | Standard_D2ds_v4
        | Standard_D2ds_v5
        | Standard_D2s_v3
        | Standard_D2s_v4
        | Standard_D2s_v5
        | Standard_D3
        | Standard_D3_v2
        | Standard_D32_v3
        | Standard_D32_v4
        | Standard_D32_v5
        | Standard_D32a_v4
        | Standard_D32ads_v5
        | Standard_D32as_v4
        | Standard_D32as_v5
        | Standard_D32d_v4
        | Standard_D32d_v5
        | Standard_D32ds_v4
        | Standard_D32ds_v5
        | Standard_D32s_v3
        | Standard_D32s_v4
        | Standard_D32s_v5
        | Standard_D4
        | Standard_D4_v2
        | Standard_D4_v3
        | Standard_D4_v4
        | Standard_D4_v5
        | Standard_D48_v3
        | Standard_D48_v4
        | Standard_D48_v5
        | Standard_D48a_v4
        | Standard_D48ads_v5
        | Standard_D48as_v4
        | Standard_D48as_v5
        | Standard_D48d_v4
        | Standard_D48d_v5
        | Standard_D48ds_v4
        | Standard_D48ds_v5
        | Standard_D48s_v3
        | Standard_D48s_v4
        | Standard_D48s_v5
        | Standard_D4a_v4
        | Standard_D4ads_v5
        | Standard_D4as_v4
        | Standard_D4as_v5
        | Standard_D4d_v4
        | Standard_D4d_v5
        | Standard_D4ds_v4
        | Standard_D4ds_v5
        | Standard_D4s_v3
        | Standard_D4s_v4
        | Standard_D4s_v5
        | Standard_D5_v2
        | Standard_D64_v3
        | Standard_D64_v4
        | Standard_D64_v5
        | Standard_D64a_v4
        | Standard_D64ads_v5
        | Standard_D64as_v4
        | Standard_D64as_v5
        | Standard_D64d_v4
        | Standard_D64d_v5
        | Standard_D64ds_v4
        | Standard_D64ds_v5
        | Standard_D64s_v3
        | Standard_D64s_v4
        | Standard_D64s_v5
        | Standard_D8_v3
        | Standard_D8_v4
        | Standard_D8_v5
        | Standard_D8a_v4
        | Standard_D8ads_v5
        | Standard_D8as_v4
        | Standard_D8as_v5
        | Standard_D8d_v4
        | Standard_D8d_v5
        | Standard_D8ds_v4
        | Standard_D8ds_v5
        | Standard_D8s_v3
        | Standard_D8s_v4
        | Standard_D8s_v5
        | Standard_D96_v5
        | Standard_D96a_v4
        | Standard_D96ads_v5
        | Standard_D96as_v4
        | Standard_D96as_v5
        | Standard_D96d_v5
        | Standard_D96ds_v5
        | Standard_D96s_v5
        | Standard_DC16ads_v5
        | Standard_DC16as_v5
        | Standard_DC1s_v2
        | Standard_DC2ads_v5
        | Standard_DC2as_v5
        | Standard_DC2s
        | Standard_DC2s_v2
        | Standard_DC32ads_v5
        | Standard_DC32as_v5
        | Standard_DC48ads_v5
        | Standard_DC48as_v5
        | Standard_DC4ads_v5
        | Standard_DC4as_v5
        | Standard_DC4s
        | Standard_DC4s_v2
        | Standard_DC64ads_v5
        | Standard_DC64as_v5
        | Standard_DC8_v2
        | Standard_DC8ads_v5
        | Standard_DC8as_v5
        | Standard_DC96ads_v5
        | Standard_DC96as_v5
        | Standard_DS1
        | Standard_DS1_v2
        | Standard_DS11
        | Standard_DS11_v2
        | Standard_DS12
        | Standard_DS12_v2
        | Standard_DS13
        | Standard_DS13_2_v2
        | Standard_DS13_4_v2
        | Standard_DS13_v2
        | Standard_DS14
        | Standard_DS14_4_v2
        | Standard_DS14_8_v2
        | Standard_DS14_v2
        | Standard_DS15_v2
        | Standard_DS2
        | Standard_DS2_v2
        | Standard_DS3
        | Standard_DS3_v2
        | Standard_DS4
        | Standard_DS4_v2
        | Standard_DS5_v2
        | Standard_E1
        | Standard_E10
        | Standard_E104i_v5
        | Standard_E104id_v5
        | Standard_E104ids_v5
        | Standard_E104is_v5
        | Standard_E112iads_v5
        | Standard_E112ias_v5
        | Standard_E15
        | Standard_E16_v3
        | Standard_E16_v4
        | Standard_E16_v5
        | Standard_E16a_v4
        | Standard_E16ads_v5
        | Standard_E16as_v4
        | Standard_E16as_v5
        | Standard_E16bds_v5
        | Standard_E16bs_v5
        | Standard_E16d_v4
        | Standard_E16d_v5
        | Standard_E16ds_v4
        | Standard_E16ds_v5
        | Standard_E16s_v3
        | Standard_E16s_v4
        | Standard_E16s_v5
        | Standard_E2
        | Standard_E2_v3
        | Standard_E2_v3_v3
        | Standard_E2_v4
        | Standard_E2_v5
        | Standard_E20
        | Standard_E20_v3
        | Standard_E20_v4
        | Standard_E20_v5
        | Standard_E20a_v4
        | Standard_E20ads_v5
        | Standard_E20as_v4
        | Standard_E20as_v5
        | Standard_E20d_v4
        | Standard_E20d_v5
        | Standard_E20ds_v4
        | Standard_E20ds_v5
        | Standard_E20s_v3
        | Standard_E20s_v4
        | Standard_E20s_v5
        | Standard_E2a_v4
        | Standard_E2ads_v5
        | Standard_E2as_v4
        | Standard_E2as_v5
        | Standard_E2bds_v5
        | Standard_E2bs_v5
        | Standard_E2d_v4
        | Standard_E2d_v5
        | Standard_E2ds_v4
        | Standard_E2ds_v5
        | Standard_E2s_v3
        | Standard_E2s_v4
        | Standard_E2s_v5
        | Standard_E3
        | Standard_E30
        | Standard_E32_16_v3
        | Standard_E32_8s_v3
        | Standard_E32_v3
        | Standard_E32_v4
        | Standard_E32_v5
        | Standard_E32a_v4
        | Standard_E32ads_v5
        | Standard_E32as_v4
        | Standard_E32as_v5
        | Standard_E32bds_v5
        | Standard_E32bs_v5
        | Standard_E32d_v4
        | Standard_E32d_v5
        | Standard_E32ds_v4
        | Standard_E32ds_v5
        | Standard_E32s_v3
        | Standard_E32s_v4
        | Standard_E32s_v5
        | Standard_E4
        | Standard_E4_v3
        | Standard_E4_v4
        | Standard_E4_v5
        | Standard_E40
        | Standard_E48_v3
        | Standard_E48_v4
        | Standard_E48_v5
        | Standard_E48a_v4
        | Standard_E48ads_v5
        | Standard_E48as_v4
        | Standard_E48as_v5
        | Standard_E48bds_v5
        | Standard_E48bs_v5
        | Standard_E48d_v4
        | Standard_E48d_v5
        | Standard_E48ds_v4
        | Standard_E48ds_v5
        | Standard_E48s_v3
        | Standard_E48s_v4
        | Standard_E48s_v5
        | Standard_E4a_v4
        | Standard_E4ads_v5
        | Standard_E4as_v4
        | Standard_E4as_v5
        | Standard_E4bds_v5
        | Standard_E4bs_v5
        | Standard_E4d_v4
        | Standard_E4d_v5
        | Standard_E4ds_v4
        | Standard_E4ds_v5
        | Standard_E4s_v3
        | Standard_E4s_v4
        | Standard_E4s_v5
        | Standard_E50
        | Standard_E6
        | Standard_E60
        | Standard_E64_16s_v3
        | Standard_E64_32s_v3
        | Standard_E64_v3
        | Standard_E64_v4
        | Standard_E64_v5
        | Standard_E64a_v4
        | Standard_E64ads_v5
        | Standard_E64as_v4
        | Standard_E64as_v5
        | Standard_E64bds_v5
        | Standard_E64bs_v5
        | Standard_E64d_v4
        | Standard_E64d_v5
        | Standard_E64ds_v4
        | Standard_E64ds_v5
        | Standard_E64i_v3
        | Standard_E64is_v3
        | Standard_E64s_v3
        | Standard_E64s_v4
        | Standard_E64s_v5
        | Standard_E70
        | Standard_E8_v3
        | Standard_E8_v4
        | Standard_E8_v5
        | Standard_E80
        | Standard_E80ids_v4
        | Standard_E80is_v4
        | Standard_E8a_v4
        | Standard_E8ads_v5
        | Standard_E8as_v4
        | Standard_E8as_v5
        | Standard_E8bds_v5
        | Standard_E8bs_v5
        | Standard_E8d_v4
        | Standard_E8d_v5
        | Standard_E8ds_v4
        | Standard_E8ds_v5
        | Standard_E8s_v3
        | Standard_E8s_v4
        | Standard_E8s_v5
        | Standard_E96_v5
        | Standard_E96a_v4
        | Standard_E96ads_v5
        | Standard_E96as_v4
        | Standard_E96as_v5
        | Standard_E96d_v5
        | Standard_E96ds_v5
        | Standard_E96ias_v4
        | Standard_E96s_v5
        | Standard_EC16ads_v5
        | Standard_EC16as_v5
        | Standard_EC20ads_v5
        | Standard_EC20as_v5
        | Standard_EC2ads_v5
        | Standard_EC2as_v5
        | Standard_EC32ads_v5
        | Standard_EC32as_v5
        | Standard_EC48ads_v5
        | Standard_EC48as_v5
        | Standard_EC4ads_v5
        | Standard_EC4as_v5
        | Standard_EC64ads_v5
        | Standard_EC64as_v5
        | Standard_EC8ads_v5
        | Standard_EC8as_v5
        | Standard_EC96ads_v5
        | Standard_EC96as_v5
        | Standard_EC96iads_v5
        | Standard_EC96ias_v5
        | Standard_F1
        | Standard_F16
        | Standard_F16s
        | Standard_F16s_v2
        | Standard_F1s
        | Standard_F2
        | Standard_F2s
        | Standard_F2s_v2
        | Standard_F32s_v2
        | Standard_F4
        | Standard_F48s_v2
        | Standard_F4s
        | Standard_F4s_v2
        | Standard_F64s_v2
        | Standard_F72s_v2
        | Standard_F8
        | Standard_F8s
        | Standard_F8s_v2
        | Standard_FX12mds
        | Standard_FX24mds
        | Standard_FX36mds
        | Standard_FX48mds
        | Standard_FX4mds
        | Standard_G1
        | Standard_G2
        | Standard_G3
        | Standard_G4
        | Standard_G5
        | Standard_GS1
        | Standard_GS2
        | Standard_GS3
        | Standard_GS4
        | Standard_GS4_4
        | Standard_GS4_8
        | Standard_GS5
        | Standard_GS5_16
        | Standard_GS5_8
        | Standard_H16
        | Standard_H16_Promo
        | Standard_H16m
        | Standard_H16m_Promo
        | Standard_H16mr
        | Standard_H16mr_Promo
        | Standard_H16r
        | Standard_H16r_Promo
        | Standard_H8
        | Standard_H8_Promo
        | Standard_H8m
        | Standard_H8m_Promo
        | Standard_HB120rs_v2
        | Standard_HB120rs_v3
        | Standard_L16as_v3
        | Standard_L16s
        | Standard_L16s_v2
        | Standard_L16s_v3
        | Standard_L32as_v3
        | Standard_L32s
        | Standard_L32s_v2
        | Standard_L32s_v3
        | Standard_L48as_v3
        | Standard_L48s_v2
        | Standard_L48s_v3
        | Standard_L4s
        | Standard_L64as_v3
        | Standard_L64s_v2
        | Standard_L64s_v3
        | Standard_L80as_v3
        | Standard_L80s_v2
        | Standard_L80s_v3
        | Standard_L8as_v3
        | Standard_L8s
        | Standard_L8s_v2
        | Standard_L8s_v3
        | Standard_M128
        | Standard_M128_32ms
        | Standard_M128_64ms
        | Standard_M128dms_v2
        | Standard_M128ds_v2
        | Standard_M128m
        | Standard_M128ms
        | Standard_M128ms_v2
        | Standard_M128s
        | Standard_M128s_v2
        | Standard_M16ms
        | Standard_M192idms_v2
        | Standard_M192ids_v2
        | Standard_M192ims_v2
        | Standard_M192is_v2
        | Standard_M208ms_v2
        | Standard_M208s_v2
        | Standard_M32dms_v2
        | Standard_M32ls
        | Standard_M32ms
        | Standard_M32ms_v2
        | Standard_M32ts
        | Standard_M416ms_v2
        | Standard_M416s_v2
        | Standard_M64
        | Standard_M64_16ms
        | Standard_M64_32ms
        | Standard_M64dms_v2
        | Standard_M64ds_v2
        | Standard_M64ls
        | Standard_M64m
        | Standard_M64ms
        | Standard_M64ms_v2
        | Standard_M64s
        | Standard_M64s_v2
        | Standard_M8ms
        | Standard_NC12
        | Standard_NC12s_v2
        | Standard_NC12s_v3
        | Standard_NC16as_T4_v3
        | Standard_NC24
        | Standard_NC24ads_A100_v4
        | Standard_NC24r
        | Standard_NC24rs_v2
        | Standard_NC24rs_v3
        | Standard_NC24s_v2
        | Standard_NC24s_v3
        | Standard_NC48ads_A100_v4
        | Standard_NC4as_T4_v3
        | Standard_NC6
        | Standard_NC64as_T4_v3
        | Standard_NC6s_v2
        | Standard_NC6s_v3
        | Standard_NC8as_T4_v3
        | Standard_NC96ads_A100_v4
        | Standard_ND12s
        | Standard_ND24rs
        | Standard_ND24s
        | Standard_ND40rs_v2
        | Standard_ND6s
        | Standard_ND96asr_v4
        | Standard_NV12
        | Standard_NV16as_v4
        | Standard_NV24
        | Standard_NV32as_v4
        | Standard_NV4as_v4
        | Standard_NV6
        | Standard_NV8as_v4
        | Standard_S10
        | Standard_S15
        | Standard_S20
        | Standard_S30
        | Standard_S4
        | Standard_S40
        | Standard_S50
        | Standard_S6
        | Standard_S60
        | Standard_S70
        | Standard_S80
        | CustomImage of string

        member this.ArmValue =
            match this with
            | CustomImage c -> c
            | _ -> this.ToString()

    type Offer =
        | Offer of string

        member this.ArmValue =
            match this with
            | Offer o -> o

    type Publisher =
        | Publisher of string

        member this.ArmValue =
            match this with
            | Publisher p -> p

    type VmImageSku =
        | ImageSku of string

        member this.ArmValue =
            match this with
            | ImageSku i -> i

    type ImageDefinition =
        {
            Offer: Offer
            Publisher: Publisher
            Sku: VmImageSku
            OS: OS
        }

    let makeVm os offer publisher sku =
        {
            Offer = Offer offer
            Publisher = Publisher publisher
            OS = os
            Sku = ImageSku sku
        }

    let makeWindowsVm = makeVm Windows "WindowsServer" "MicrosoftWindowsServer"
    let makeLinuxVm = makeVm Linux

    let CentOS_75 = makeLinuxVm "CentOS" "OpenLogic" "7.5"
    let CoreOS_Stable = makeLinuxVm "CoreOS" "CoreOS" "Stable"
    let debian_10 = makeLinuxVm "debian-10" "Debian" "10"
    let openSUSE_423 = makeLinuxVm "openSUSE-Leap" "SUSE" "42.3"
    let RHEL_7RAW = makeLinuxVm "RHEL" "RedHat" "7-RAW"
    let SLES_15 = makeLinuxVm "SLES" "SUSE" "15"
    let UbuntuServer_1804LTS = makeLinuxVm "UbuntuServer" "Canonical" "18.04-LTS"

    let UbuntuServer_2004LTS =
        makeLinuxVm "0001-com-ubuntu-server-focal" "canonical" "20_04-lts-gen2"

    let WindowsServer_2019Datacenter = makeWindowsVm "2019-Datacenter"
    let WindowsServer_2016Datacenter = makeWindowsVm "2016-Datacenter"
    let WindowsServer_2012R2Datacenter = makeWindowsVm "2012-R2-Datacenter"
    let WindowsServer_2012Datacenter = makeWindowsVm "2012-Datacenter"
    let WindowsServer_2008R2SP1 = makeWindowsVm "2008-R2-SP1"
    let Windows10Pro = makeVm Windows "Windows-10" "MicrosoftWindowsDesktop" "20h2-pro"

    /// The type of disk to use.
    type DiskType =
        | StandardSSD_LRS
        | Standard_LRS
        | Premium_LRS

        member this.ArmValue =
            match this with
            | x -> x.ToString()

    /// Represents a disk in a VM.
    type DiskInfo = { Size: int; DiskType: DiskType }

    type EvictionPolicy =
        | Deallocate
        | Delete

        member this.ArmValue =
            match this with
            | Deallocate -> "Deallocate"
            | Delete -> "Delete"

    type BillingProfile = { MaxPrice: decimal }

    type Priority =
        | Low
        | Regular
        | Spot of evictionPolicy: EvictionPolicy * maxPrice: decimal

        member this.ArmValue =
            match this with
            | Low -> "Low"
            | Regular -> "Regular"
            | Spot _ -> "Spot"

module internal Validation =
    // ANDs two validation rules
    let (<+>) a b v = a v && b v
    /// ORs two validation rules
    let (<|>) a b v = a v || b v

    /// Combines two validation rules. Both OK -> OK, otherwise Error.
    let (<!>) a b e s =
        match a e s, b e s with
        | Ok _, Ok _ -> Ok()
        | Error x, _
        | _, Error x -> Error x

    let isNonEmpty entity s =
        if String.IsNullOrWhiteSpace s then
            Error $"%s{entity} cannot be empty"
        else
            Ok()

    let isNotAGuid entity (s: string) =
        match Guid.TryParse s with
        | true, _ -> Error $"%s{entity} cannot be a GUID"
        | false, _ -> Ok()

    let notLongerThan max entity (s: string) =
        if s.Length > max then
            Error $"%s{entity} max length is %d{max}, but here is {s.Length}"
        else
            Ok()

    let notShorterThan min entity (s: string) =
        if s.Length < min then
            Error $"%s{entity} min length is %d{min}, but here is {s.Length}"
        else
            Ok()

    let lengthBetween min max entity (s: string) =
        s
        |> notLongerThan max entity
        |> Result.bind (fun _ -> s |> notShorterThan min entity)

    let containsOnly (message, predicate) entity (s: string) =
        if s |> Seq.exists (predicate >> not) then
            Error $"%s{entity} can only contain %s{message}"
        else
            Ok()

    let cannotContain (message, predicate) entity (s: string) =
        if s |> Seq.exists predicate then
            Error $"%s{entity} do not allow %s{message}"
        else
            Ok()

    let startsWith (message, predicate) entity (s: string) =
        if not (predicate s.[0]) then
            Error $"%s{entity} must start with %s{message}"
        else
            Ok()

    let endsWith (message, predicate) entity (s: string) =
        if not (predicate s.[s.Length - 1]) then
            Error $"%s{entity} must end with %s{message}"
        else
            Ok()

    let cannotStartWith (message, predicate) entity (s: string) =
        if predicate s.[0] then
            Error $"%s{entity} cannot start with %s{message}"
        else
            Ok()

    let cannotEndWith (message, predicate) entity (s: string) =
        if predicate s.[s.Length - 1] then
            Error $"%s{entity} cannot end with %s{message}"
        else
            Ok()

    let cannotEndsWith (predicate: (string * string) seq) entity (s: string) =
        let matches =
            predicate
            |> Seq.filter (fun (_, postfix) -> s.EndsWith(postfix, StringComparison.Ordinal))
            |> Seq.map fst
            |> Seq.toList

        match matches with
        | [] -> Ok()
        | predicatesThatFailes ->
            let message = System.String.Join(", ", predicatesThatFailes)
            Error $"%s{entity} cannot end with %s{message}"

    let arb (message, predicate) entity s =
        if predicate s then
            Error $"%s{entity} %s{message}"
        else
            Ok()

    let containsOnlyM containers =
        containers |> List.map containsOnly |> List.reduce (<!>)

    let nonEmptyLengthBetween a b = isNonEmpty <!> lengthBetween a b

    let lowercaseLetters = "lowercase letters", Char.IsLetter >> not <|> Char.IsLower
    let aLetterOrNumber = "an alphanumeric character", Char.IsLetterOrDigit
    let lettersOrNumbers = "alphanumeric characters", Char.IsLetterOrDigit
    let letters = "letters", Char.IsLetter
    let dash = "a dash (-)", ((=) '-')
    let dot = "a dash (.)", ((=) '.')

    let lettersNumbersOrDash =
        "alphanumeric characters or the dash (-)", Char.IsLetterOrDigit <|> (snd dash)

    let lettersNumbersDashOrDot =
        "alphanumeric characters, a dash (-) or a dot (.)", Char.IsLetterOrDigit <|> (snd dash) <|> (snd dot)

    let validate entity inputValue rules =
        rules
        |> Seq.choose (fun rule ->
            match rule entity inputValue with
            | Error msg -> Some msg
            | Ok _ -> None)
        |> Seq.tryHead
        |> Option.map (fun errorMessage ->
            let inputValueDescription =
                if String.IsNullOrWhiteSpace inputValue then
                    ""
                else
                    $". The invalid value is '{inputValue}'"

            Error $"{errorMessage}{inputValueDescription}")
        |> Option.defaultValue (Ok inputValue)

module CosmosDbValidation =
    open Validation

    type CosmosDbName =
        private
        | CosmosDbName of ResourceName

        static member Create name =
            [
                nonEmptyLengthBetween 3 44
                containsOnlyM [ lowercaseLetters; lettersNumbersOrDash ]
            ]
            |> validate "CosmosDb account names" name
            |> Result.map (ResourceName >> CosmosDbName)

        static member Create(ResourceName name) = CosmosDbName.Create name

        member this.ResourceName =
            match this with
            | CosmosDbName name -> name

// https://docs.microsoft.com/en-us/rest/api/servicebus/create-namespace
module ServiceBusValidation =
    open Validation

    type ServiceBusName =
        private
        | ServiceBusName of ResourceName

        static member Create(name: string) =
            [
                nonEmptyLengthBetween 6 50
                containsOnly lettersNumbersOrDash
                startsWith letters
                isNotAGuid
                cannotEndsWith [ ("a dash", "-"); ("a sb postfix", "-sb"); ("a management postfix", "-mgmt") ]
            ]
            |> validate "ServiceBus namespace" name
            |> Result.map (ResourceName >> ServiceBusName)

        member this.ResourceName =
            match this with
            | ServiceBusName name -> name

module ContainerAppValidation =
    open Validation

    type ContainerAppSettingKey =
        private
        | ContainerAppSettingKey of string

        static member Create(name: string) =
            [
                containsOnly lettersNumbersDashOrDot
                startsWith aLetterOrNumber
                endsWith aLetterOrNumber
                containsOnly lowercaseLetters
            ]
            |> validate "Container App Setting Key" name
            |> Result.map ContainerAppSettingKey

        member this.Value =
            match this with
            | ContainerAppSettingKey value -> value

module Insights =

    /// https://docs.microsoft.com/en-us/azure/azure-monitor/essentials/metrics-supported
    type MetricsName =
        | MetricsName of string

        static member PercentageCPU = MetricsName "Percentage CPU"
        static member DiskReadOperationsPerSec = MetricsName "Disk Read Operations/Sec"
        static member DiskWriteOperationsPerSec = MetricsName "Disk Write Operations/Sec"
        static member DiskReadBytes = MetricsName "Disk Read Bytes"
        static member DiskWriteBytes = MetricsName "Disk Write Bytes"
        static member MemoryAvailable = MetricsName "Available Memory Bytes"
        static member NetworkIn = MetricsName "Network In"
        static member NetworkOut = MetricsName "Network Out"
        static member SQL_DB_DTU = MetricsName "dtu_consumption_percent"
        static member SQL_DB_Size = MetricsName "storage_percent"

module Storage =
    open Validation

    type StorageAccountName =
        private
        | StorageAccountName of ResourceName

        static member Create name =
            [
                nonEmptyLengthBetween 3 24
                containsOnlyM [ lowercaseLetters; lettersOrNumbers ]
            ]
            |> validate "Storage account names" name
            |> Result.map (ResourceName >> StorageAccountName)

        static member internal Empty = StorageAccountName ResourceName.Empty
        static member Create(ResourceName name) = StorageAccountName.Create name

        member this.ResourceName =
            match this with
            | StorageAccountName name -> name

    type StorageResourceName =
        private
        | StorageResourceName of ResourceName

        static member Create name =
            [
                nonEmptyLengthBetween 3 63
                startsWith aLetterOrNumber
                endsWith aLetterOrNumber
                containsOnlyM [ lettersNumbersOrDash; lowercaseLetters ]
                arb ("do not allow consecutive dashes", (fun s -> s.Contains "--"))
            ]
            |> validate "Storage resource names" name
            |> Result.map (ResourceName >> StorageResourceName)

        static member Create(ResourceName name) = StorageResourceName.Create name

        member this.ResourceName =
            match this with
            | StorageResourceName name -> name

    type DefaultAccessTier =
        | Hot
        | Cool

    type StoragePerformance =
        | Standard
        | Premium

        member this.ArmValue =
            match this with
            | Standard -> "Standard"
            | Premium -> "Premium"

    type BasicReplication =
        | LRS
        | ZRS

        member this.ReplicationModelDescription =
            match this with
            | LRS -> "LRS"
            | ZRS -> "ZRS"

    type BlobReplication =
        | LRS
        | GRS
        | RAGRS

        member this.ReplicationModelDescription =
            match this with
            | LRS -> "LRS"
            | GRS -> "GRS"
            | RAGRS -> "RAGRS"

    type V1Replication =
        | LRS of StoragePerformance
        | GRS
        | RAGRS

        member this.ReplicationModelDescription =
            match this with
            | LRS _ -> "LRS"
            | GRS -> "GRS"
            | RAGRS -> "RAGRS"

    type V2Replication =
        | LRS of StoragePerformance
        | GRS
        | ZRS
        | GZRS
        | RAGRS
        | RAGZRS

        member this.ReplicationModelDescription =
            match this with
            | LRS _ -> "LRS"
            | GRS -> "GRS"
            | ZRS -> "ZRS"
            | GZRS -> "GZRS"
            | RAGRS -> "RAGRS"
            | RAGZRS -> "RAGZRS"

    type GeneralPurpose =
        | V1 of V1Replication
        | V2 of V2Replication * DefaultAccessTier option

    type Sku =
        | GeneralPurpose of GeneralPurpose
        | Blobs of BlobReplication * DefaultAccessTier option
        | BlockBlobs of BasicReplication
        | Files of BasicReplication

        /// General Purpose V2 Standard LRS with no default access tier.
        static member Standard_LRS = GeneralPurpose(V2(LRS Standard, None))
        /// General Purpose V2 Premium LRS with no default access tier.
        static member Premium_LRS = GeneralPurpose(V2(LRS Premium, None))
        /// General Purpose V2 Standard GRS with no default access tier.
        static member Standard_GRS = GeneralPurpose(V2(GRS, None))
        /// General Purpose V2 Standard RAGRS with no default access tier.
        static member Standard_RAGRS = GeneralPurpose(V2(RAGRS, None))
        /// General Purpose V2 Standard ZRS with no default access tier.
        static member Standard_ZRS = GeneralPurpose(V2(ZRS, None))
        /// General Purpose V2 Standard GZRS with no default access tier.
        static member Standard_GZRS = GeneralPurpose(V2(GZRS, None))
        /// General Purpose V2 Standard RAGZRS with no default access tier.
        static member Standard_RAGZRS = GeneralPurpose(V2(RAGZRS, None))

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
    let NoRuleFilters: string list = []

    type AllOrSpecific<'T> =
        | All
        | Specific of 'T list

    type HttpMethod =
        | DELETE
        | GET
        | HEAD
        | MERGE
        | POST
        | OPTIONS
        | PUT
        | PATCH

        static member All =
            NonEmptyList.create [ DELETE; GET; HEAD; MERGE; POST; OPTIONS; PUT; PATCH ]

        member this.ArmValue =
            match this with
            | DELETE -> "DELETE"
            | GET -> "GET"
            | HEAD -> "HEAD"
            | MERGE -> "MERGE"
            | POST -> "POST"
            | OPTIONS -> "OPTIONS"
            | PUT -> "PUT"
            | PATCH -> "PATCH"

    type CorsRule =
        {
            AllowedOrigins: AllOrSpecific<Uri>
            AllowedMethods: HttpMethod NonEmptyList
            MaxAgeInSeconds: int
            ExposedHeaders: AllOrSpecific<string>
            AllowedHeaders: AllOrSpecific<string>
        }

        static member AllowAll =
            {
                AllowedOrigins = All
                AllowedMethods = HttpMethod.All
                MaxAgeInSeconds = 0
                ExposedHeaders = All
                AllowedHeaders = All
            }

        /// Creates a new CORS rule with
        static member create(?allowedOrigins, ?allowedMethods, ?maxAgeInSeconds, ?exposedHeaders, ?allowedHeaders) =
            let mapDefault mapper defaultValue =
                Option.map mapper >> Option.defaultValue defaultValue

            {
                AllowedOrigins =
                    allowedOrigins
                    |> mapDefault (List.map Uri >> Specific) CorsRule.AllowAll.AllowedOrigins
                AllowedMethods =
                    allowedMethods
                    |> mapDefault NonEmptyList.create CorsRule.AllowAll.AllowedMethods
                MaxAgeInSeconds = defaultArg maxAgeInSeconds CorsRule.AllowAll.MaxAgeInSeconds
                ExposedHeaders = exposedHeaders |> mapDefault Specific CorsRule.AllowAll.ExposedHeaders
                AllowedHeaders = allowedHeaders |> mapDefault Specific CorsRule.AllowAll.AllowedHeaders
            }

    type DeleteRetentionPolicy = { Enabled: bool; Days: int }

    type RestorePolicy = DeleteRetentionPolicy

    type LastAccessTimeTrackingPolicy =
        {
            Enabled: bool
            TrackingGranularityInDays: int
        }

    type ChangeFeed = { Enabled: bool; RetentionInDays: int }

    type Policy =
        | DeleteRetention of DeleteRetentionPolicy
        | Restore of RestorePolicy
        | ContainerDeleteRetention of DeleteRetentionPolicy
        | LastAccessTimeTracking of LastAccessTimeTrackingPolicy
        | ChangeFeed of ChangeFeed

    [<RequireQualifiedAccess>]
    type StorageService =
        | Blobs
        | Tables
        | Files
        | Queues

/// A network represented by an IP address and CIDR prefix.
type public IPAddressCidr = { Address: Net.IPAddress; Prefix: int }

/// Functions for IP networks and CIDR notation.
module IPAddressCidr =
    let parse (s: string) : IPAddressCidr =
        match s.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries) with
        | [| ip; prefix |] ->
            {
                Address = Net.IPAddress.Parse(ip.Trim())
                Prefix = int prefix
            }
        | [| ip |] ->
            {
                Address = Net.IPAddress.Parse(ip.Trim())
                Prefix = 32
            }
        | _ -> raise (ArgumentOutOfRangeException "Malformed CIDR, expecting an IP and prefix separated by '/'")

    let safeParse (s: string) : Result<IPAddressCidr, Exception> =
        try
            parse s |> Ok
        with ex ->
            Error ex

    let format (cidr: IPAddressCidr) = $"{cidr.Address}/{cidr.Prefix}"

    /// Gets uint32 representation of an IP address.
    let private num (ip: Net.IPAddress) =
        ip.GetAddressBytes()
        |> Array.rev
        |> fun bytes -> BitConverter.ToUInt32(bytes, 0)

    /// Gets IP address from uint32 representations
    let private ofNum (num: uint32) =
        num |> BitConverter.GetBytes |> Array.rev |> Net.IPAddress

    let private ipRangeNums (cidr: IPAddressCidr) =
        let ipNumber = cidr.Address |> num
        let mask = 0xffffffffu <<< (32 - cidr.Prefix)
        ipNumber &&& mask, ipNumber ||| (mask ^^^ 0xffffffffu)

    /// Indicates if one CIDR block can fit entirely within another CIDR block
    let contains (inner: IPAddressCidr) (outer: IPAddressCidr) =
        // outer |> IPAddressCidr.contains inner
        let innerStart, innerFinish = ipRangeNums inner
        let outerStart, outerFinish = ipRangeNums outer
        outerStart <= innerStart && outerFinish >= innerFinish

    /// Calculates a range of IP addresses from an CIDR block.
    let ipRange (cidr: IPAddressCidr) =
        let first, last = ipRangeNums cidr
        first |> ofNum, last |> ofNum

    /// Sequence of IP addresses for a CIDR block.
    let addresses (cidr: IPAddressCidr) =
        let first, last = ipRangeNums cidr

        seq {
            for i in first..last do
                ofNum i
        }

    /// Carve a subnet out of an address space.
    let carveAddressSpace (addressSpace: IPAddressCidr) (subnetSizes: int list) =
        [
            let addressSpaceStart, addressSpaceEnd = addressSpace |> ipRangeNums
            let mutable startAddress = addressSpaceStart |> ofNum
            let mutable index = 0

            for size in subnetSizes do
                index <- index + 1

                let cidr =
                    {
                        Address = startAddress
                        Prefix = size
                    }

                let first, last = cidr |> ipRangeNums
                let overlapping = first < (startAddress |> num)

                let last, cidr =
                    if overlapping then
                        let cidr =
                            {
                                Address = ofNum (last + 1u)
                                Prefix = size
                            }

                        let _, last = cidr |> ipRangeNums
                        last, cidr
                    else
                        last, cidr

                if last <= addressSpaceEnd then
                    startAddress <- (last + 1u) |> ofNum
                    cidr
                else
                    raise (IndexOutOfRangeException $"Unable to create subnet {index} of /{size}")
        ]

    /// The first two addresses are the network address and gateway address
    /// so not assignable.
    let assignable (cidr: IPAddressCidr) =
        if cidr.Prefix < 31 then // only has 2 addresses
            cidr |> addresses |> Seq.skip 2
        else
            Seq.empty

module WebApp =
    type WorkerSize =
        | Small
        | Medium
        | Large
        | Serverless

    type Cors =
        | AllOrigins
        | SpecificOrigins of origins: Uri list * allowCredentials: bool option

    type Sku =
        | Shared
        | Free
        | Basic of string
        | Standard of string
        | Premium of string
        | PremiumV2 of string
        | PremiumV3 of string
        | ElasticPremium of string
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
        static member P1V3 = PremiumV3 "P1V3"
        static member P2V3 = PremiumV3 "P2V3"
        static member P3V3 = PremiumV3 "P3V3"
        static member EP1 = ElasticPremium "EP1"
        static member EP2 = ElasticPremium "EP2"
        static member EP3 = ElasticPremium "EP3"
        static member I1 = Isolated "I1"
        static member I2 = Isolated "I2"
        static member I3 = Isolated "I3"
        static member Y1 = Dynamic

    type ConnectionStringKind =
        | MySql
        | SQLServer
        | SQLAzure
        | Custom
        | NotificationHub
        | ServiceBus
        | EventHub
        | ApiHub
        | DocDb
        | RedisCache
        | PostgreSQL

    type ExtensionName = ExtensionName of string

    type Bitness =
        | Bits32
        | Bits64

    type IpSecurityAction =
        | Allow
        | Deny

    type IpSecurityRestriction =
        {
            Name: string
            IpAddressCidr: IPAddressCidr
            Action: IpSecurityAction
        }

        static member Create name cidr action =
            {
                Name = name
                IpAddressCidr = cidr
                Action = action
            }

    type VirtualApplication =
        {
            PhysicalPath: string
            PreloadEnabled: bool option
        }

    module Extensions =
        /// The Microsoft.AspNetCore.AzureAppServices logging extension.
        let Logging = ExtensionName "Microsoft.AspNetCore.AzureAppServices.SiteExtension"

    open Validation

    type WebAppName =
        private
        | WebAppName of ResourceName

        static member Create name =
            [
                nonEmptyLengthBetween 2 60
                containsOnly lettersNumbersOrDash
                cannotStartWith dash
                cannotEndWith dash
            ]
            |> validate "Web App site names" name
            |> Result.map (ResourceName >> WebAppName)

        static member internal Empty = WebAppName ResourceName.Empty
        static member Create(ResourceName name) = WebAppName.Create name

        member this.ResourceName =
            match this with
            | WebAppName name -> name

module CognitiveServices =
    /// Type of SKU. See https://docs.microsoft.com/en-us/rest/api/cognitiveservices/accountmanagement/resourceskus/list
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
        | CognitiveServices
        | ComputerVision
        | ContentModerator
        | CustomVision_Prediction
        | CustomVision_Training
        | Face
        | FormRecognizer
        | ImmersiveReader
        | InkRecognizer
        | LUIS
        | LUIS_Authoring
        | Personalizer
        | QnAMaker
        | SpeakerRecognition
        | SpeechServices
        | TextAnalytics
        | TextTranslation

module BingSearch =
    /// Type of SKU. See https://www.microsoft.com/en-us/bing/apis/pricing
    type Sku =
        /// Free Tier
        | F1
        | S0
        | S1
        | S2
        | S3
        | S4
        | S5
        | S6
        | S7
        | S8
        | S9

module ContainerRegistry =
    /// Container Registry SKU
    type Sku =
        | Basic
        | Standard
        | Premium

module ContainerRegistryValidation =
    open Validation

    type ContainerRegistryName =
        private
        | ContainerRegistryName of ResourceName

        static member Create name =
            [ containsOnly lettersOrNumbers; nonEmptyLengthBetween 5 50 ]
            |> validate "Container Registry Name" name
            |> Result.map (ResourceName >> ContainerRegistryName)

        static member Create(ResourceName name) = ContainerRegistryName.Create name

        member this.ResourceName =
            match this with
            | ContainerRegistryName name -> name

module Search =
    type HostingMode =
        | Default
        | HighDensity

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
        | S_Gen5 of CapacityMin: int * CapacityMax: int

        member this.Name =
            Reflection.FSharpValue.GetUnionFields(this, typeof<Gen5Series>)
            |> fun (v, _) -> v.Name

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

        member this.Name =
            Reflection.FSharpValue.GetUnionFields(this, typeof<FSeries>)
            |> fun (v, _) -> v.Name

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

        member this.Name =
            Reflection.FSharpValue.GetUnionFields(this, typeof<MSeries>)
            |> fun (v, _) -> v.Name

    type VCoreSku =
        | MemoryIntensive of MSeries
        | CpuIntensive of FSeries
        | GeneralPurpose of Gen5Series
        | BusinessCritical of Gen5Series
        | Hyperscale of Gen5Series

        member this.Edition =
            match this with
            | GeneralPurpose _
            | CpuIntensive _ -> "GeneralPurpose"
            | BusinessCritical _
            | MemoryIntensive _ -> "BusinessCritical"
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

        member this.ArmValue =
            match this with
            | AzureHybridBenefit -> "BasePrice"
            | LicenseRequired -> "LicenseIncluded"

    type DbPurchaseModel =
        | DTU of DtuSku
        | VCore of VCoreSku * SqlLicense

        member this.Edition =
            match this with
            | DTU d -> d.Edition
            | VCore (v, _) -> v.Edition

        member this.Name =
            match this with
            | DTU d -> d.Name
            | VCore (v, _) -> v.Name

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
            | PremiumPool c -> c

    open Validation

    type SqlAccountName =
        private
        | SqlAccountName of ResourceName

        static member Create name =
            [
                nonEmptyLengthBetween 1 63
                cannotStartWith dash
                cannotEndWith dash
                containsOnlyM [ lowercaseLetters; lettersNumbersOrDash ]
            ]
            |> validate "SQL account names" name
            |> Result.map (ResourceName >> SqlAccountName)

        static member internal Empty = SqlAccountName ResourceName.Empty
        static member Create(ResourceName name) = SqlAccountName.Create name

        member this.ResourceName =
            match this with
            | SqlAccountName name -> name

    type GeoReplicationSettings =
        {
            /// Suffix name for server and database name
            NameSuffix: string
            /// Replication location, different from the original one
            Location: Farmer.Location
            /// Override database Skus
            DbSku: DtuSku option
        }

/// Represents a role that can be granted to an identity.
type RoleId =
    | RoleId of {| Name: string; Id: Guid |}

    member this.ArmValue =
        match this with
        | RoleId roleId ->
            $"concat('/subscriptions/', subscription().subscriptionId, '/providers/Microsoft.Authorization/roleDefinitions/', '{roleId.Id}')"
            |> ArmExpression.create

    member this.Name =
        match this with
        | (RoleId v) -> v.Name

    member this.Id =
        match this with
        | (RoleId v) -> v.Id

module Identity =

    /// Represents a User Assigned Identity, and the ability to create a Principal Id from it.
    type UserAssignedIdentity =
        | UserAssignedIdentity of ResourceId
        | LinkedUserAssignedIdentity of ResourceId

        member private this.CreateExpression field =
            let resourceId =
                match this with
                | UserAssignedIdentity rid -> rid
                | LinkedUserAssignedIdentity rid -> rid

            ArmExpression
                .create($"reference({resourceId.ArmExpression.Value}).%s{field}")
                .WithOwner(resourceId)

        member this.PrincipalId = this.CreateExpression "principalId" |> PrincipalId
        member this.ClientId = this.CreateExpression "clientId"

        member this.ResourceId =
            match this with
            | UserAssignedIdentity rid -> rid
            | LinkedUserAssignedIdentity rid -> rid

    type SystemIdentity =
        | SystemIdentity of ResourceId

        member this.ResourceId =
            match this with
            | SystemIdentity r -> r

        member private this.CreateExpression field =
            let identity = this.ResourceId.ArmExpression.Value

            ArmExpression
                .create($"reference({identity}, '{this.ResourceId.Type.ApiVersion}', 'full').identity.%s{field}")
                .WithOwner(this.ResourceId)

        member this.PrincipalId = this.CreateExpression "principalId" |> PrincipalId
        member this.ClientId = this.CreateExpression "clientId"

    /// Represents an identity that can be assigned to a resource for impersonation.
    type ManagedIdentity =
        {
            SystemAssigned: FeatureFlag
            UserAssigned: UserAssignedIdentity list
        }

        member this.Dependencies =
            this.UserAssigned
            |> List.choose (fun identity ->
                match identity with
                | UserAssignedIdentity rid -> Some rid
                | LinkedUserAssignedIdentity _ -> None)

        static member Empty =
            {
                SystemAssigned = Disabled
                UserAssigned = []
            }

        static member (+)(a, b) =
            {
                SystemAssigned = (a.SystemAssigned.AsBoolean || b.SystemAssigned.AsBoolean) |> FeatureFlag.ofBool
                UserAssigned = a.UserAssigned @ b.UserAssigned |> List.distinct
            }

        static member (+)(managedIdentity, userAssignedIdentity: UserAssignedIdentity) =
            { managedIdentity with
                UserAssigned = userAssignedIdentity :: managedIdentity.UserAssigned
            }

open Identity

module Containers =
    type DockerImage =
        | PrivateImage of RegistryDomain: string * ContainerName: string * Version: string option
        | PublicImage of ContainerName: string * Version: string option

        member this.ImageTag =
            match this with
            | PrivateImage (registry, container, version) ->
                let version = version |> Option.defaultValue "latest"
                $"{registry}/{container}:{version}"
            | PublicImage (container, version) ->
                let version = version |> Option.defaultValue "latest"
                $"{container}:{version}"

        /// Parses an image tag into a DockerImage record.
        static member Parse(tag: string) =
            match tag.Split([| ':' |], StringSplitOptions.RemoveEmptyEntries) with
            | [| repo; version |] ->
                match repo.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries) |> List.ofArray with
                | first :: rest when (first.Contains ".") ->
                    DockerImage.PrivateImage(first, (rest |> String.concat "/"), Version = Some version)
                | _ -> DockerImage.PublicImage(repo, Version = Some version)
            | [| repo |] ->
                match repo.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries) |> List.ofArray with
                | first :: rest when (first.Contains ".") ->
                    DockerImage.PrivateImage(first, (rest |> String.concat "/"), None)
                | _ -> DockerImage.PublicImage(repo, None)
            | _ -> raiseFarmer $"Malformed docker image tag - incorrect number of version segments: '{tag}'"

/// Credential for accessing an image registry.
type ImageRegistryCredential =
    {
        Server: string
        Username: string
        Password: SecureParameter
        Identity: ManagedIdentity
    }

[<RequireQualifiedAccess>]
type ImageRegistryAuthentication =
    /// Credentials for the container registry are included with the password as a template parameter.
    | Credential of ImageRegistryCredential
    /// Credentials for the container registry will be listed by ARM expression.
    | ListCredentials of ResourceId
    /// Credentials for the container registry are included with the identity as a template parameter.
    | ManagedIdentityCredential of ImageRegistryCredential

[<RequireQualifiedAccess>]
type LogAnalyticsWorkspace =
    | WorkspaceResourceId of LinkedResource
    | WorkspaceKey of WorkspaceId: string * WorkspaceKey: string


module ContainerGroup =
    type PortAccess =
        | PublicPort
        | InternalPort

    type RestartPolicy =
        | NeverRestart
        | AlwaysRestart
        | RestartOnFailure

    type IpAddressType =
        | PublicAddress
        | PublicAddressWithDns of DnsName: string
        | PrivateAddress

    type LogType =
        | ContainerInstanceLogs
        | ContainerInsights

    /// A secret file that will be attached to a container group.
    type SecretFile =
        /// A secret file which will be encoded as base64 data.
        | SecretFileContents of Name: string * Secret: byte array
        /// A secret file which will provided by an ARM parameter at runtime.
        | SecretFileParameter of Name: string * Secret: SecureParameter

    /// A container group volume.
    [<RequireQualifiedAccess>]
    type Volume =
        /// Mounts an empty directory on the container group.
        | EmptyDirectory
        /// Mounts an Azure File Share in the same resource group, performing a key lookup.
        | AzureFileShare of ShareName: ResourceName * StorageAccountName: Storage.StorageAccountName
        /// A git repo volume, clonable by public HTTPS access.
        | GitRepo of Repository: Uri * Directory: string option * Revision: string option
        /// Mounts a volume containing secret files.
        | Secret of SecretFile list

    module Gpu =
        type Sku =
            | K80
            | P100
            | V100

module ContainerService =
    type NetworkPlugin =
        | Kubenet
        | AzureCni

        member this.ArmValue =
            match this with
            | Kubenet -> "kubenet"
            | AzureCni -> "azure"

module Redis =
    type Sku =
        | Basic
        | Standard
        | Premium

module EventHub =
    /// The SKU of the event hub instance.
    type EventHubSku =
        | Basic
        | Standard
        | Premium

    type InflateSetting =
        | ManualInflate
        | AutoInflate of maxThroughput: int

    type AuthorizationRuleRight =
        | Manage
        | Send
        | Listen

module KeyVault =
    type Bypass =
        | AzureServices
        | NoTraffic

    type SoftDeletionMode =
        | SoftDeleteWithPurgeProtection
        | SoftDeletionOnly

    type DefaultAction =
        | Allow
        | Deny

    type Key =
        | Encrypt
        | Decrypt
        | WrapKey
        | UnwrapKey
        | Sign
        | Verify
        | Get
        | List
        | Create
        | Update
        | Import
        | Delete
        | Backup
        | Restore
        | Recover
        | Purge

        static member All = makeAll<Key>

    type Secret =
        | Get
        | List
        | Set
        | Delete
        | Backup
        | Restore
        | Recover
        | Purge

        static member All = makeAll<Secret>
        static member ReadSecrets = [ Get; List ]

    type Certificate =
        | Get
        | List
        | Delete
        | Create
        | Import
        | Update
        | ManageContacts
        | GetIssuers
        | ListIssuers
        | SetIssuers
        | DeleteIssuers
        | ManageIssuers
        | Recover
        | Purge
        | Backup
        | Restore

        static member All = makeAll<Certificate>

    type Storage =
        | Get
        | List
        | Delete
        | Set
        | Update
        | RegenerateKey
        | Recover
        | Purge
        | Backup
        | Restore
        | SetSas
        | ListSas
        | GetSas
        | DeleteSas

        static member All = makeAll<Storage>

    type Sku =
        | Standard
        | Premium

        member this.ArmValue =
            match this with
            | Standard -> "standard"
            | Premium -> "premium"

    type KeyCurveName =
        | P256
        | P256K
        | P384
        | P521

        static member ArmValue =
            function
            | P256 -> "P-256"
            | P256K -> "P-256K"
            | P384 -> "P-384"
            | P521 -> "P-521"

    type RsaKeyLength = RsaKeyLength of int

    type KeyType =
        | EC of KeyCurveName
        | ECHSM of KeyCurveName
        | RSA of RsaKeyLength
        | RSAHSM of RsaKeyLength

        static member ArmValue =
            function
            | EC _ -> "EC"
            | ECHSM _ -> "EC-HSM"
            | RSA _ -> "RSA"
            | RSAHSM _ -> "RSA-HSM"

        static member RSA_2048 = RSA(RsaKeyLength 2048)
        static member RSA_3072 = RSA(RsaKeyLength 3072)
        static member RSA_4096 = RSA(RsaKeyLength 4096)
        static member EC_P256 = EC P256
        static member EC_P384 = EC P384
        static member EC_P521 = EC P521
        static member EC_P256K = EC P256K

    type KeyOperation =
        | Encrypt
        | Decrypt
        | WrapKey
        | UnwrapKey
        | Sign
        | Verify

        static member ArmValue =
            function
            | Encrypt -> "encrypt"
            | Decrypt -> "decrypt"
            | WrapKey -> "wrapKey"
            | UnwrapKey -> "unwrapKey"
            | Sign -> "sign"
            | Verify -> "verify"

module ExpressRoute =
    type Tier =
        | Standard
        | Premium

    type Family =
        | UnlimitedData
        | MeteredData

    type PeeringType =
        | AzurePrivatePeering
        | MicrosoftPeering

        member this.Value = this.ToString()

[<AutoOpen>]
module PrivateIpAddress =
    type AllocationMethod =
        | DynamicPrivateIp
        | StaticPrivateIp of System.Net.IPAddress

module LoadBalancer =
    [<RequireQualifiedAccess>]
    type Sku =
        | Basic
        | Standard

        member this.ArmValue =
            match this with
            | Basic -> "Basic"
            | Standard -> "Standard"

    [<RequireQualifiedAccess>]
    type Tier =
        | Regional
        | Global

        member this.ArmValue =
            match this with
            | Regional -> "Regional"
            | Global -> "Global"

    type LoadBalancerSku = { Name: Sku; Tier: Tier }

    [<RequireQualifiedAccess>]
    type LoadDistributionPolicy =
        | Default
        | SourceIP
        | SourceIPProtocol

        member this.ArmValue =
            match this with
            | Default -> "Default"
            | SourceIP -> "SourceIP"
            | SourceIPProtocol -> "SourceIPProtocol"

    [<RequireQualifiedAccess>]
    type LoadBalancerProbeProtocol =
        | TCP
        | HTTP
        | HTTPS

        member this.ArmValue =
            match this with
            | TCP -> "Tcp"
            | HTTP -> "Http"
            | HTTPS -> "Https"

module ApplicationGateway =
    [<RequireQualifiedAccess>]
    type Tier =
        | Standard
        | Standard_v2
        | WAF
        | WAF_v2

        member this.ArmValue =
            match this with
            | Standard -> "Standard"
            | Standard_v2 -> "Standard_v2"
            | WAF -> "WAF"
            | WAF_v2 -> "WAF_v2"

    [<RequireQualifiedAccess>]
    type Sku =
        | Standard_Large
        | Standard_Medium
        | Standard_Small
        | Standard_v2
        | WAF_Large
        | WAF_Medium
        | WAF_v2

        member this.ArmValue =
            match this with
            | Standard_Large -> "Standard_Large"
            | Standard_Medium -> "Standard_Medium"
            | Standard_Small -> "Standard_Small"
            | Standard_v2 -> "Standard_v2"
            | WAF_Large -> "WAF_Large"
            | WAF_Medium -> "WAF_Medium"
            | WAF_v2 -> "WAF_v2"

    type ApplicationGatewaySku =
        {
            Name: Sku
            Capacity: int option
            Tier: Tier
        }

    [<RequireQualifiedAccess>]
    type BackendAddress =
        | Ip of System.Net.IPAddress
        | Fqdn of string

    [<RequireQualifiedAccess>]
    type HttpStatusCode =
        | HttpStatus403
        | HttpStatus502

        static member toString =
            function
            | HttpStatus403 -> "HttpStatus403"
            | HttpStatus502 -> "HttpStatus502"

        member this.ArmValue = HttpStatusCode.toString this

    [<RequireQualifiedAccess>]
    type RuleType =
        | Basic
        | PathBasedRouting

        member this.ArmValue =
            match this with
            | Basic -> "Basic"
            | PathBasedRouting -> "PathBasedRouting"

    [<RequireQualifiedAccess>]
    type FirewallMode =
        | Detection
        | Prevention

        static member toString =
            function
            | Detection -> "Detection"
            | Prevention -> "Prevention"

        member this.ArmValue = FirewallMode.toString this

    [<RequireQualifiedAccess>]
    type RuleSetType =
        | OWASP

        member this.ArmValue =
            match this with
            | OWASP -> "OWASP"

    [<RequireQualifiedAccess>]
    type CipherSuite =
        | TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA
        | TLS_DHE_DSS_WITH_AES_128_CBC_SHA
        | TLS_DHE_DSS_WITH_AES_128_CBC_SHA256
        | TLS_DHE_DSS_WITH_AES_256_CBC_SHA
        | TLS_DHE_DSS_WITH_AES_256_CBC_SHA256
        | TLS_DHE_RSA_WITH_AES_128_CBC_SHA
        | TLS_DHE_RSA_WITH_AES_128_GCM_SHA256
        | TLS_DHE_RSA_WITH_AES_256_CBC_SHA
        | TLS_DHE_RSA_WITH_AES_256_GCM_SHA384
        | TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA
        | TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256
        | TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256
        | TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA
        | TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384
        | TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384
        | TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA
        | TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256
        | TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256
        | TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA
        | TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384
        | TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384
        | TLS_RSA_WITH_3DES_EDE_CBC_SHA
        | TLS_RSA_WITH_AES_128_CBC_SHA
        | TLS_RSA_WITH_AES_128_CBC_SHA256
        | TLS_RSA_WITH_AES_128_GCM_SHA256
        | TLS_RSA_WITH_AES_256_CBC_SHA
        | TLS_RSA_WITH_AES_256_CBC_SHA256
        | TLS_RSA_WITH_AES_256_GCM_SHA384

        static member toString =
            function
            | TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA -> "TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA"
            | TLS_DHE_DSS_WITH_AES_128_CBC_SHA -> "TLS_DHE_DSS_WITH_AES_128_CBC_SHA"
            | TLS_DHE_DSS_WITH_AES_128_CBC_SHA256 -> "TLS_DHE_DSS_WITH_AES_128_CBC_SHA256"
            | TLS_DHE_DSS_WITH_AES_256_CBC_SHA -> "TLS_DHE_DSS_WITH_AES_256_CBC_SHA"
            | TLS_DHE_DSS_WITH_AES_256_CBC_SHA256 -> "TLS_DHE_DSS_WITH_AES_256_CBC_SHA256"
            | TLS_DHE_RSA_WITH_AES_128_CBC_SHA -> "TLS_DHE_RSA_WITH_AES_128_CBC_SHA"
            | TLS_DHE_RSA_WITH_AES_128_GCM_SHA256 -> "TLS_DHE_RSA_WITH_AES_128_GCM_SHA256"
            | TLS_DHE_RSA_WITH_AES_256_CBC_SHA -> "TLS_DHE_RSA_WITH_AES_256_CBC_SHA"
            | TLS_DHE_RSA_WITH_AES_256_GCM_SHA384 -> "TLS_DHE_RSA_WITH_AES_256_GCM_SHA384"
            | TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA -> "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA"
            | TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256 -> "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256"
            | TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 -> "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256"
            | TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA -> "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA"
            | TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384 -> "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384"
            | TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384 -> "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384"
            | TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA -> "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA"
            | TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256 -> "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256"
            | TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256 -> "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256"
            | TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA -> "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA"
            | TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384 -> "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384"
            | TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384 -> "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384"
            | TLS_RSA_WITH_3DES_EDE_CBC_SHA -> "TLS_RSA_WITH_3DES_EDE_CBC_SHA"
            | TLS_RSA_WITH_AES_128_CBC_SHA -> "TLS_RSA_WITH_AES_128_CBC_SHA"
            | TLS_RSA_WITH_AES_128_CBC_SHA256 -> "TLS_RSA_WITH_AES_128_CBC_SHA256"
            | TLS_RSA_WITH_AES_128_GCM_SHA256 -> "TLS_RSA_WITH_AES_128_GCM_SHA256"
            | TLS_RSA_WITH_AES_256_CBC_SHA -> "TLS_RSA_WITH_AES_256_CBC_SHA"
            | TLS_RSA_WITH_AES_256_CBC_SHA256 -> "TLS_RSA_WITH_AES_256_CBC_SHA256"
            | TLS_RSA_WITH_AES_256_GCM_SHA384 -> "TLS_RSA_WITH_AES_256_GCM_SHA384"

        member this.ArmValue = CipherSuite.toString this


    [<RequireQualifiedAccess>]
    type SslProtocol =
        | TLSv1_0
        | TLSv1_1
        | TLSv1_2

        static member toString =
            function
            | TLSv1_0 -> "TLSv1_0"
            | TLSv1_1 -> "TLSv1_1"
            | TLSv1_2 -> "TLSv1_2"

        member this.ArmValue = SslProtocol.toString this

    [<RequireQualifiedAccess>]
    type PolicyName =
        | Custom of string
        | AppGwSslPolicy20150501
        | AppGwSslPolicy20170401
        | AppGwSslPolicy20170401S

        static member toString this =
            function
            | Custom name -> name
            | AppGwSslPolicy20150501 -> "AppGwSslPolicy20150501"
            | AppGwSslPolicy20170401 -> "AppGwSslPolicy20170401"
            | AppGwSslPolicy20170401S -> "AppGwSslPolicy20170401S"

        member this.ArmValue = PolicyName.toString this

    [<RequireQualifiedAccess>]
    type PolicyType =
        | Custom
        | Predefined

        member this.ArmValue =
            match this with
            | Custom -> "Custom"
            | Predefined -> "Predefined"

    [<RequireQualifiedAccess>]
    type Protocol =
        | Http
        | Https

        member this.ArmValue =
            match this with
            | Http -> "Http"
            | Https -> "Https"

    [<RequireQualifiedAccess>]
    type RedirectType =
        | Found
        | Moved
        | TemporaryRedirect
        | PermanentRedirect

        member this.ArmValue =
            match this with
            | Found -> "Found"
            | Moved -> "Moved"
            | TemporaryRedirect -> "TemporaryRedirect"
            | PermanentRedirect -> "PermanentRedirect"


module VirtualNetworkGateway =
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
            | RouteBased -> "RouteBased"

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
    type MessagingUnits =
        | OneUnit
        | TwoUnits
        | FourUnits

    type Sku =
        | Basic
        | Standard
        | Premium of MessagingUnits

        member this.NameArmValue =
            match this with
            | Basic -> "Basic"
            | Standard -> "Standard"
            | Premium OneUnit
            | Premium TwoUnits
            | Premium FourUnits -> "Premium"

        member this.TierArmValue = this.NameArmValue

    type Rule =
        | SqlFilter of ResourceName * SqlExpression: string
        | CorrelationFilter of Name: ResourceName * CorrelationId: string option * Properties: Map<string, string>

        member this.Name =
            match this with
            | SqlFilter (name, _)
            | CorrelationFilter (name, _, _) -> name

        static member CreateCorrelationFilter(name, properties, ?correlationId) =
            CorrelationFilter(ResourceName name, correlationId, Map properties)

        static member CreateSqlFilter(name, expression) =
            SqlFilter(ResourceName name, expression)

    type AuthorizationRuleRight =
        | Manage
        | Send
        | Listen

module CosmosDb =
    /// The consistency policy of a CosmosDB account.
    type ConsistencyPolicy =
        | Eventual
        | ConsistentPrefix
        | Session
        | BoundedStaleness of maxStaleness: int * maxIntervalSeconds: int
        | Strong

    /// The failover policy of a CosmosDB account.
    type FailoverPolicy =
        | NoFailover
        | AutoFailover of secondaryLocation: Location
        | MultiMaster of secondaryLocation: Location

    /// The kind of index to use on a CosmoDB container.
    type IndexKind =
        | Hash
        | Range

    /// The datatype for the key of index to use on a CosmoDB container.
    type IndexDataType =
        | Number
        | String

    /// A request unit.
    [<Measure>]
    type RU

    /// The throughput for CosmosDB account
    type Throughput =
        | Provisioned of int<RU>
        | Serverless

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

    type Version =
        | VS_9_5
        | VS_9_6
        | VS_10
        | VS_11

module IotHub =
    type Sku =
        | F1
        | B1
        | B2
        | B3
        | S1
        | S2
        | S3

    type Policy =
        | IotHubOwner
        | Service
        | Device
        | RegistryRead
        | RegistryReadWrite

        member this.Index =
            match this with
            | IotHubOwner -> 0
            | Service -> 1
            | Device -> 2
            | RegistryRead -> 3
            | RegistryReadWrite -> 4

module Maps =
    type Sku =
        | S0
        | S1

module SignalR =
    type Sku =
        | Free
        | Standard

    type ServiceMode =
        | Default
        | Serverless
        | Classic

module DataLake =
    type Sku =
        | Consumption
        | Commitment_1TB
        | Commitment_10TB
        | Commitment_100TB
        | Commitment_500TB
        | Commitment_1PB
        | Commitment_5PB

module Network =
    type SubnetDelegationService =
        | SubnetDelegationService of string

        /// Microsoft.ApiManagement/service
        static member ApiManagementService = SubnetDelegationService "Microsoft.ApiManagement/service"
        /// Microsoft.AzureCosmosDB/clusters
        static member CosmosDBClusters = SubnetDelegationService "Microsoft.AzureCosmosDB/clusters"
        /// Microsoft.BareMetal/AzureVMware
        static member BareMetalVMware = SubnetDelegationService "Microsoft.BareMetal/AzureVMware"
        /// Microsoft.BareMetal/CrayServers
        static member BareMetalCrayServers = SubnetDelegationService "Microsoft.BareMetal/CrayServers"
        /// Microsoft.Batch/batchAccounts
        static member BatchAccounts = SubnetDelegationService "Microsoft.Batch/batchAccounts"

        /// Microsoft.ContainerInstance/containerGroups
        static member ContainerGroups =
            SubnetDelegationService "Microsoft.ContainerInstance/containerGroups"

        /// Microsoft.Databricks/workspaces
        static member DatabricksWorkspaces = SubnetDelegationService "Microsoft.Databricks/workspaces"

        /// Microsoft.MachineLearningServices/workspaces
        static member MachineLearningWorkspaces =
            SubnetDelegationService "Microsoft.MachineLearningServices/workspaces"

        /// Microsoft.Netapp/volumes
        static member NetappVolumes = SubnetDelegationService "Microsoft.Netapp/volumes"

        /// Microsoft.ServiceFabricMesh/networks
        static member ServiceFabricMeshNetworks =
            SubnetDelegationService "Microsoft.ServiceFabricMesh/networks"

        /// Microsoft.Sql/managedInstances
        static member SqlManagedInstances = SubnetDelegationService "Microsoft.Sql/managedInstances"
        /// Microsoft.Web/serverFarms
        static member WebServerFarms = SubnetDelegationService "Microsoft.Web/serverFarms"

    type EndpointServiceType =
        | EndpointServiceType of string

        /// Microsoft.AzureActiveDirectory
        static member AzureActiveDirectory = EndpointServiceType "Microsoft.AzureActiveDirectory"
        /// Microsoft.AzureCosmosDB
        static member AzureCosmosDB = EndpointServiceType "Microsoft.AzureCosmosDB"
        /// Microsoft.CognitiveServices
        static member CognitiveServices = EndpointServiceType "Microsoft.CognitiveServices"
        /// Microsoft.ContainerRegistry
        static member ContainerRegistry = EndpointServiceType "Microsoft.ContainerRegistry"
        /// Microsoft.EventHub
        static member EventHub = EndpointServiceType "Microsoft.EventHub"
        /// Microsoft.KeyVault
        static member KeyVault = EndpointServiceType "Microsoft.KeyVault"
        /// Microsoft.ServiceBus
        static member ServiceBus = EndpointServiceType "Microsoft.ServiceBus"
        /// Microsoft.Sql
        static member Sql = EndpointServiceType "Microsoft.Sql"
        /// Microsoft.Storage
        static member Storage = EndpointServiceType "Microsoft.Storage"
        /// Microsoft.Web
        static member Web = EndpointServiceType "Microsoft.Web"


module NetworkSecurity =
    type Operation =
        | Allow
        | Deny

    module Operation =
        let ArmValue =
            function
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
        let ArmValue =
            function
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
        | Range of First: uint16 * Last: uint16
        | AnyPort

        member this.ArmValue =
            match this with
            | Port num -> num |> string
            | Range (first, last) -> $"{first}-{last}"
            | AnyPort -> "*"

    module Port =
        let ArmValue (port: Port) = port.ArmValue

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
        let ArmValue (endpoint: Endpoint) = endpoint.ArmValue

    type NetworkService = NetworkService of name: string * Port

    type TrafficDirection =
        | Inbound
        | Outbound

    module TrafficDirection =
        let ArmValue =
            function
            | Inbound -> "Inbound"
            | Outbound -> "Outbound"

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
        | Premium_AzureFrontDoor
        | Standard_AzureFrontDoor

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

module DeliveryPolicy =
    type IOperator =
        abstract member AsOperator: string
        abstract member AsNegateCondition: bool

    type EqualityOperator =
        | Equals
        | NotEquals

        interface IOperator with
            member this.AsOperator = "Equal"

            member this.AsNegateCondition =
                match this with
                | Equals -> false
                | NotEquals -> true

    type ComparisonOperator =
        | Any
        | Equals
        | Contains
        | BeginsWith
        | EndsWith
        | LessThan
        | LessThanOrEquals
        | GreaterThan
        | GreaterThanOrEquals
        | NotAny
        | NotEquals
        | NotContains
        | NotBeginsWith
        | NotEndsWith
        | NotLessThan
        | NotLessThanOrEquals
        | NotGreaterThan
        | NotGreaterThanOrEquals

        interface IOperator with
            member this.AsOperator =
                match this with
                | Any
                | NotAny -> "Any"
                | Equals
                | NotEquals -> "Equal"
                | Contains
                | NotContains -> "Contains"
                | BeginsWith
                | NotBeginsWith -> "BeginsWith"
                | EndsWith
                | NotEndsWith -> "EndsWith"
                | LessThan
                | NotLessThan -> "LessThan"
                | LessThanOrEquals
                | NotLessThanOrEquals -> "LessThanOrEqual"
                | GreaterThan
                | NotGreaterThan -> "GreaterThan"
                | GreaterThanOrEquals
                | NotGreaterThanOrEquals -> "GreaterThanOrEqual"

            member this.AsNegateCondition =
                match this with
                | NotAny
                | NotEquals
                | NotContains
                | NotBeginsWith
                | NotEndsWith
                | NotLessThan
                | NotLessThanOrEquals
                | NotGreaterThan
                | NotGreaterThanOrEquals -> true
                | _ -> false

    type RemoteAddressOperator =
        | Any
        | GeoMatch
        | IPMatch
        | NotAny
        | NotGeoMatch
        | NotIPMatch

        interface IOperator with
            member this.AsOperator =
                match this with
                | Any
                | NotAny -> "Any"
                | GeoMatch
                | NotGeoMatch -> "GeoMatch"
                | IPMatch
                | NotIPMatch -> "IPMatch"

            member this.AsNegateCondition =
                match this with
                | NotAny
                | NotGeoMatch
                | NotIPMatch -> true
                | _ -> false

    type DeviceType =
        | Mobile
        | Desktop

        member this.ArmValue =
            match this with
            | Desktop -> "Desktop"
            | Mobile -> "Mobile"

    type HttpVersion =
        | Version20
        | Version11
        | Version10
        | Version09

        member this.ArmValue =
            match this with
            | Version20 -> "2.0"
            | Version11 -> "1.1"
            | Version10 -> "1.0"
            | Version09 -> "0.9"

    type RequestMethod =
        | Get
        | Post
        | Put
        | Delete
        | Head
        | Options
        | Trace

        member this.ArmValue =
            match this with
            | Get -> "GET"
            | Post -> "POST"
            | Put -> "PUT"
            | Delete -> "DELETE"
            | Head -> "HEAD"
            | Options -> "OPTIONS"
            | Trace -> "TRACE"

    type Protocol =
        | Http
        | Https

        member this.ArmValue =
            match this with
            | Http -> "HTTP"
            | Https -> "HTTPS"

    type UrlRedirectProtocol =
        | Http
        | Https
        | MatchRequest

        member this.ArmValue =
            match this with
            | Http -> "Http"
            | Https -> "Https"
            | MatchRequest -> "MatchRequest"

    type CaseTransform =
        | NoTransform
        | ToLowercase
        | ToUppercase

        member this.ArmValue =
            match this with
            | NoTransform -> []
            | ToLowercase -> [ "Lowercase" ]
            | ToUppercase -> [ "Uppercase" ]

    type CacheBehaviour =
        | Override of TimeSpan
        | BypassCache
        | SetIfMissing of TimeSpan

        member this.ArmValue =
            match this with
            | Override t ->
                {|
                    Behaviour = "Override"
                    CacheDuration = Some t
                |}
            | BypassCache ->
                {|
                    Behaviour = "BypassCache"
                    CacheDuration = None
                |}
            | SetIfMissing t ->
                {|
                    Behaviour = "SetIfMissing"
                    CacheDuration = Some t
                |}

    type QueryStringCacheBehavior =
        | Include
        | IncludeAll
        | Exclude
        | ExcludeAll

        member this.ArmValue =
            match this with
            | Include -> "Include"
            | IncludeAll -> "IncludeAll"
            | Exclude -> "Exclude"
            | ExcludeAll -> "ExcludeAll"

    type ModifyHeaderAction =
        | Append
        | Overwrite
        | Delete

        member this.ArmValue =
            match this with
            | Append -> "Append"
            | Overwrite -> "Overwrite"
            | Delete -> "Delete"

    type RedirectType =
        | Found
        | Moved
        | TemporaryRedirect
        | PermanentRedirect

        member this.ArmValue =
            match this with
            | Found -> "Found"
            | Moved -> "Moved"
            | TemporaryRedirect -> "TemporaryRedirect"
            | PermanentRedirect -> "PermanentRedirect"


module EventGrid =
    [<Struct>]
    type EventGridEvent<'T> =
        | EventGridEvent of string

        member this.Value =
            match this with
            | EventGridEvent s -> s

/// Built in Azure roles (https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)
module Dns =
    type DnsZoneType =
        | Public
        | Private

    type SrvRecord =
        {
            Priority: int option
            Weight: int option
            Port: int option
            Target: string option
        }

    type SoaRecord =
        {
            Host: string option
            Email: string option
            SerialNumber: int64 option
            RefreshTime: int64 option
            RetryTime: int64 option
            ExpireTime: int64 option
            MinimumTTL: int64 option
        }

    [<RequireQualifiedAccess>]
    type NsRecords =
        | Records of string list
        | SourceZone of ResourceId

    type DnsRecordType =
        | A of TargetResource: ResourceId option * ARecords: string list
        | AAAA of TargetResource: ResourceId option * AaaaRecords: string list
        | CName of TargetResource: ResourceId option * CNameRecord: string option
        | NS of NsRecords
        | PTR of PtrRecords: string list
        | TXT of TxtRecords: string list
        | MX of {| Preference: int; Exchange: string |} list
        | SRV of SrvRecord list
        | SOA of SoaRecord

module Databricks =
    type KeySource =
        | Databricks
        | KeyVault

        member this.ArmValue =
            match this with
            | Databricks -> "Default"
            | KeyVault -> "MicrosoftKeyVault"

    type Sku =
        | Standard
        | Premium

        member this.ArmValue =
            match this with
            | Standard -> "standard"
            | Premium -> "premium"

module TrafficManager =
    type RoutingMethod =
        | Performance
        | Weighted
        | Priority
        | Geographic
        | Subnet

        member this.ArmValue = this.ToString()

    type MonitorProtocol =
        | Http
        | Https

        member this.ArmValue = this.ToString().ToUpperInvariant()

    type MonitorConfig =
        {
            Protocol: MonitorProtocol
            Port: int
            Path: string
            IntervalInSeconds: int<Seconds>
            ToleratedNumberOfFailures: int
            TimeoutInSeconds: int<Seconds>
        }

    type EndpointTarget =
        | Website of ResourceName
        | External of (string * Location)

        member this.ArmValue =
            match this with
            | Website name -> name.Value
            | External (target, _) -> target

module Serialization =
    open System.Text.Json
    open System.Text.Encodings.Web

    let jsonSerializerOptions =
        JsonSerializerOptions(
            WriteIndented = true,
            IgnoreNullValues = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        )

    let toJson x =
        JsonSerializer.Serialize(x, jsonSerializerOptions)

    let ofJson<'T> (x: string) =
        JsonSerializer.Deserialize<'T>(x, jsonSerializerOptions)

module Resource =
    /// Creates a unique IArmResource from an arbitrary object.
    let ofObj armObject =
        { new IArmResource with
            member _.ResourceId =
                ResourceId.create (ResourceType("", ""), ResourceName(System.Guid.NewGuid().ToString()))

            member _.JsonModel = armObject
        }

    /// Creates a unique IArmResource from a JSON string containing the output you want.
    let ofJson = Serialization.ofJson >> ofObj

module Json =
    /// Creates a unique IArmResource from a JSON string containing the output you want.
    let toIArmResource = Resource.ofJson

module Subscription =
    /// Gets an ARM expression pointing to the tenant id of the current subscription.
    let TenantId = ArmExpression.create "subscription().tenantid"

module AzureFirewall =

    type SkuName =
        | AZFW_VNet
        | AZFW_Hub

        member this.ArmValue =
            match this with
            | AZFW_VNet -> "AZFW_VNet"
            | AZFW_Hub -> "AZFW_Hub"

    type SkuTier =
        | Standard
        | Premium

        member this.ArmValue =
            match this with
            | Standard -> "Standard"
            | Premium -> "Premium"

module VirtualHub =
    type Sku =
        | Standard

        member this.ArmValue =
            match this with
            | Standard -> "Standard"

    module HubRouteTable =
        type Destination =
            | CidrDestination of IPAddressCidr list

            member this.DestinationTypeArmValue =
                match this with
                | CidrDestination _ -> "CIDR"

            member this.DestinationsArmValue =
                match this with
                | CidrDestination destinations -> destinations |> List.map IPAddressCidr.format

        [<RequireQualifiedAccess>]
        type NextHop =
            | ResourceId of Farmer.LinkedResource

            member this.NextHopTypeArmValue =
                match this with
                | ResourceId _ -> "ResourceId"

            member this.NextHopArmValue =
                match this with
                | ResourceId linkedResource ->
                    match linkedResource with
                    | Managed resId
                    | Unmanaged resId -> resId.Eval()

module AvailabilityTest =
    /// Availability test types: WebsiteUrl or CustomWebtestXml
    type WebTestType =
        /// Raw Visual Stuido WebTest XML
        | CustomWebtestXml of string
        /// URL of website that the test will ping
        | WebsiteUrl of System.Uri

    /// Availability test sites, from where the webtest is run
    type TestSiteLocation =
        | AvailabilityTestSite of Farmer.Location

        static member NorthCentralUS = Farmer.Location "us-il-ch1-azr" |> AvailabilityTestSite
        static member WestEurope = Farmer.Location "emea-nl-ams-azr" |> AvailabilityTestSite
        static member SoutheastAsia = Farmer.Location "apac-sg-sin-azr" |> AvailabilityTestSite
        static member WestUS = Farmer.Location "us-ca-sjc-azr" |> AvailabilityTestSite
        static member SouthCentralUS = Farmer.Location "us-tx-sn1-azr" |> AvailabilityTestSite
        static member EastUS = Farmer.Location "us-va-ash-azr" |> AvailabilityTestSite
        static member EastAsia = Farmer.Location "apac-hk-hkn-azr" |> AvailabilityTestSite
        static member NorthEurope = Farmer.Location "emea-gb-db3-azr" |> AvailabilityTestSite
        static member JapanEast = Farmer.Location "apac-jp-kaw-edge" |> AvailabilityTestSite
        static member AustraliaEast = Farmer.Location "emea-au-syd-edge" |> AvailabilityTestSite
        static member FranceCentralSouth = Farmer.Location "emea-ch-zrh-edge" |> AvailabilityTestSite
        static member FranceCentral = Farmer.Location "emea-fr-pra-edge" |> AvailabilityTestSite
        static member UKSouth = Farmer.Location "emea-ru-msa-edge" |> AvailabilityTestSite
        static member UKWest = Farmer.Location "emea-se-sto-edge" |> AvailabilityTestSite
        static member BrazilSouth = Farmer.Location "latam-br-gru-edge" |> AvailabilityTestSite
        static member CentralUS = Farmer.Location "us-fl-mia-edge" |> AvailabilityTestSite

module ContainerApp =
    //type SecretRef = SecretRef of string
    type EventHubScaleRule =
        {
            ConsumerGroup: string
            UnprocessedEventThreshold: int
            CheckpointBlobContainerName: string
            EventHubConnectionSecretRef: string
            StorageConnectionSecretRef: string
        }

    type ServiceBusScaleRule =
        {
            QueueName: string
            MessageCount: int
            SecretRef: string
        }

    type HttpScaleRule = { ConcurrentRequests: int }

    type StorageQueueScaleRule =
        {
            QueueName: string
            QueueLength: int
            StorageConnectionSecretRef: string
            AccountName: string
        }

    type UtilisationRule = { Utilisation: int }
    type AverageValueRule = { AverageValue: int }

    type MetricScaleRule =
        | Utilisation of UtilisationRule
        | AverageValue of AverageValueRule

    [<RequireQualifiedAccess>]
    type ScaleRule =
        | EventHub of EventHubScaleRule
        | ServiceBus of ServiceBusScaleRule
        | Http of HttpScaleRule
        | CPU of MetricScaleRule
        | Memory of MetricScaleRule
        | StorageQueue of StorageQueueScaleRule
        | Custom of obj

    type Transport =
        | HTTP1
        | HTTP2
        | Auto

    type IngressMode =
        | External of port: uint16 * Transport option
        | InternalOnly

    type ActiveRevisionsMode =
        | Single
        | Multiple

    type StorageAccessMode =
        | ReadOnly
        | ReadWrite

        member this.ArmValue = this.ToString()

    [<RequireQualifiedAccess>]
    type Volume =
        /// Mounts an empty directory on the container group.
        | EmptyDirectory
        /// Mounts an Azure File Share in the same resource group, performing a key lookup.
        | AzureFileShare of ShareName: ResourceName * StorageAccountName: Storage.StorageAccountName * StorageAccessMode

namespace Farmer.DiagnosticSettings

open Farmer
open System

[<AutoOpen>]
module private Helpers =
    let (|InBounds|OutOfBounds|) days =
        if days > 365<Days> then OutOfBounds days
        elif days < 1<Days> then OutOfBounds days
        else InBounds days

[<Struct>]
type LogCategory =
    | LogCategory of string

    member this.Value =
        match this with
        | LogCategory v -> v

type RetentionPolicy =
    {
        Enabled: bool
        RetentionPeriod: int<Days>
    }

    static member Create(retentionPeriod, ?enabled) =
        match retentionPeriod with
        | OutOfBounds days ->
            raiseFarmer $"The retention period must be between 1 and 365 days. It is currently {days}."
        | InBounds _ ->
            {
                Enabled = defaultArg enabled true
                RetentionPeriod = retentionPeriod
            }

type MetricSetting =
    {
        Category: string
        TimeGrain: TimeSpan option
        Enabled: bool
        RetentionPolicy: RetentionPolicy option
    }

    static member Create(category, ?retentionPeriod, ?timeGrain) =
        {
            Category = category
            TimeGrain = timeGrain
            Enabled = true
            RetentionPolicy = retentionPeriod |> Option.map (fun days -> RetentionPolicy.Create(days, true))
        }

type LogSetting =
    {
        Category: LogCategory
        Enabled: bool
        RetentionPolicy: RetentionPolicy option
    }

    static member Create(category, ?retentionPeriod) =
        {
            Category = category
            Enabled = true
            RetentionPolicy = retentionPeriod |> Option.map (fun days -> RetentionPolicy.Create(days, true))
        }

    static member Create(category, ?retentionPeriod) =
        LogSetting.Create(LogCategory category, ?retentionPeriod = retentionPeriod)

/// Represents the kind of destination for log analytics
type LogAnalyticsDestination =
    | AzureDiagnostics
    | Dedicated
