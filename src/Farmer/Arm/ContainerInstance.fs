[<AutoOpen>]
module Farmer.Arm.ContainerInstance

open Farmer
open Farmer.ContainerGroup
open Farmer.CoreTypes
open Newtonsoft.Json.Linq

let containerGroups = ResourceType "Microsoft.ContainerInstance/containerGroups"

type ContainerGroupIpAddress =
    { Type : IpAddressType
      Ports :
        {| Protocol : TransmissionProtocol
           Port : uint16 |} Set }

type ContainerGroup =
    { Name : ResourceName<ContainerGroupName>
      Location : Location
      ContainerInstances :
        {| Name : ResourceName<ContainerInstanceName>
           Image : string
           Ports : uint16 Set
           Cpu : int
           Memory : float<Gb>
           EnvironmentVariables: Map<string, {| Value:string; Secure:bool |}>
           VolumeMounts : Map<string,string>
        |} list
      OperatingSystem : OS
      RestartPolicy : RestartPolicy
      IpAddress : ContainerGroupIpAddress
      NetworkProfile : ResourceName<NetworkProfileName> option
      Volumes : Map<string, {| Volume:Volume |}>
      Tags: Map<string,string>  }
    member this.NetworkProfilePath =
        this.NetworkProfile
        |> Option.map (fun networkProfile -> ArmExpression.resourceId(Network.networkProfiles, networkProfile).Eval())
    member private this.Dependencies = seq {
        match this.NetworkProfilePath with
        | Some networkProfilePath -> networkProfilePath
        | None -> ()

        let fileShares = [
            for _, v in Map.toSeq this.Volumes do
                match v.Volume with
                | Volume.AzureFileShare (shareName, storageAccountName) -> shareName, storageAccountName
                | _ -> () ]
        for shareName, storageAccountName in fileShares do
            let fullShareName = [| storageAccountName.Untyped; ResourceName "default"; shareName.Untyped |]
            ArmExpression.resourceId(Storage.fileShares, fullShareName).Eval()
    }

    interface IArmResource with
        member this.ResourceName = this.Name.Untyped
        member this.JsonModel =
            {| ``type`` = containerGroups.ArmValue
               apiVersion = "2018-10-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = this.Dependencies
               properties =
                   {| containers =
                       this.ContainerInstances
                       |> List.map (fun container ->
                           {| name = container.Name.Value.ToLowerInvariant ()
                              properties =
                               {| image = container.Image
                                  ports = container.Ports |> Set.map (fun port -> {| port = port |})
                                  environmentVariables =
                                      container.EnvironmentVariables
                                      |> Seq.map (fun kvp ->
                                          if kvp.Value.Secure then
                                              {| name = kvp.Key; value=null; secureValue=kvp.Value.Value |}
                                          else
                                              {| name = kvp.Key; value=kvp.Value.Value; secureValue=null |})
                                  resources =
                                   {| requests =
                                       {| cpu = container.Cpu
                                          memoryInGB = container.Memory |}
                                   |}
                                  volumeMounts = container.VolumeMounts |> Seq.map (fun kvp -> {| name=kvp.Key; mountPath=kvp.Value |}) |> List.ofSeq
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
                            | PrivateAddress _ -> "Private"
                           ports = [
                               for port in this.IpAddress.Ports do
                                {| protocol = string port.Protocol
                                   port = port.Port |}
                           ]
                           dnsNameLabel =
                            match this.IpAddress.Type with
                            | PublicAddressWithDns dnsLabel -> dnsLabel
                            | _ -> null
                        |}
                      networkProfile =
                        this.NetworkProfilePath
                        |> Option.map(fun path -> box {| id = path |})
                        |> Option.toObj
                      volumes = this.Volumes |> Seq.map (fun volume ->
                          match volume.Key, volume.Value.Volume with
                          |  volumeName, Volume.AzureFileShare (shareName, accountName) ->
                              {| name = volumeName
                                 azureFile =
                                     {| shareName = shareName.Value
                                        storageAccountName = accountName.Value
                                        storageAccountKey = sprintf "[listKeys('Microsoft.Storage/storageAccounts/%s', '2018-07-01').keys[0].value]" accountName.Value |}
                                 emptyDir = null
                                 gitRepo = Unchecked.defaultof<_>
                                 secret = Unchecked.defaultof<_> |}
                          |  volumeName, Volume.EmptyDirectory ->
                              {| name = volumeName
                                 azureFile = Unchecked.defaultof<_>
                                 emptyDir = obj()
                                 gitRepo = Unchecked.defaultof<_>
                                 secret = Unchecked.defaultof<_> |}
                          |  volumeName, Volume.GitRepo (repository, directory, revision) ->
                              {| name = volumeName
                                 azureFile = Unchecked.defaultof<_>
                                 emptyDir = null
                                 gitRepo = {| repository = repository
                                              directory = directory |> Option.toObj
                                              revision = revision |> Option.toObj |}
                                 secret = Unchecked.defaultof<_> |}
                          |  volumeName, Volume.Secret secrets->
                              {| name = volumeName
                                 azureFile = Unchecked.defaultof<_>
                                 emptyDir = null
                                 gitRepo = Unchecked.defaultof<_>
                                 secret =
                                     let jobj = JObject()
                                     for (SecretFile (name, secret)) in secrets do
                                         jobj.Add (name, secret |> System.Convert.ToBase64String |> JValue)
                                     jobj
                                 |}
                      )
                   |}
               tags = this.Tags
            |} :> _