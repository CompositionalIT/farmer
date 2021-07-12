[<AutoOpen>]
module Farmer.Builders.AzureFirewall

open Farmer
open Farmer.AzureFirewall
open Farmer.Arm.AzureFirewall
open Farmer.Arm.VirtualHub

type HubIPAddressSpace =
    | PublicCount of int
    member this.Arm =
        match this with
        | PublicCount count ->
            { PublicIPAddresses =
                { Count = count
                  Addresses = [] } |> Some }
        
type AzureFirewallConfig =
    { Name : ResourceName
      FirewallPolicy : LinkedResource option
      VirtualHub : LinkedResource option
      HubIPAddressSpace : HubIPAddressSpace option
      Sku : Sku }
    interface IBuilder with
        member this.ResourceId = azureFirewalls.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Dependencies = [
                match this.FirewallPolicy with
                | Some (Managed resId) -> resId // Only generate dependency if this is managed by Farmer (same template)
                | _ -> ()
                match this.VirtualHub with
                | Some (Managed resId) -> resId
                | _ -> ()
              ] |> Set.ofList
              FirewallPolicy = this.FirewallPolicy |> Option.map (fun x -> match x with | Managed resId | Unmanaged resId -> resId)
              VirtualHub = this.VirtualHub |> Option.map (fun x -> match x with | Managed resId | Unmanaged resId -> resId)
              HubIPAddresses = this.HubIPAddressSpace |> Option.map (fun x -> x.Arm)
              Sku = this.Sku }
            ]

type AzureFirewallBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = {Name = SkuName.AZFW_Hub; Tier = SkuTier.Standard}
          FirewallPolicy = None
          VirtualHub = None
          HubIPAddressSpace = None }
    /// The name of the firewall.
    [<CustomOperation "name">] member _.Name(state:AzureFirewallConfig, name) = { state with Name = ResourceName name }
    /// The SKU of the firewall.
    [<CustomOperation "sku">]
    member _.Sku(state:AzureFirewallConfig, name, tier) = { state with Sku = {Name = name; Tier = tier} }

    /// Configure this firewall to use an unmanaged firewall policy.
    [<CustomOperation "link_to_unmanaged_firewall_policy">]
    member this.LinkToUnmanagedFirewallPolicy (state:AzureFirewallConfig, resourceId) =
        { state with FirewallPolicy = Some (Unmanaged resourceId) }
    /// Configure this firewall to use a managed firewall policy.
    [<CustomOperation "link_to_firewall_policy">]
    member this.LinkToFirewallPolicy (state:AzureFirewallConfig, firewallPolicy:IArmResource) =
        { state with FirewallPolicy = Some (Managed firewallPolicy.ResourceId) }
    /// The unmanaged virtualHub to which the firewall belongs. 
    [<CustomOperation "link_to_unmanaged_vhub">]
    member this.LinkToUnmanagedVirtualHub (state:AzureFirewallConfig, resourceId) =
        { state with VirtualHub = Some (Unmanaged resourceId) }
    /// The managed virtualHub to which the firewall belongs
    [<CustomOperation "link_to_vhub">]
    member this.LinkToVirtualHub (state:AzureFirewallConfig, vhub:IArmResource) =    
        { state with FirewallPolicy = Some (Managed vhub.ResourceId) }
    /// Configure this firewall to reserve a specified number of public ips.
    /// 0 is not a valid value for AZFW_HUB 
    [<CustomOperation "public_ip_reservation_count">]
    member _.PublicIpReservationCount(state:AzureFirewallConfig, count) =
        { state with HubIPAddressSpace = Some (HubIPAddressSpace.PublicCount count) }
    member _.Run(state:AzureFirewallConfig) =
        let stateIBuilder = state :> IBuilder
        match state.Sku.Name with
        | AZFW_Hub ->
            match state.HubIPAddressSpace with
            | None -> failwith $"Sku AZFW_Hub requires Public IPs provided for Azure Firewall {stateIBuilder.ResourceId}. Please specify valid IPs count (count cannot be zero) and/or Public IPs to be retained (in case of deleting IPs). Some Public IPs specified may be incorrect, please specify the IPs that are linked to the firewall"
            | Some (PublicCount 0) -> failwith $"Sku AZFW_Hub requires Public IPs count be > 0 for Azure Firewall {stateIBuilder.ResourceId}"
            | Some (PublicCount _) -> ()
        | AZFW_VNet -> ()
        state
       
        
let azureFirewall = AzureFirewallBuilder()