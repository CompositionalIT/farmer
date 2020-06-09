[<AutoOpen>]
module Farmer.Arm.ContainerInstance

open Farmer
open Farmer.CoreTypes
open Farmer.ContainerGroup

type ContainerGroupIpAddress =
    { Type : IpAddressType
      Ports :
        {| Protocol : TransmissionProtocol
           Port : uint16 |} list
    }

type ContainerGroup =
    { Name : ResourceName
      Location : Location
      ContainerInstances :
        {| Name : ResourceName
           Image : string
           Ports : uint16 list
           Cpu : int
           Memory : float<Gb> |} list
      OsType : string
      RestartPolicy : RestartPolicy
      IpAddress : ContainerGroupIpAddress
      NetworkProfile : ResourceName option }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.ContainerInstance/containerGroups"
               apiVersion = "2018-10-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn =
                   match this.NetworkProfile with
                   | None -> []
                   | Some networkProfile -> [ sprintf "[resourceId('Microsoft.Network/networkProfiles','%s')]" networkProfile.Value ]
               properties =
                   {| containers =
                       this.ContainerInstances
                       |> List.map (fun container ->
                           {| name = container.Name.Value.ToLowerInvariant ()
                              properties =
                               {| image = container.Image
                                  ports = container.Ports |> List.map (fun port -> {| port = port |})
                                  resources =
                                   {| requests =
                                       {| cpu = container.Cpu
                                          memoryInGB = container.Memory |}
                                   |}
                               |}
                           |})
                      osType = this.OsType
                      restartPolicy = this.RestartPolicy.ToString()
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
                          match this.NetworkProfile with
                          | Some networkProfile -> {| id = sprintf "[resourceId('Microsoft.Network/networkProfiles','%s')]" networkProfile.Value |}
                          | None -> Unchecked.defaultof<_>
                   |}
            |} :> _