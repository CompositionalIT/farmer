[<AutoOpen>]
module Farmer.Arm.ContainerService

open Farmer
open Farmer.CoreTypes
open Farmer.Vm

let managedClusters = ResourceType "Microsoft.ContainerService/managedClusters"

type AgentPoolMode = System | User

type ManagedCluster =
    { Name : ResourceName
      Location : Location
      AgentPoolProfiles :
        {| Name : ResourceName
           Count : int
           Mode : AgentPoolMode
           OsDiskSize : int<Gb>
           OsType : OS
           VmSize : VMSize
        |} list
      DnsPrefix : string
      EnableRBAC : bool
      LinuxProfile :
       {| AdminUserName : string
          PublicKeys : string list |} option
      WindowsProfile :
       {| AdminUserName : string
          AdminPassword : SecureParameter |} option
      ServicePrincipalProfile :
       {| ClientId : string
          ClientSecret : SecureParameter |} option
    }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = managedClusters.ArmValue
               apiVersion = "2020-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               properties =
                   {| agentPoolProfiles =
                       this.AgentPoolProfiles
                       |> List.mapi (fun idx agent ->
                           {| name = if agent.Name = ResourceName.Empty then (sprintf "%s-agent-pool%i" this.Name.Value idx) 
                                     else agent.Name.Value.ToLowerInvariant ()
                              count = agent.Count
                              osDiskSizeGB = agent.OsDiskSize
                              osType = string agent.OsType
                              vmSize = agent.VmSize.ArmValue
                              mode = agent.Mode |> string
                           |})
                      dnsPrefix = this.DnsPrefix
                      enableRBAC = this.EnableRBAC
                      linuxProfile =
                            match this.LinuxProfile with
                            | Some linuxProfile ->
                                {| adminUsername = linuxProfile.AdminUserName
                                   ssh = {| publicKeys = linuxProfile.PublicKeys |> List.map (fun k -> {| keyData = k |}) |} |}
                            | None -> Unchecked.defaultof<_>
                      servicePrincipalProfile =
                            match this.ServicePrincipalProfile with
                            | Some spProfile ->
                                {| clientId = spProfile.ClientId
                                   secret = spProfile.ClientSecret.AsArmRef.Eval() |}
                            | None -> Unchecked.defaultof<_>
                      windowsProfile =
                            match this.WindowsProfile with
                            | Some winProfile ->
                                {| adminUsername = winProfile.AdminUserName
                                   adminPassword = winProfile.AdminPassword.AsArmRef.Eval() |}
                            | None -> Unchecked.defaultof<_>
                   |}
            |} :> _
