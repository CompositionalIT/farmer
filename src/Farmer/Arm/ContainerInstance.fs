[<AutoOpen>]
module Farmer.Arm.ContainerInstance

open Farmer

type ContainerGroupIpAddress =
    { Type : ContainerGroupIpAddressType
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
      RestartPolicy : ContainerGroupRestartPolicy
      IpAddress : ContainerGroupIpAddress }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.ContainerInstance/containerGroups"
               apiVersion = "2018-10-01"
               name = this.Name.Value
               location = this.Location.ArmValue
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
                                          memoryInGb = container.Memory |}
                                   |}
                               |}
                           |})
                      osType = this.OsType
                      restartPolicy = this.RestartPolicy.ToString().ToLower()
                      ipAddress =
                        {| Type =
                            match this.IpAddress.Type with
                            | PublicAddress -> "Public"
                            | PrivateAddress -> "Private"
                           Ports = [
                               for port in this.IpAddress.Ports do
                                {| Protocol = string port.Protocol
                                   Port = port.Port |}
                           ]
                        |}
                   |}
            |} :> _