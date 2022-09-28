[<AutoOpen>]
module Farmer.Builders.HostGroups

open System
open Farmer
open Farmer.Arm
open Farmer.DedicatedHosts
    
type HostGroupConfig =
    {
        Name: ResourceName
        AvailabilityZones: string list option
        SupportAutomaticPlacement: FeatureFlag option
        UltraSSDEnabled: FeatureFlag option
        PlatformFaultDomainCount: int option
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
                    SupportAutomaticPlacement = this.SupportAutomaticPlacement |> (Option.defaultValue FeatureFlag.Disabled)
                    UltraSSDEnabled = this.UltraSSDEnabled |> (Option.defaultValue FeatureFlag.Disabled)
                    PlatformFaultDomainCount = this.PlatformFaultDomainCount |> (Option.defaultValue 0)
                    Tags = this.Tags
                }
            [ hostGroup ]

type HostGroupBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            AvailabilityZones = None
            SupportAutomaticPlacement = None
            UltraSSDEnabled = None
            PlatformFaultDomainCount = None
            Tags = Map.empty
        }
    [<CustomOperation "name">]
    member _.Name(state: HostGroupConfig, name: string) = { state with Name = ResourceName name }
    [<CustomOperation "add_availabilityZones">] // todo what do these even look like?
    member _.AddAvailabilityZone(state: HostGroupConfig, az: string list) =
        match state.AvailabilityZones with
        | None -> { state with AvailabilityZones = Some az }
        | Some curZones -> { state with AvailabilityZones = Some (az @ curZones) }
    [<CustomOperation "supportAutomaticPlacement">]
    member _.SupportAutomaticPlacement(state: HostGroupConfig, flag: FeatureFlag) =
        { state with SupportAutomaticPlacement = Some flag }
    [<CustomOperation "enableUltraSsd">]
    member _.EnableUltraSSD(state: HostGroupConfig, flag: FeatureFlag) =
        { state with UltraSSDEnabled = Some flag }
    [<CustomOperation "platformFaultDomainCount">]
    member _.PlatformFaultDomainCount(state: HostGroupConfig, domainCount: int) =
        { state with PlatformFaultDomainCount = Some domainCount }
        
let hostGroup = HostGroupBuilder()
    
type HostConfig =
    {
        Name: ResourceName
        AutoReplaceOnFailure: FeatureFlag option
        LicenseType: HostLicenseType option
        HostSku: HostSku option
        PlatformFaultDomain: int option
        HostGroupName: LinkedResource
        PublicKey: Uri option
        Tags: Map<string, string>
    }
     
     interface IBuilder with
         member this.ResourceId = hosts.resourceId this.Name

         member this.BuildResources location =
            match this.HostGroupName.Name.Value with
            | "" -> raiseFarmer "Hosts must have a linked host group"
            | _ -> 
                 let host: Compute.Host =
                     { Name = this.Name
                       Location = location 
                       Sku = this.HostSku
                                |> Option.defaultValue
                                       { Capacity = 1
                                         Name = "VSv1-Type3"
                                         Tier = HostTier.Standard
                                        }
                       ParentHostGroupName = this.HostGroupName.ResourceId.Name
                       AutoReplaceOnFailure = FeatureFlag.Enabled
                       LicenseType = HostLicenseType.NoLicense
                       PlatformFaultDomain = 0
                       PublicKey = this.PublicKey |> Option.map string
                       Tags = this.Tags }
                 
                 [ host ]

type HostBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            AutoReplaceOnFailure = None
            LicenseType = None
            HostSku = None
            PlatformFaultDomain = None
            HostGroupName = Managed (ResourceId.create(hostGroups, ResourceName.Empty))
            PublicKey = None
            Tags = Map.empty
        }
        
    [<CustomOperation "name">]
    member _.Name(state: HostConfig, name: string) = { state with Name = ResourceName name }
    
    [<CustomOperation "autoReplaceOnFailure">]
    member _.AutoReplaceOnFailure(state: HostConfig, flag: FeatureFlag) = { state with AutoReplaceOnFailure = Some flag }
    [<CustomOperation "licenseType">]
    member _.LicenseType(state: HostConfig, licenseType: HostLicenseType) = { state with LicenseType = Some licenseType }
    
    [<CustomOperation "sku">]
    member _.Sku(state: HostConfig, s: HostSku) = { state with HostSku = Some s }
    [<CustomOperation "sku">]
    member _.Sku(state: HostConfig, skuName: string) =
        { state with
            HostSku = Some { Capacity = 1; Name = skuName; Tier = HostTier.Standard }
        }
    [<CustomOperation "platformFaultDomain">]
    member _.PlatformFaultDomain(state: HostConfig, faultDomains: int) = { state with PlatformFaultDomain = Some faultDomains }
    [<CustomOperation "parentHostGroup">]
    member _.ParentHostGroup(state: HostConfig, hostGroup: LinkedResource) = { state with HostGroupName = hostGroup }
    [<CustomOperation "parentHostGroup">]
    member _.ParentHostGroup(state: HostConfig, hostGroupName: ResourceName) = { state with HostGroupName =  Unmanaged (ResourceId.create(hostGroups, hostGroupName)) }
    [<CustomOperation "publicKey">]
    member _.publicKey(state: HostConfig, keyUri: Uri) = { state with PublicKey = Some keyUri }

let host = HostBuilder()