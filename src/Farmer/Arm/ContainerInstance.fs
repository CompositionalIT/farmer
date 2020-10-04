[<AutoOpen>]
module Farmer.Arm.ContainerInstance

open Farmer
open Farmer.Arm.ManagedIdentity
open Farmer.ContainerGroup
open Farmer.CoreTypes
open Newtonsoft.Json.Linq

let containerGroups = ResourceType ("Microsoft.ContainerInstance/containerGroups", "2018-10-01")

type ContainerGroupIpAddress =
    { Type : IpAddressType
      Ports :
        {| Protocol : TransmissionProtocol
           Port : uint16 |} Set }

type EnvVarValue = EnvValue of string | EnvSecureValue of string

type ContainerGroup =
    { Name : ResourceName
      Location : Location
      ContainerInstances :
        {| Name : ResourceName
           Image : string
           Ports : uint16 Set
           Cpu : int
           Memory : float<Gb>
           EnvironmentVariables: Map<string, EnvVarValue>
           VolumeMounts : Map<string,string>
        |} list
      OperatingSystem : OS
      RestartPolicy : RestartPolicy
      Identity : ContainerGroupIdentity option
      IpAddress : ContainerGroupIpAddress
      NetworkProfile : ResourceName option
      Volumes : Map<string, Volume>
      Tags: Map<string,string>  }
    member this.NetworkProfilePath =
        this.NetworkProfile
        |> Option.map (fun networkProfile -> ArmExpression.resourceId(networkProfiles, networkProfile).Eval())
    member private this.Dependencies = [
        match this.NetworkProfilePath with
        | Some path -> ResourceName path
        | None -> ()

        for _, volume in this.Volumes |> Map.toSeq do
            match volume with
            | Volume.AzureFileShare (shareName, storageAccountName) ->
                let fullShareName = [ storageAccountName; "default"; shareName ] |> Seq.map ResourceName |> Array.ofSeq
                ArmExpression.resourceId(fileShares, fullShareName).Eval() |> ResourceName
            | _ ->
                ()

        match this.Identity with
        | Some (UserAssigned identities) ->
            yield! identities |> List.map (UserAssignedIdentity.resourceId >> ResourceName)
        | _ -> ()
    ]

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| containerGroups.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                   identity =
                       match this.Identity with
                       | None -> {| ``type`` = "None"; userAssignedIdentities = null |}
                       | Some (SystemAssigned) -> {| ``type`` = "SystemAssigned"; userAssignedIdentities = null |}
                       | Some (UserAssigned identities) ->
                         // Identities are assigned as a dictionary with the user identity resource ID as the key
                         // and an empty object as the value.
                         let userAssigned = JObject()
                         identities |> List.iter (fun identity ->
                             userAssigned.Add (JProperty(UserAssignedIdentity.resourceId identity, JObject())))
                         {| ``type`` = "UserAssigned"
                            userAssignedIdentities = userAssigned |}
                   properties =
                       {| containers =
                           this.ContainerInstances
                           |> List.map (fun container ->
                               {| name = container.Name.Value.ToLowerInvariant ()
                                  properties =
                                   {| image = container.Image
                                      ports = container.Ports |> Set.map (fun port -> {| port = port |})
                                      environmentVariables = [
                                          for (key, value) in Map.toSeq container.EnvironmentVariables do
                                              match value with
                                              | EnvValue v -> {| name = key; value = v; secureValue = null |}
                                              | EnvSecureValue v -> {| name = key; value = null; secureValue = v |}
                                      ]
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
                          volumes = [
                            for (key, value) in Map.toSeq this.Volumes do
                                match key, value with
                                |  volumeName, Volume.AzureFileShare (shareName, accountName) ->
                                    {| name = volumeName
                                       azureFile =
                                           {| shareName = shareName
                                              storageAccountName = accountName
                                              storageAccountKey = sprintf "[listKeys('Microsoft.Storage/storageAccounts/%s', '2018-07-01').keys[0].value]" accountName |}
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
                          ]
                       |}
            |} :> _
