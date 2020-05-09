[<AutoOpen>]
module Farmer.Arm.ContainerInstance

open Farmer

type ContainerGroup =
    { Name : ResourceName
      ContainerInstances :
        {| Name : ResourceName
           Image : string
           Ports : uint16 list
           Cpu : int
           Memory : float |} list
      OsType : string
      RestartPolicy : string
      IpAddress : {| Type : string; Ports : {| Protocol : string; Port : uint16 |} list |} }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.ToArmObject location =
            {| ``type`` = "Microsoft.ContainerInstance/containerGroups"
               apiVersion = "2018-10-01"
               name = this.Name.Value
               location = location.ArmValue
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
                      restartPolicy = this.RestartPolicy
                      ipAddress = this.IpAddress
                   |}
            |} :> _
