[<AutoOpen>]
module Farmer.Builders.HostGroups

open System
open Farmer
open Farmer.Arm
open Farmer.DedicatedHosts

type HostGroupConfig =
    {
        Name: ResourceName
        AvailabilityZones: AvailabilityZone list option
        SupportAutomaticPlacement: FeatureFlag option
        PlatformFaultDomainCount: PlatformFaultDomainCount option
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = hostGroups.resourceId this.Name

        member this.BuildResources location =
            let hostGroup: HostGroup =
                {
                    Name = this.Name
                    Location = location
                    AvailabilityZones = this.AvailabilityZones |> (Option.defaultValue List.Empty)
                    SupportAutomaticPlacement =
                        this.SupportAutomaticPlacement |> (Option.defaultValue FeatureFlag.Disabled)
                    PlatformFaultDomainCount =
                        this.PlatformFaultDomainCount
                        |> (Option.defaultValue PlatformFaultDomainCount.One)
                    Tags = this.Tags
                }

            [ hostGroup ]

type HostGroupBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            AvailabilityZones = None
            SupportAutomaticPlacement = None
            PlatformFaultDomainCount = None
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: HostGroupConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "add_availability_zones">]
    member _.AddAvailabilityZone(state: HostGroupConfig, az: string list) =
        match state.AvailabilityZones with
        | None ->
            { state with
                AvailabilityZones = Some(az |> List.map AvailabilityZone.Parse)
            }
        | Some curZones ->
            { state with
                AvailabilityZones = Some((az |> List.map AvailabilityZone.Parse) @ curZones)
            }

    [<CustomOperation "add_availability_zones">]
    member _.AddAvailabilityZone(state: HostGroupConfig, az: AvailabilityZone list) =
        match state.AvailabilityZones with
        | None ->
            { state with
                AvailabilityZones = Some az
            }
        | Some curZones ->
            { state with
                AvailabilityZones = Some(az @ curZones)
            }

    [<CustomOperation "supportAutomaticPlacement">]
    member _.SupportAutomaticPlacement(state: HostGroupConfig, flag: FeatureFlag) =
        { state with
            SupportAutomaticPlacement = Some flag
        }

    [<CustomOperation "supportAutomaticPlacement">]
    member _.SupportAutomaticPlacement(state: HostGroupConfig, flag: bool) =
        { state with
            SupportAutomaticPlacement = Some(FeatureFlag.ofBool flag)
        }

    [<CustomOperation "platformFaultDomainCount">]
    member _.PlatformFaultDomainCount(state: HostGroupConfig, domainCount: int) =
        { state with
            PlatformFaultDomainCount = Some(PlatformFaultDomainCount.Parse domainCount)
        }

    [<CustomOperation "platformFaultDomainCount">]
    member _.PlatformFaultDomainCount(state: HostGroupConfig, domainCount: PlatformFaultDomainCount) =
        { state with
            PlatformFaultDomainCount = Some domainCount
        }

let hostGroup = HostGroupBuilder()

type HostConfig =
    {
        Name: ResourceName
        AutoReplaceOnFailure: FeatureFlag option
        LicenseType: HostLicenseType option
        HostSku: HostSku option
        PlatformFaultDomain: PlatformFaultDomain option
        HostGroupName: LinkedResource
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = hosts.resourceId this.Name

        member this.BuildResources location =
            match this.HostGroupName.Name.Value, this.HostSku with
            | "", _ -> raiseFarmer "Hosts must have a linked host group"
            | _, None -> raiseFarmer "Hosts must have a sku"
            | _, Some sku ->
                let host: Compute.Host =
                    {
                        Name = this.Name
                        Location = location
                        Sku = sku
                        ParentHostGroupName = this.HostGroupName.ResourceId.Name
                        AutoReplaceOnFailure = FeatureFlag.Enabled
                        LicenseType = HostLicenseType.NoLicense
                        PlatformFaultDomain = this.PlatformFaultDomain |> Option.defaultValue PlatformFaultDomain.Zero
                        Tags = this.Tags
                    }

                [ host ]

type HostBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            AutoReplaceOnFailure = None
            LicenseType = None
            HostSku = None
            PlatformFaultDomain = None
            HostGroupName = Managed(ResourceId.create (hostGroups, ResourceName.Empty))
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: HostConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "autoReplaceOnFailure">]
    member _.AutoReplaceOnFailure(state: HostConfig, flag: FeatureFlag) =
        { state with
            AutoReplaceOnFailure = Some flag
        }

    [<CustomOperation "licenseType">]
    member _.LicenseType(state: HostConfig, licenseType: HostLicenseType) =
        { state with
            LicenseType = Some licenseType
        }

    [<CustomOperation "sku">]
    member _.Sku(state: HostConfig, s: HostSku) = { state with HostSku = Some s }

    [<CustomOperation "sku">]
    member _.Sku(state: HostConfig, skuName: string) =
        { state with
            HostSku = Some(HostSku skuName)
        }

    [<CustomOperation "platformFaultDomain">]
    member _.PlatformFaultDomain(state: HostConfig, faultDomains: int) =
        { state with
            PlatformFaultDomain = Some(faultDomains |> PlatformFaultDomain.Parse)
        }

    [<CustomOperation "parentHostGroup">]
    member _.ParentHostGroup(state: HostConfig, hostGroup: LinkedResource) =
        { state with HostGroupName = hostGroup }

    [<CustomOperation "parentHostGroup">]
    member _.ParentHostGroup(state: HostConfig, hostGroupName: ResourceName) =
        { state with
            HostGroupName = Unmanaged(ResourceId.create (hostGroups, hostGroupName))
        }

let host = HostBuilder()
