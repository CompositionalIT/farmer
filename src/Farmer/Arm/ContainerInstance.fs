[<AutoOpen>]
module Farmer.Arm.ContainerInstance

open Farmer
open Farmer.ContainerGroup

type ContainerGroupIpAddress =
    { Type : IpAddressType
      Ports :
        {| Protocol : TransmissionProtocol
           Port : uint16 |} Set }

type ContainerGroup =
    { Name : ResourceName
      Location : Location
      ContainerInstances :
        {| Name : ResourceName
           Image : string
           Ports : uint16 Set
           Cpu : int
           Memory : float<Gb> |} list
      OperatingSystem : OS
      RestartPolicy : RestartPolicy
      IpAddress : ContainerGroupIpAddress
      NetworkProfile : ResourceName option }
    member this.NetworkProfilePath =
        this.NetworkProfile
        |> Option.map (fun networkProfile -> sprintf "[resourceId('Microsoft.Network/networkProfiles','%s')]" networkProfile.Value)

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.ContainerInstance/containerGroups"
               apiVersion = "2018-10-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = this.NetworkProfilePath |> Option.toList
               properties =
                   {| containers =
                       this.ContainerInstances
                       |> List.map (fun container ->
                           {| name = container.Name.Value.ToLowerInvariant ()
                              properties =
                               {| image = container.Image
                                  ports = container.Ports |> Set.map (fun port -> {| port = port |})
                                  resources =
                                   {| requests =
                                       {| cpu = container.Cpu
                                          memoryInGB = container.Memory |}
                                   |}
                               |}
                           |})
                      osType = string this.OperatingSystem
                      restartPolicy =
                        match this.RestartPolicy with
                        | AlwaysRestart -> "Always"
                        | NeverRestart -> "Never"
                        | RestartOnFailure -> "OnFailure"
                      ipAddress =
                        {| ``type`` =
                            match this.IpAddress.Type with
                            | PublicAddress | PublicAddressWithDns _ -> "Public"
                            | PrivateAddress _ | PrivateAddressWithIp _ -> "Private"
                           ports = [
                               for port in this.IpAddress.Ports do
                                {| protocol = string port.Protocol
                                   port = port.Port |}
                           ]
                           ip =
                            match this.IpAddress.Type with
                            | PrivateAddressWithIp ip -> string ip
                            | _ -> null
                           dnsNameLabel =
                            match this.IpAddress.Type with
                            | PublicAddressWithDns dnsLabel -> dnsLabel
                            | _ -> null
                        |}
                      networkProfile =
                        this.NetworkProfilePath
                        |> Option.map(fun path -> box {| id = path |})
                        |> Option.toObj
                   |}
            |} :> _