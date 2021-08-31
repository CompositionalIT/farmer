[<AutoOpen>]
module Farmer.Arm.ContainerService

open Farmer
open Farmer.Identity
open Farmer.Vm

let managedClusters = ResourceType ("Microsoft.ContainerService/managedClusters", "2021-03-01")

type AgentPoolMode = System | User

/// Additional identity settings for the managed cluster, such as the identity for kubelet to pull container images.
type ManagedClusterIdentityProfile =
    { KubeletIdentity : ResourceId option }
    member internal this.ToArmJson =
        {| kubeletIdentity =
            match this.KubeletIdentity with
            | Some kubeletIdentity ->
                {| resourceId = kubeletIdentity.Eval()
                   clientId = ArmExpression.reference(kubeletIdentity.Type, kubeletIdentity).Map(fun r -> r + ".clientId").Eval()
                   objectId = ArmExpression.reference(kubeletIdentity.Type, kubeletIdentity).Map(fun r -> r + ".principalId").Eval()
                |}
            | None -> Unchecked.defaultof<_>
        |}
    member internal this.Dependencies = [ this.KubeletIdentity ] |> List.choose id

type ManagedCluster =
    { Name : ResourceName
      Location : Location
      Dependencies : ResourceId Set
      /// Dependencies that are expressed in ARM functions instead of a resource Id
      DependencyExpressions : ArmExpression Set
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
      IdentityProfile : ManagedClusterIdentityProfile option
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
            let dependencies =
                [
                    this.AgentPoolProfiles
                    |> List.choose (fun pool -> pool.VirtualNetworkName)
                    |> List.map virtualNetworks.resourceId
                    this.Identity.Dependencies
                    this.IdentityProfile
                         |> Option.map (fun identityProfile -> identityProfile.Dependencies)
                         |> Option.defaultValue []
                ] |> Seq.concat |> Set.ofSeq |> Set.union this.Dependencies
            {| managedClusters.Create(this.Name, this.Location) with
                   dependsOn = [ 
                       dependencies |> Seq.map (fun r -> r.Eval())
                       this.DependencyExpressions |> Seq.map (fun r -> r.Eval())
                       ] |> Seq.concat
                   identity = // If using MSI but no identity was set, then enable the system identity like the CLI
                       if this.ServicePrincipalProfile.ClientId = "msi"
                          && this.Identity.SystemAssigned = FeatureFlag.Disabled
                          && this.Identity.UserAssigned.Length = 0 then
                           { SystemAssigned = Enabled; UserAssigned = [] }.ToArmJson
                       else
                           this.Identity.ToArmJson
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
                          identityProfile =
                              match this.IdentityProfile with
                              | Some identityProfile -> identityProfile.ToArmJson
                              | None -> Unchecked.defaultof<_>
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
