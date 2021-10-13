[<AutoOpen>]
module Farmer.Builders.ApplicationGateway

open Farmer
open Farmer.Arm.Network
open Farmer.PublicIpAddress
open Farmer.Arm.ApplicationGateway
open Farmer.ApplicationGateway

// Desired Properties: 
// Zones
//X *Skus
//X *IP config
//X *Frontend IP config
// *SSL certificates
//X *Frontend ports
// Autoscale
// *Probes
// *Backend address pools
// *Backend HTTP settings
// *Http listeners
// *Request routing rules
// Web application firewall configuration
// Diagnostics settings
// Project



type GatewayIpConfig = 
    {
        Name: ResourceName
        Subnet: LinkedResource option
    }
    static member BuildResource gatewayIp =
        {|
            Name = gatewayIp.Name
            Subnet =
                gatewayIp.Subnet
                |> Option.map (function | Managed resId -> resId | Unmanaged resId -> resId)
        |}

type GatewayIpBuilder() = 
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Subnet = None
        }
    [<CustomOperation "name">]
    member _.Name(state:GatewayIpConfig, name) =
        { state with Name = ResourceName name }
    [<CustomOperation "link_to_subnet">]
    member _.LinktoSubnet(state:GatewayIpConfig, subnet) =
        { state with Subnet = Some (Unmanaged subnet) }

let gatewayIp = GatewayIpBuilder()

// Subtle differences between load balancers
type FrontendIpConfig =
    {
        Name: ResourceName
        PrivateIpAllocationMethod: PrivateIpAddress.AllocationMethod
        PublicIp: LinkedResource option
    }
    static member BuildResource frontend =
        {|
            Name = frontend.Name
            PrivateIpAllocationMethod = frontend.PrivateIpAllocationMethod
            PublicIp =
                frontend.PublicIp
                |> Option.map (function | Managed resId -> resId | Unmanaged resId -> resId)
        |}
    static member BuildIp (frontend:FrontendIpConfig) (agwSku:ApplicationGateway.Sku) (location:Location) : PublicIpAddress option =
        match frontend.PublicIp with
        | Some (Managed resId) ->
            {
                Name = resId.Name
                AllocationMethod = AllocationMethod.Static
                Location = location
                Sku = PublicIpAddress.Sku.Standard
                    // TODO how to match this? App Gateway SKUs are different from load balancer
                    // Azure Portal only allows Standard
                    // match agwSku with
                    // | Farmer.ApplicationGateway.Sku.Basic ->
                    //     PublicIpAddress.Sku.Basic
                    // | Farmer.ApplicationGateway.Sku.Standard ->
                    //     PublicIpAddress.Sku.Standard
                DomainNameLabel = None
                Tags = Map.empty
            } |> Some
        | _ -> None

type FrontendIpBuilder () =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            PrivateIpAllocationMethod = PrivateIpAddress.DynamicPrivateIp
            PublicIp = None
        }
    /// Sets the name of the frontend IP configuration.
    [<CustomOperation "name">]
    member _.Name(state:FrontendIpConfig, name) = { state with Name = ResourceName name }
    /// Sets the frontend's private IP allocation method.
    [<CustomOperation "private_ip_allocation_method">]
    member _.PrivateIpAllocationMethod(state:FrontendIpConfig, allocationMethod) =
        { state with PrivateIpAllocationMethod = allocationMethod  }
    /// Sets the name of the frontend public IP.
    [<CustomOperation "public_ip">]
    member _.PublicIp(state:FrontendIpConfig, publicIp) = { state with PublicIp = Some (Managed (Farmer.Arm.Network.publicIPAddresses.resourceId (ResourceName publicIp))) }
    /// Links the frontend to an existing public IP.
    [<CustomOperation "link_to_public_ip">]
    member _.LinkToPublicIp(state:FrontendIpConfig, publicIp) = { state with PublicIp = Some (Unmanaged publicIp) }

let frontend = FrontendIpBuilder()

type FrontendPortConfig = 
    {
        Name: ResourceName
        Port: uint16
    }
    static member BuildResource frontendPort =
        {|
            Name = frontendPort.Name
            Port = frontendPort.Port
        |}

type FrontendPortBuilder = 
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Port = uint16 80
        }
    [<CustomOperation "name">]
    member _.Name(state:FrontendPortConfig, name) =
        { state with Name = ResourceName name }
    [<CustomOperation "port">]
    member _.Port(state:FrontendPortConfig, port) =
        { state with Port = port }

type AppGatewayConfig =
    { Name : ResourceName
      Sku: ApplicationGatewaySku
      GatewayIpConfigs: GatewayIpConfig list
      FrontendIpConfigs: FrontendIpConfig list
      FrontendPorts: FrontendPortConfig list
     }
    // // TODO - Still missing properties for the below
    // interface IBuilder with
    //     member this.ResourceId = ApplicationGateways.resourceId this.Name
    //     member this.BuildResources location =
    //         let frontendPublicIps =
    //             this.FrontendIpConfigs
    //             |> List.map (fun frontend -> FrontendIpConfig.BuildIp frontend this.Name.Value this.Sku.Name location)
    //             |> List.choose id
    //         {
    //             Name = this.Name
    //             Location = location
    //             Sku = this.Sku
    //             GatewayIPConfigurations = this.GatewayIpConfigs |> List.map GatewayIpConfigs.BuildResource
    //             FrontendIpConfigs = this.FrontendIpConfigs |> List.map FrontendIpConfig.BuildResource
    //             FrontendPorts = this.FrontendPorts |> List.Map FrontendPortConfig.BuildResource
    //             Dependencies =
    //                 frontendPublicIps
    //                 |> List.map (fun pip -> publicIPAddresses.resourceId pip.Name)
    //                 |> Set.ofList
    //                 |> Set.union this.Dependencies
    //             Tags = this.Tags
    //         } :> IArmResource
    //         :: (frontendPublicIps |> Seq.cast<IArmResource> |> List.ofSeq)
            

type AppGatewayBuilder() =
    member _.Yield _ : AppGatewayConfig = {
        Name = ResourceName.Empty
        Sku = {
            Name = Sku.Standard_v2
            Capacity = 1 // TODO - what value?
            Tier = Tier.Standard_v2
        }
        GatewayIpConfigs = []
        FrontendIpConfigs = []
        FrontendPorts = []
    }
    [<CustomOperation "name">]
    member _.Name (state:AppGatewayConfig, name) =
        { state with Name = ResourceName name }
    [<CustomOperation "sku">]
    member _.Sku (state:AppGatewayConfig, skuName) = 
        { state with Sku = { state.Sku with Name = skuName}}
    [<CustomOperation "tier">]
    member _.Tier(state:AppGatewayConfig, skuTier) = 
        { state with Sku = { state.Sku with Tier = skuTier } }
    [<CustomOperation "add_ip_configs">]
    member _.AddIpConfigs (state:AppGatewayConfig, ipConfigs) =
        { state with GatewayIpConfigs = state.GatewayIpConfigs @ ipConfigs }
    [<CustomOperation "add_frontends">]
    member _.AddFrontends (state:AppGatewayConfig, frontends) =
        { state with FrontendIpConfigs = state.FrontendIpConfigs @ frontends }
    [<CustomOperation "add_frontend_ports">]
    member _.AddFrontendPorts (state:AppGatewayConfig, frontendPorts) =
        { state with FrontendPorts = state.FrontendPorts @ frontendPorts}

let appGateway = AppGatewayBuilder()
