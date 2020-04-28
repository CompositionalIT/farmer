namespace Farmer
open Microsoft.FSharp.Reflection

type internal ArmValueAttribute(value : string) = class
    inherit System.Attribute()
    member val ArmValue = value
end
[<AutoOpen>]
module ArmValue =
    let getArmValue (this : 'a) =
        match FSharpValue.GetUnionFields(this, typeof<'a>) with
        | case, _ ->
            case.GetCustomAttributes(typeof<ArmValueAttribute>)
            |> Seq.tryHead
            |> Option.map (fun attr -> attr :?> ArmValueAttribute)
            |> Option.map (fun attr -> attr.ArmValue)
            |> Option.defaultValue (this.GetType().Name)

[<AutoOpen>]
module Locations =
    type Locations =
    | [<ArmValue("eastasia")>]EastAsia
    | [<ArmValue("southeastasia")>]SoutheastAsia
    | [<ArmValue("centralus")>]CentralUS
    | [<ArmValue("eastus")>]EastUS
    | [<ArmValue("eastus2")>]EastUS2
    | [<ArmValue("westus")>]WestUS
    | [<ArmValue("northcentralus")>]NorthCentralUS
    | [<ArmValue("southcentralus")>]SouthCentralUS
    | [<ArmValue("northeurope")>]NorthEurope
    | [<ArmValue("westeurope")>]WestEurope
    | [<ArmValue("japanwest")>]JapanWest
    | [<ArmValue("japaneast")>]JapanEast
    | [<ArmValue("brazilsouth")>]BrazilSouth
    | [<ArmValue("australiaeast")>]AustraliaEast
    | [<ArmValue("australiasoutheast")>]AustraliaSoutheast
    | [<ArmValue("southindia")>]SouthIndia
    | [<ArmValue("centralindia")>]CentralIndia
    | [<ArmValue("westindia")>]WestIndia
    with
        member this.ArmValue = getArmValue this

[<AutoOpen>]
module VMSize =
    type VMSize =
    | [<ArmValue("Basic_A0")>] Basic_A0
    | [<ArmValue("Basic_A1")>] Basic_A1
    | [<ArmValue("Basic_A2")>] Basic_A2
    | [<ArmValue("Basic_A3")>] Basic_A3
    | [<ArmValue("Basic_A4")>] Basic_A4
    | [<ArmValue("Standard_A0")>] Standard_A0
    | [<ArmValue("Standard_A1")>] Standard_A1
    | [<ArmValue("Standard_A2")>] Standard_A2
    | [<ArmValue("Standard_A3")>] Standard_A3
    | [<ArmValue("Standard_A4")>] Standard_A4
    | [<ArmValue("Standard_A5")>] Standard_A5
    | [<ArmValue("Standard_A6")>] Standard_A6
    | [<ArmValue("Standard_A7")>] Standard_A7
    | [<ArmValue("Standard_A8")>] Standard_A8
    | [<ArmValue("Standard_A9")>] Standard_A9
    | [<ArmValue("Standard_A10")>] Standard_A10
    | [<ArmValue("Standard_A11")>] Standard_A11
    | [<ArmValue("Standard_A1_v2")>] Standard_A1_v2
    | [<ArmValue("Standard_A2_v2")>] Standard_A2_v2
    | [<ArmValue("Standard_A4_v2")>] Standard_A4_v2
    | [<ArmValue("Standard_A8_v2")>] Standard_A8_v2
    | [<ArmValue("Standard_A2m_v2")>] Standard_A2m_v2
    | [<ArmValue("Standard_A4m_v2")>] Standard_A4m_v2
    | [<ArmValue("Standard_A8m_v2")>] Standard_A8m_v2
    | [<ArmValue("Standard_B1s")>] Standard_B1s
    | [<ArmValue("Standard_B1ms")>] Standard_B1ms
    | [<ArmValue("Standard_B2s")>] Standard_B2s
    | [<ArmValue("Standard_B2ms")>] Standard_B2ms
    | [<ArmValue("Standard_B4ms")>] Standard_B4ms
    | [<ArmValue("Standard_B8ms")>] Standard_B8ms
    | [<ArmValue("Standard_D1")>] Standard_D1
    | [<ArmValue("Standard_D2")>] Standard_D2
    | [<ArmValue("Standard_D3")>] Standard_D3
    | [<ArmValue("Standard_D4")>] Standard_D4
    | [<ArmValue("Standard_D11")>] Standard_D11
    | [<ArmValue("Standard_D12")>] Standard_D12
    | [<ArmValue("Standard_D13")>] Standard_D13
    | [<ArmValue("Standard_D14")>] Standard_D14
    | [<ArmValue("Standard_D1_v2")>] Standard_D1_v2
    | [<ArmValue("Standard_D2_v2")>] Standard_D2_v2
    | [<ArmValue("Standard_D3_v2")>] Standard_D3_v2
    | [<ArmValue("Standard_D4_v2")>] Standard_D4_v2
    | [<ArmValue("Standard_D5_v2")>] Standard_D5_v2
    | [<ArmValue("Standard_D2_v3")>] Standard_D2_v3
    | [<ArmValue("Standard_D4_v3")>] Standard_D4_v3
    | [<ArmValue("Standard_D8_v3")>] Standard_D8_v3
    | [<ArmValue("Standard_D16_v3")>] Standard_D16_v3
    | [<ArmValue("Standard_D32_v3")>] Standard_D32_v3
    | [<ArmValue("Standard_D64_v3")>] Standard_D64_v3
    | [<ArmValue("Standard_D2s_v3")>] Standard_D2s_v3
    | [<ArmValue("Standard_D4s_v3")>] Standard_D4s_v3
    | [<ArmValue("Standard_D8s_v3")>] Standard_D8s_v3
    | [<ArmValue("Standard_D16s_v3")>] Standard_D16s_v3
    | [<ArmValue("Standard_D32s_v3")>] Standard_D32s_v3
    | [<ArmValue("Standard_D64s_v3")>] Standard_D64s_v3
    | [<ArmValue("Standard_D11_v2")>] Standard_D11_v2
    | [<ArmValue("Standard_D12_v2")>] Standard_D12_v2
    | [<ArmValue("Standard_D13_v2")>] Standard_D13_v2
    | [<ArmValue("Standard_D14_v2")>] Standard_D14_v2
    | [<ArmValue("Standard_D15_v2")>] Standard_D15_v2
    | [<ArmValue("Standard_DS1")>] Standard_DS1
    | [<ArmValue("Standard_DS2")>] Standard_DS2
    | [<ArmValue("Standard_DS3")>] Standard_DS3
    | [<ArmValue("Standard_DS4")>] Standard_DS4
    | [<ArmValue("Standard_DS11")>] Standard_DS11
    | [<ArmValue("Standard_DS12")>] Standard_DS12
    | [<ArmValue("Standard_DS13")>] Standard_DS13
    | [<ArmValue("Standard_DS14")>] Standard_DS14
    | [<ArmValue("Standard_DS1_v2")>] Standard_DS1_v2
    | [<ArmValue("Standard_DS2_v2")>] Standard_DS2_v2
    | [<ArmValue("Standard_DS3_v2")>] Standard_DS3_v2
    | [<ArmValue("Standard_DS4_v2")>] Standard_DS4_v2
    | [<ArmValue("Standard_DS5_v2")>] Standard_DS5_v2
    | [<ArmValue("Standard_DS11_v2")>] Standard_DS11_v2
    | [<ArmValue("Standard_DS12_v2")>] Standard_DS12_v2
    | [<ArmValue("Standard_DS13_v2")>] Standard_DS13_v2
    | [<ArmValue("Standard_DS14_v2")>] Standard_DS14_v2
    | [<ArmValue("Standard_DS15_v2")>] Standard_DS15_v2
    | [<ArmValue("Standard_DS13-4_v2")>] Standard_DS13_4_v2
    | [<ArmValue("Standard_DS13-2_v2")>] Standard_DS13_2_v2
    | [<ArmValue("Standard_DS14-8_v2")>] Standard_DS14_8_v2
    | [<ArmValue("Standard_DS14-4_v2")>] Standard_DS14_4_v2
    | [<ArmValue("Standard_E2_v3")>] Standard_E2_v3_v3
    | [<ArmValue("Standard_E4_v3")>] Standard_E4_v3
    | [<ArmValue("Standard_E8_v3")>] Standard_E8_v3
    | [<ArmValue("Standard_E16_v3")>] Standard_E16_v3
    | [<ArmValue("Standard_E32_v3")>] Standard_E32_v3
    | [<ArmValue("Standard_E64_v3")>] Standard_E64_v3
    | [<ArmValue("Standard_E2s_v3")>] Standard_E2s_v3
    | [<ArmValue("Standard_E4s_v3")>] Standard_E4s_v3
    | [<ArmValue("Standard_E8s_v3")>] Standard_E8s_v3
    | [<ArmValue("Standard_E16s_v3")>] Standard_E16s_v3
    | [<ArmValue("Standard_E32s_v3")>] Standard_E32s_v3
    | [<ArmValue("Standard_E64s_v3")>] Standard_E64s_v3
    | [<ArmValue("Standard_E32-16_v3")>] Standard_E32_16_v3
    | [<ArmValue("Standard_E32-8s_v3")>] Standard_E32_8s_v3
    | [<ArmValue("Standard_E64-32s_v3")>] Standard_E64_32s_v3
    | [<ArmValue("Standard_E64-16s_v3")>] Standard_E64_16s_v3
    | [<ArmValue("Standard_F1")>] Standard_F1
    | [<ArmValue("Standard_F2")>] Standard_F2
    | [<ArmValue("Standard_F4")>] Standard_F4
    | [<ArmValue("Standard_F8")>] Standard_F8
    | [<ArmValue("Standard_F16")>] Standard_F16
    | [<ArmValue("Standard_F1s")>] Standard_F1s
    | [<ArmValue("Standard_F2s")>] Standard_F2s
    | [<ArmValue("Standard_F4s")>] Standard_F4s
    | [<ArmValue("Standard_F8s")>] Standard_F8s
    | [<ArmValue("Standard_F16s")>] Standard_F16s
    | [<ArmValue("Standard_F2s_v2")>] Standard_F2s_v2
    | [<ArmValue("Standard_F4s_v2")>] Standard_F4s_v2
    | [<ArmValue("Standard_F8s_v2")>] Standard_F8s_v2
    | [<ArmValue("Standard_F16s_v2")>] Standard_F16s_v2
    | [<ArmValue("Standard_F32s_v2")>] Standard_F32s_v2
    | [<ArmValue("Standard_F64s_v2")>] Standard_F64s_v2
    | [<ArmValue("Standard_F72s_v2")>] Standard_F72s_v2
    | [<ArmValue("Standard_G1")>] Standard_G1
    | [<ArmValue("Standard_G2")>] Standard_G2
    | [<ArmValue("Standard_G3")>] Standard_G3
    | [<ArmValue("Standard_G4")>] Standard_G4
    | [<ArmValue("Standard_G5")>] Standard_G5
    | [<ArmValue("Standard_GS1")>] Standard_GS1
    | [<ArmValue("Standard_GS2")>] Standard_GS2
    | [<ArmValue("Standard_GS3")>] Standard_GS3
    | [<ArmValue("Standard_GS4")>] Standard_GS4
    | [<ArmValue("Standard_GS5")>] Standard_GS5
    | [<ArmValue("Standard_GS4-8")>] Standard_GS4_8
    | [<ArmValue("Standard_GS4-4")>] Standard_GS4_4
    | [<ArmValue("Standard_GS5-16")>] Standard_GS5_16
    | [<ArmValue("Standard_GS5-8")>] Standard_GS5_8
    | [<ArmValue("Standard_H8")>] Standard_H8
    | [<ArmValue("Standard_H16")>] Standard_H16
    | [<ArmValue("Standard_H8m")>] Standard_H8m
    | [<ArmValue("Standard_H16m")>] Standard_H16m
    | [<ArmValue("Standard_H16r")>] Standard_H16r
    | [<ArmValue("Standard_H16mr")>] Standard_H16mr
    | [<ArmValue("Standard_L4s")>] Standard_L4s
    | [<ArmValue("Standard_L8s")>] Standard_L8s
    | [<ArmValue("Standard_L16s")>] Standard_L16s
    | [<ArmValue("Standard_L32s")>] Standard_L32s
    | [<ArmValue("Standard_M64s")>] Standard_M64s
    | [<ArmValue("Standard_M64ms")>] Standard_M64ms
    | [<ArmValue("Standard_M128s")>] Standard_M128s
    | [<ArmValue("Standard_M128ms")>] Standard_M128ms
    | [<ArmValue("Standard_M64-32ms")>] Standard_M64_32ms
    | [<ArmValue("Standard_M64-16ms")>] Standard_M64_16ms
    | [<ArmValue("Standard_M128-64ms")>] Standard_M128_64ms
    | [<ArmValue("Standard_M128-32ms")>] Standard_M128_32ms
    | [<ArmValue("Standard_NC6")>] Standard_NC6
    | [<ArmValue("Standard_NC12")>] Standard_NC12
    | [<ArmValue("Standard_NC24")>] Standard_NC24
    | [<ArmValue("Standard_NC24r")>] Standard_NC24r
    | [<ArmValue("Standard_NC6s_v2")>] Standard_NC6s_v2
    | [<ArmValue("Standard_NC12s_v2")>] Standard_NC12s_v2
    | [<ArmValue("Standard_NC24s_v2")>] Standard_NC24s_v2
    | [<ArmValue("Standard_NC24rs_v2")>] Standard_NC24rs_v2
    | [<ArmValue("Standard_NC6s_v3")>] Standard_NC6s_v3
    | [<ArmValue("Standard_NC12s_v3")>] Standard_NC12s_v3
    | [<ArmValue("Standard_NC24s_v3")>] Standard_NC24s_v3
    | [<ArmValue("Standard_NC24rs_v3")>] Standard_NC24rs_v3
    | [<ArmValue("Standard_ND6s")>] Standard_ND6s
    | [<ArmValue("Standard_ND12s")>] Standard_ND12s
    | [<ArmValue("Standard_ND24s")>] Standard_ND24s
    | [<ArmValue("Standard_ND24rs")>] Standard_ND24rs
    | [<ArmValue("Standard_NV6")>] Standard_NV6
    | [<ArmValue("Standard_NV12")>] Standard_NV12
    | [<ArmValue("Standard_NV24")>] Standard_NV24
    with
        member this.ArmValue = getArmValue this

[<AutoOpen>]
module Storage =
    type Sku =
    | [<ArmValue("Standard_LRS")>]StandardLRS
    | [<ArmValue("Standard_GRS")>]StandardGRS
    | [<ArmValue("Standard_RAGRS")>]StandardRAGRS
    | [<ArmValue("Standard_ZRS")>]StandardZRS
    | [<ArmValue("Standard_GZRS")>]StandardGZRS
    | [<ArmValue("Standard_RAGZRS")>]StandardRAGZRS
    | [<ArmValue("Premium_LRS")>]PremiumLRS
    | [<ArmValue("Premium_ZRS")>]PremiumZRS
    with
        member this.ArmValue = getArmValue this

[<AutoOpen>]
module CommonImages =
    type ImageDefinition = {
        Offer : string
        Publisher : string
        Sku : string
    }

    let CentOS_75                      = { Offer = "CentOS";        Publisher = "OpenLogic";              Sku = "7.5"                }
    let CoreOS_Stable                  = { Offer = "CoreOS";        Publisher = "CoreOS";                 Sku = "Stable"             }
    let debian_10                      = { Offer = "debian-10";     Publisher = "Debian";                 Sku = "10"                 }
    let openSUSE_423                   = { Offer = "openSUSE-Leap"; Publisher = "SUSE";                   Sku = "42.3"               }
    let RHEL_7RAW                      = { Offer = "RHEL";          Publisher = "RedHat";                 Sku = "7-RAW"              }
    let SLES_15                        = { Offer = "SLES";          Publisher = "SUSE";                   Sku = "15"                 }
    let UbuntuServer_1804LTS           = { Offer = "UbuntuServer";  Publisher = "Canonical";              Sku = "18.04-LTS"          }
    let WindowsServer_2019Datacenter   = { Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2019-Datacenter"    }
    let WindowsServer_2016Datacenter   = { Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2016-Datacenter"    }
    let WindowsServer_2012R2Datacenter = { Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2012-R2-Datacenter" }
    let WindowsServer_2012Datacenter   = { Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2012-Datacenter"    }
    let WindowsServer_2008R2SP1        = { Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2008-R2-SP1"        }