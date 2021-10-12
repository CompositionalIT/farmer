[<AutoOpen>]
module Farmer.Builders.ApplicationGateway

open Farmer
open Farmer.Arm.Network
open Farmer.PublicIpAddress
// open Farmer.Arm.ApplicationGateway
// open Farmer.Arm.Network
// open Farmer.PublicIpAddress

// Location?

// Zones
// *Skus
//X *IP config
//X *Frontend IP config
// *SSL certificates
// *Frontend ports
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
    static member BuildIp (frontend:FrontendIpConfig) (lbName:string) (lbSku:LoadBalancer.Sku) (location:Location) : PublicIpAddress option =
        match frontend.PublicIp with
        | Some (Managed resId) ->
            {
                Name = resId.Name
                AllocationMethod = AllocationMethod.Static
                Location = location
                Sku =
                    match lbSku with
                    | Farmer.LoadBalancer.Sku.Basic ->
                        PublicIpAddress.Sku.Basic
                    | Farmer.LoadBalancer.Sku.Standard ->
                        PublicIpAddress.Sku.Standard
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

type AppGatewayConfig =
    { Name : ResourceName
    //   Sku: AppGatewaySku
      IpConfigs: GatewayIpConfig list
      FrontendIpConfigs: FrontendIpConfig list
     }
    // interface IBuilder with
    //     member this.ResourceId = appGateways.resourceId this.Name
    //     member this.BuildResources location =
            
    //         let frontendPublicIps =
    //             this.FrontendIpConfigs
    //             |> List.map (fun frontend -> FrontendIpConfig.BuildIp frontend this.Name.Value this.Sku.Name location)
    //             |> List.choose id
            
    //         {
    //             Name = this.Name
    //             Location = location
    //             // Sku = this.Sku
    //             IpConfigs = this.IpConfigs |> List.map IpConfigs.BuildResource
    //             FrontendIpConfigs = this.FrontendIpConfigs |> List.map FrontendIpConfig.BuildResource
    //             Dependencies =
    //                 frontendPublicIps
    //                 |> List.map (fun pip -> publicIPAddresses.resourceId pip.Name)
    //                 |> Set.ofList
    //                 |> Set.union this.Dependencies
    //             Tags = this.Tags
    //         } :> IArmResource
    //         @ (frontendPublicIps |> Seq.cast<IArmResource> |> List.ofSeq)
            

type AppGatewayBuilder() =
    member _.Yield _ : AppGatewayConfig = {
        Name = ResourceName.Empty
        // Sku = {
        //     Name = AppGateway.Sku.Basic
        //     Tier = AppGateway.Tier.Regional // TODO where is this defined? (in ApplicationGateway.fs)
        // }
        IpConfigs = []
        FrontendIpConfigs = []
    }
    [<CustomOperation "name">]
    member _.Name (state:AppGatewayConfig, name) =
        { state with Name = ResourceName name }
    // [<CustomOperation "sku">]
    // member _.Sku (state:AppGatewayConfig, skuName) = 
    //     { state with Sku = { state.Sku with Name = skuName}}
    // TODO sku tier
    [<CustomOperation "add_ip_configs">]
    member _.AddIpConfigs (state:AppGatewayConfig, ipConfigs) =
        { state with IpConfigs = state.IpConfigs @ ipConfigs }
    [<CustomOperation "add_frontends">]
    member _.AddFrontends (state:AppGatewayConfig, frontends) =
        { state with FrontendIpConfigs = state.FrontendIpConfigs @ frontends }

let appGateway = AppGatewayBuilder()
