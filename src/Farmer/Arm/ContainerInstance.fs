[<AutoOpen>]
module Farmer.Arm.ContainerInstance

open Farmer
open Farmer.ContainerGroup
open Farmer.Identity
open System

let containerGroups = ResourceType ("Microsoft.ContainerInstance/containerGroups", "2019-12-01")

type ContainerGroupIpAddress =
    { Type : IpAddressType
      Ports :
        {| Protocol : TransmissionProtocol
           Port : uint16 |} Set }

type ImageRegistryCredential =
    { Server : string
      Username : string
      Password : SecureParameter }

type ContainerGroup =
    { Name : ResourceName
      Location : Location
      ContainerInstances :
        {| Name : ResourceName
           Image : string
           Command : string list
           Ports : uint16 Set
           Cpu : float
           Memory : float<Gb>
           EnvironmentVariables: Map<string, EnvVar>
           VolumeMounts : Map<string,string>
        |} list
      OperatingSystem : OS
      RestartPolicy : RestartPolicy
      Identity : ManagedIdentity
      ImageRegistryCredentials : ImageRegistryCredential list
      InitContainers :
        {| Name : ResourceName
           Image : string
           Command : string list
           EnvironmentVariables: Map<string, EnvVar>
           VolumeMounts : Map<string,string>
        |} list
      IpAddress : ContainerGroupIpAddress option
      NetworkProfile : ResourceName option
      Volumes : Map<string, Volume>
      Tags: Map<string,string>  }
    member this.NetworkProfilePath =
        this.NetworkProfile
        |> Option.map networkProfiles.resourceId
    member private this.Dependencies = [
        yield! Option.toList this.NetworkProfilePath

        for _, volume in this.Volumes |> Map.toSeq do
            match volume with
            | Volume.AzureFileShare (shareName, storageAccountName) ->
                fileShares.resourceId (storageAccountName.ResourceName, ResourceName "default", shareName)
            | _ ->
                ()

        // If the identity is set, include any dependent identity's resource ID
        yield! this.Identity.Dependencies
    ]

    interface IParameters with
        member this.SecureParameters = [
            for credential in this.ImageRegistryCredentials do
                credential.Password
            for container in this.ContainerInstances do
                for envVar in container.EnvironmentVariables do
                    match envVar.Value with
                    | SecureEnvValue p -> p
                    | EnvValue _ -> ()
            for volume in this.Volumes do
                match volume.Value with
                | Volume.Secret secrets ->
                    for secret in secrets do
                        match secret with
                        | SecretFileParameter (_, parameter) -> parameter
                        | SecretFileContents _ -> ()
                | Volume.EmptyDirectory
                | Volume.AzureFileShare _
                | Volume.Secret _
                | Volume.GitRepo _ ->
                    ()
        ]
    interface IArmResource with
        member this.ResourceId = containerGroups.resourceId this.Name
        member this.JsonModel =
            {| containerGroups.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                   identity = this.Identity |> ManagedIdentity.toArmJson
                   properties =
                       {| containers =
                           this.ContainerInstances
                           |> List.map (fun container ->
                               {| name = container.Name.Value.ToLowerInvariant ()
                                  properties =
                                   {| image = container.Image
                                      command = container.Command
                                      ports = container.Ports |> Set.map (fun port -> {| port = port |})
                                      environmentVariables = [
                                          for key, value in Map.toSeq container.EnvironmentVariables do
                                              match value with
                                              | EnvValue value ->
                                                {| name = key; value = value; secureValue = null |}
                                              | SecureEnvValue value ->
                                                {| name = key; value = null; secureValue = value.ArmExpression.Eval() |}
                                      ]
                                      resources =
                                       {| requests =
                                           {| cpu = container.Cpu
                                              memoryInGB = container.Memory |}
                                       |}
                                      volumeMounts =
                                          container.VolumeMounts
                                          |> Seq.map (fun kvp -> {| name=kvp.Key; mountPath=kvp.Value |}) |> List.ofSeq
                                   |}
                               |})
                          initContainers =
                           this.InitContainers
                           |> List.map (fun container ->
                               {| name = container.Name.Value.ToLowerInvariant ()
                                  properties =
                                   {| image = container.Image
                                      command = container.Command
                                      environmentVariables = [
                                          for key, value in Map.toSeq container.EnvironmentVariables do
                                              match value with
                                              | EnvValue value ->
                                                {| name = key; value = value; secureValue = null |}
                                              | SecureEnvValue value ->
                                                {| name = key; value = null; secureValue = value.ArmExpression.Eval() |}
                                      ]
                                      volumeMounts =
                                          container.VolumeMounts
                                          |> Seq.map (fun kvp -> {| name=kvp.Key; mountPath=kvp.Value |}) |> List.ofSeq
                                   |}
                               |})
                          osType = string this.OperatingSystem
                          restartPolicy =
                            match this.RestartPolicy with
                            | AlwaysRestart -> "Always"
                            | NeverRestart -> "Never"
                            | RestartOnFailure -> "OnFailure"
                          imageRegistryCredentials =
                              this.ImageRegistryCredentials
                              |> List.map (fun cred ->
                                  {| server = cred.Server
                                     username = cred.Username
                                     password = cred.Password.ArmExpression.Eval() |})
                          ipAddress =
                            match this.IpAddress with
                            | Some ipAddresses ->
                                {| ``type`` =
                                    match ipAddresses.Type with
                                    | PublicAddress | PublicAddressWithDns _ -> "Public"
                                    | PrivateAddress _ -> "Private"
                                   ports = [
                                       for port in ipAddresses.Ports do
                                        {| protocol = string port.Protocol
                                           port = port.Port |}
                                   ]
                                   dnsNameLabel =
                                    match ipAddresses.Type with
                                    | PublicAddressWithDns dnsLabel -> dnsLabel
                                    | _ -> null
                                |} |> box
                            | None -> null
                          networkProfile =
                            this.NetworkProfilePath
                            |> Option.map(fun path -> box {| id = path.Eval() |})
                            |> Option.toObj
                          volumes = [
                            for (key, value) in Map.toSeq this.Volumes do
                                match key, value with
                                |  volumeName, Volume.AzureFileShare (shareName, accountName) ->
                                    {| name = volumeName
                                       azureFile =
                                           {| shareName = shareName.Value
                                              storageAccountName = accountName.ResourceName.Value
                                              storageAccountKey = $"[listKeys('Microsoft.Storage/storageAccounts/{accountName.ResourceName.Value}', '2018-07-01').keys[0].value]" |}
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
                                       secret = dict [
                                        for secret in secrets do
                                            match secret with
                                            | SecretFileContents (name, secret) ->
                                                name, Convert.ToBase64String secret
                                            | SecretFileParameter (name, parameter) ->
                                                name, parameter.ArmExpression.Map(sprintf "base64(%s)").Eval()
                                       ]
                                    |}
                          ]
                       |}
            |} :> _
