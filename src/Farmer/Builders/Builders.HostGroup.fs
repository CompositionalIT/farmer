[<AutoOpen>]
module Farmer.Builders.HostGroups

open System
open Farmer
open Farmer.Arm
open Farmer.DedicatedHosts

type HostGroupConfig =
    {
        Name: ResourceName
        AvailabilityZone: string option
        SupportAutomaticPlacement: FeatureFlag option
        PlatformFaultDomainCount: PlatformFaultDomainCount option
        DependsOn: Set<ResourceId>
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = hostGroups.resourceId this.Name

        member this.BuildResources location =
            let hostGroup: HostGroup =
                {
                    Name = this.Name
                    Location = location
                    AvailabilityZone = this.AvailabilityZone |> Option.map (fun zone -> [zone]) |> Option.defaultValue []
                    SupportAutomaticPlacement =
                        this.SupportAutomaticPlacement |> Option.defaultValue FeatureFlag.Disabled
                    PlatformFaultDomainCount =
                        this.PlatformFaultDomainCount |> Option.defaultValue (PlatformFaultDomainCount 1)
                    DependsOn = this.DependsOn
                    Tags = this.Tags
                }

            [ hostGroup ]

type HostGroupBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            AvailabilityZone = None
            SupportAutomaticPlacement = None
            PlatformFaultDomainCount = None
            DependsOn = Set.empty
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: HostGroupConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "add_availability_zone">]
    member _.AddAvailabilityZone(state: HostGroupConfig, az: string) =
        { state with
            AvailabilityZone = Some az
        }

    [<CustomOperation "support_automatic_placement">]
    member _.SupportAutomaticPlacement(state: HostGroupConfig, flag: FeatureFlag) =
        { state with
            SupportAutomaticPlacement = Some flag
        }

    [<CustomOperation "support_automatic_placement">]
    member _.SupportAutomaticPlacement(state: HostGroupConfig, flag: bool) =
        { state with
            SupportAutomaticPlacement = Some(FeatureFlag.ofBool flag)
        }

    [<CustomOperation "platform_fault_domain_count">]
    member _.PlatformFaultDomainCount(state: HostGroupConfig, domainCount: int) =
        { state with
            PlatformFaultDomainCount = Some(PlatformFaultDomainCount.Parse domainCount)
        }
        
    interface IDependable<HostGroupConfig> with
        member _.Add state resIds =
            { state with
                DependsOn = state.DependsOn + resIds
            }

let hostGroup = HostGroupBuilder()

type HostConfig =
    {
        Name: ResourceName
        AutoReplaceOnFailure: FeatureFlag option
        LicenseType: HostLicenseType option
        HostSku: HostSku option
        PlatformFaultDomain: PlatformFaultDomainCount option
        HostGroupName: LinkedResource
        DependsOn: Set<ResourceId>
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
                        DependsOn = this.DependsOn
                        PlatformFaultDomain = this.PlatformFaultDomain |> Option.defaultValue (PlatformFaultDomainCount 1)
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
            DependsOn = Set.empty
            PlatformFaultDomain = None
            HostGroupName = Managed(ResourceId.create (hostGroups, ResourceName.Empty))
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: HostConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "auto_replace_on_failure">]
    member _.AutoReplaceOnFailure(state: HostConfig, flag: FeatureFlag) =
        { state with
            AutoReplaceOnFailure = Some flag
        }

    [<CustomOperation "license_type">]
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

    [<CustomOperation "platform_fault_domain">]
    member _.PlatformFaultDomain(state: HostConfig, faultDomains: int) =
        { state with
            PlatformFaultDomain = Some(faultDomains |> PlatformFaultDomainCount.Parse)
        }

    [<CustomOperation "parent_host_group">]
    member _.ParentHostGroup(state: HostConfig, hostGroup: LinkedResource) =
        { state with HostGroupName = hostGroup }

    [<CustomOperation "parent_host_group">]
    member _.ParentHostGroup(state: HostConfig, hostGroupName: ResourceName) =
        { state with
            HostGroupName = Unmanaged(ResourceId.create (hostGroups, hostGroupName))
        }
        
    interface IDependable<HostConfig> with
        member _.Add state resIds =
            { state with
                DependsOn = state.DependsOn + resIds
            }

let host = HostBuilder()
