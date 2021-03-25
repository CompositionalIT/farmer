[<AutoOpen>]
module Farmer.Arm.ContainerService

open Farmer
open Farmer.Identity
open Farmer.Vm

let managedClusters = ResourceType ("Microsoft.ContainerService/managedClusters", "2020-04-01")

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
      LinuxProfile :
       {| AdminUserName : string
          PublicKeys : string list |} option
      NetworkProfile :
       {| NetworkPlugin : ContainerService.NetworkPlugin
          DnsServiceIP : System.Net.IPAddress
          DockerBridgeCidr : IPAddressCidr
          ServiceCidr : IPAddressCidr |} option
      WindowsProfile :
        {| AdminUserName : string
           AdminPassword : SecureParameter |} option
      ServicePrincipalProfile :
        {| ClientId : string
           ClientSecret : SecureParameter |} option
    }

    interface IParameters with
        member this.SecureParameters = [
            yield! this.ServicePrincipalProfile |> Option.mapList(fun spp -> spp.ClientSecret)
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
                   identity = this.Identity |> ManagedIdentity.toArmJson
                   properties =
                       {| agentPoolProfiles =
                           this.AgentPoolProfiles
                           |> List.mapi (fun idx agent ->
                               {| name = if agent.Name = ResourceName.Empty then $"{this.Name.Value}-agent-pool{idx}"
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
                          linuxProfile =
                                match this.LinuxProfile with
                                | Some linuxProfile ->
                                    {| adminUsername = linuxProfile.AdminUserName
                                       ssh = {| publicKeys = linuxProfile.PublicKeys |> List.map (fun k -> {| keyData = k |}) |} |}
                                | None -> Unchecked.defaultof<_>
                          networkProfile =
                              match this.NetworkProfile with
                              | Some networkProfile ->
                                    {| dnsServiceIP = networkProfile.DnsServiceIP |> string
                                       dockerBridgeCidr = networkProfile.DockerBridgeCidr |> IPAddressCidr.format
                                       networkPlugin = networkProfile.NetworkPlugin.ArmValue
                                       serviceCidr = networkProfile.ServiceCidr |> IPAddressCidr.format |}
                              | None -> Unchecked.defaultof<_>
                          servicePrincipalProfile =
                                match this.ServicePrincipalProfile with
                                | Some spProfile ->
                                    {| clientId = spProfile.ClientId
                                       secret = spProfile.ClientSecret.ArmExpression.Eval() |}
                                | None -> Unchecked.defaultof<_>
                          windowsProfile =
                                match this.WindowsProfile with
                                | Some winProfile ->
                                    {| adminUsername = winProfile.AdminUserName
                                       adminPassword = winProfile.AdminPassword.ArmExpression.Eval() |}
                                | None -> Unchecked.defaultof<_>
                       |}
            |} :> _
