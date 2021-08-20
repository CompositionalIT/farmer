[<AutoOpen>]
module Farmer.Arm.ContainerService

open Farmer
open Farmer.Identity
open Farmer.Vm

let managedClusters = ResourceType ("Microsoft.ContainerService/managedClusters", "2021-03-01")

type AgentPoolMode = System | User

type ManagedCluster =
    { Name : ResourceName
      Location : Location
      AgentPoolProfiles :
        {| Name : ResourceName
           Count : int
           MaxPods : int option
           Mode : AgentPoolMode
           OsDiskSize : int<Gb>
           OsType : OS
           VmSize : VMSize
           VirtualNetworkName : ResourceName option
           SubnetName : ResourceName option
        |} list
      DnsPrefix : string
      EnableRBAC : bool
      Identity : ManagedIdentity
      ApiServerAccessProfile :
       {| AuthorizedIPRanges : string list
          EnablePrivateCluster : bool option |} option
      LinuxProfile :
       {| AdminUserName : string
          PublicKeys : string list |} option
      NetworkProfile :
       {| NetworkPlugin : ContainerService.NetworkPlugin option
          DnsServiceIP : System.Net.IPAddress option
          DockerBridgeCidr : IPAddressCidr option
          LoadBalancerSku : LoadBalancer.Sku option
          ServiceCidr : IPAddressCidr option |} option
      WindowsProfile :
        {| AdminUserName : string
           AdminPassword : SecureParameter |} option
      ServicePrincipalProfile :
        {| ClientId : string
           ClientSecret : SecureParameter option |}
    }

    interface IParameters with
        member this.SecureParameters = [
            yield! this.ServicePrincipalProfile.ClientSecret |> Option.mapList id
            yield! this.WindowsProfile |> Option.mapList (fun wp -> wp.AdminPassword)
        ]
    interface IArmResource with
        member this.ResourceId = managedClusters.resourceId this.Name
        member this.JsonModel =
            let dependencies = [
                yield!
                    this.AgentPoolProfiles
                    |> List.choose (fun pool -> pool.VirtualNetworkName)
                    |> List.map virtualNetworks.resourceId
                yield! this.Identity.Dependencies
            ]
            {| managedClusters.Create(this.Name, this.Location, dependencies) with
                   identity = this.Identity.ToArmJson
                   properties =
                       {| agentPoolProfiles =
                           this.AgentPoolProfiles
                           |> List.mapi (fun idx agent ->
                               {| name = if agent.Name = ResourceName.Empty then $"nodepool{idx + 1}"
                                         else agent.Name.Value.ToLowerInvariant ()
                                  count = agent.Count
                                  maxPods = agent.MaxPods |> Option.toNullable
                                  mode = agent.Mode |> string
                                  osDiskSizeGB = agent.OsDiskSize
                                  osType = string agent.OsType
                                  vmSize = agent.VmSize.ArmValue
                                  vnetSubnetID =
                                      match agent.VirtualNetworkName, agent.SubnetName with
                                      | Some vnet, Some subnet -> subnets.resourceId(vnet, subnet).Eval()
                                      | _ -> null
                               |})
                          dnsPrefix = this.DnsPrefix
                          enableRBAC = this.EnableRBAC
                          apiServerAccessProfile =
                              match this.ApiServerAccessProfile with
                              | Some apiServerProfile ->
                                  {| authorizedIPRanges = apiServerProfile.AuthorizedIPRanges
                                     enablePrivateCluster =
                                        apiServerProfile.EnablePrivateCluster
                                        |> Option.map box |> Option.toObj |}
                              | None -> Unchecked.defaultof<_>
                          linuxProfile =
                                match this.LinuxProfile with
                                | Some linuxProfile ->
                                    {| adminUsername = linuxProfile.AdminUserName
                                       ssh = {| publicKeys = linuxProfile.PublicKeys |> List.map (fun k -> {| keyData = k |}) |} |}
                                | None -> Unchecked.defaultof<_>
                          networkProfile =
                              match this.NetworkProfile with
                              | Some networkProfile ->
                                    {| dnsServiceIP = networkProfile.DnsServiceIP |> Option.map string |> Option.toObj
                                       dockerBridgeCidr = networkProfile.DockerBridgeCidr |> Option.map IPAddressCidr.format |> Option.toObj
                                       loadBalancerSku = networkProfile.LoadBalancerSku |> Option.map (fun sku -> sku.ArmValue) |> Option.toObj
                                       networkPlugin = networkProfile.NetworkPlugin |> Option.map (fun plugin -> plugin.ArmValue) |> Option.toObj
                                       serviceCidr = networkProfile.ServiceCidr |> Option.map IPAddressCidr.format |> Option.toObj |}
                              | None -> Unchecked.defaultof<_>
                          servicePrincipalProfile =
                              {| clientId = this.ServicePrincipalProfile.ClientId
                                 secret =
                                    this.ServicePrincipalProfile.ClientSecret
                                    |> Option.map (fun clientSecret -> clientSecret.ArmExpression.Eval())
                                    |> Option.toObj |}
                          windowsProfile =
                                match this.WindowsProfile with
                                | Some winProfile ->
                                    {| adminUsername = winProfile.AdminUserName
                                       adminPassword = winProfile.AdminPassword.ArmExpression.Eval() |}
                                | None -> Unchecked.defaultof<_>
                       |}
            |} :> _
