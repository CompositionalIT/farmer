[<AutoOpen>]
module Farmer.Arm.ContainerService

open Farmer
open Farmer.CoreTypes
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
        member this.SecureParameters =
            [
                match this.ServicePrincipalProfile with
                | Some servicePrincipalProfile -> servicePrincipalProfile.ClientSecret
                | None -> ()
                match this.WindowsProfile with
                | Some windowsProfile -> windowsProfile.AdminPassword
                | None -> ()
            ]
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            let dependencies =
               this.AgentPoolProfiles
               |> List.map (fun pool -> pool.VirtualNetworkName)
               |> List.choose id
               |> List.map(fun vnet -> ResourceId.create(virtualNetworks, vnet))
            {| managedClusters.Create(this.Name, this.Location, dependencies) with
                   properties =
                       {| agentPoolProfiles =
                           this.AgentPoolProfiles
                           |> List.mapi (fun idx agent ->
                               {| name = if agent.Name = ResourceName.Empty then (sprintf "%s-agent-pool%i" this.Name.Value idx)
                                         else agent.Name.Value.ToLowerInvariant ()
                                  count = agent.Count
                                  maxPods = agent.MaxPods |> Option.toNullable
                                  mode = agent.Mode |> string
                                  osDiskSizeGB = agent.OsDiskSize
                                  osType = string agent.OsType
                                  vmSize = agent.VmSize.ArmValue
                                  vnetSubnetID =
                                      match agent.VirtualNetworkName, agent.SubnetName with
                                      | Some vnet, Some subnet -> ResourceId.create(subnets, vnet, subnet).Eval()
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
