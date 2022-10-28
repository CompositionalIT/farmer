[<AutoOpen>]
module Farmer.Arm.ContainerInstance

open Farmer
open Farmer.Arm
open Farmer.ContainerGroup
open Farmer.Identity
open System

let containerGroups =
    ResourceType("Microsoft.ContainerInstance/containerGroups", "2021-10-01")

type ContainerGroupIpAddress =
    {
        Type: IpAddressType
        Ports: {| Protocol: TransmissionProtocol
                  Port: uint16 |} Set
    }

type ContainerInstanceGpu =
    {
        Count: int
        Sku: Gpu.Sku
    }

    static member internal JsonModel =
        function
        | None -> null
        | Some gpu ->
            {|
                count = gpu.Count
                sku = string gpu.Sku
            |}
            :> obj

/// Defines a command or HTTP request to get the status of a container.
type ContainerProbe =
    {
        Exec: string list
        HttpGet: Uri option
        /// The probe will not run until this delay after container startup. Default is 0 - runs immediately.
        InitialDelaySeconds: int option
        /// How often to execute the probe against the container - default is 10 seconds.
        PeriodSeconds: int option
        /// Number of failures before this container is considered unhealthy - default is 3.
        FailureThreshold: int option
        /// Number of successes before this container is considered healthy - default is 1.
        SuccessThreshold: int option
        /// Number of seconds for the probe to run - default is 1 second.
        TimeoutSeconds: int option
    }

    static member internal JsonModel =
        function
        | None -> null
        | Some probe ->
            {|
                exec =
                    if probe.Exec.Length > 0 then
                        {| command = probe.Exec |} |> box
                    else
                        null
                httpGet =
                    probe.HttpGet
                    |> Option.map (fun (uri: Uri) ->
                        {|
                            path = uri.AbsolutePath
                            port = uri.Port
                            scheme = uri.Scheme
                        |}
                        |> box)
                    |> Option.defaultValue null
                initialDelaySeconds = probe.InitialDelaySeconds |> Option.map box |> Option.defaultValue null
                periodSeconds = probe.PeriodSeconds |> Option.map box |> Option.defaultValue null
                failureThreshold = probe.FailureThreshold |> Option.map box |> Option.defaultValue null
                successThreshold = probe.SuccessThreshold |> Option.map box |> Option.defaultValue null
                timeoutSeconds = probe.TimeoutSeconds |> Option.map box |> Option.defaultValue null
            |}
            :> obj

type ContainerGroupDiagnostics =
    {
        LogType: LogType
        Workspace: LogAnalyticsWorkspace
    }

    static member internal JsonModel =
        function
        | None -> null
        | Some diag ->
            let logAnalyticsId, logAnalyticsKey =
                match diag.Workspace with
                | LogAnalyticsWorkspace.WorkspaceKey (workspaceId, workspaceKey) -> workspaceId, workspaceKey
                | LogAnalyticsWorkspace.WorkspaceResourceId resourceRef ->
                    (LogAnalytics.LogAnalytics.getCustomerId resourceRef.ResourceId).Eval(),
                    (LogAnalytics.LogAnalytics.getPrimarySharedKey resourceRef.ResourceId).Eval()

            {|
                logAnalytics =
                    {|
                        logType =
                            match diag.LogType with
                            | ContainerInsights -> "ContainerInsights"
                            | ContainerInstanceLogs -> "ContainerInstanceLogs"
                        workspaceId = logAnalyticsId
                        workspaceKey = logAnalyticsKey
                    |}
            |}
            :> obj

type ContainerGroupDnsConfiguration =
    {
        NameServers: string list
        SearchDomains: string list
        Options: string list
    }

    static member internal JsonModel =
        function
        | None -> null
        | Some dnsConfig ->
            {|
                nameServers = dnsConfig.NameServers
                options =
                    if dnsConfig.Options.IsEmpty then
                        null
                    else
                        dnsConfig.Options |> String.concat " "
                searchDomains =
                    if dnsConfig.SearchDomains.IsEmpty then
                        null
                    else
                        dnsConfig.SearchDomains |> String.concat " "
            |}
            :> obj

type ContainerGroup =
    {
        Name: ResourceName
        Location: Location
        AvailabilityZone: string option
        ContainerInstances: {| Name: ResourceName
                               Image: Containers.DockerImage
                               Command: string list
                               Ports: uint16 Set
                               Cpu: float
                               Memory: float<Gb>
                               Gpu: ContainerInstanceGpu option
                               EnvironmentVariables: Map<string, EnvVar>
                               VolumeMounts: Map<string, string>
                               LivenessProbe: ContainerProbe option
                               ReadinessProbe: ContainerProbe option |} list
        Diagnostics: ContainerGroupDiagnostics option
        DnsConfig: ContainerGroupDnsConfiguration option
        OperatingSystem: OS
        RestartPolicy: RestartPolicy
        Identity: ManagedIdentity
        ImageRegistryCredentials: ImageRegistryAuthentication list
        InitContainers: {| Name: ResourceName
                           Image: Containers.DockerImage
                           Command: string list
                           EnvironmentVariables: Map<string, EnvVar>
                           VolumeMounts: Map<string, string> |} list
        IpAddress: ContainerGroupIpAddress option
        NetworkProfile: ResourceName option
        SubnetIds: LinkedResource list
        Volumes: Map<string, Volume>
        Tags: Map<string, string>
        Dependencies: Set<ResourceId>
    }

    member this.NetworkProfilePath =
        this.NetworkProfile |> Option.map networkProfiles.resourceId

    member private this.dependencies =
        [
            yield! Option.toList this.NetworkProfilePath
            for id in this.SubnetIds do
                match id with
                | Managed subnetId ->
                    { subnetId with
                        Type = virtualNetworks
                        Segments = []
                    } // should be vnet ID
                | Unmanaged _ -> ()

            for _, volume in this.Volumes |> Map.toSeq do
                match volume with
                | Volume.AzureFileShare (shareName, storageAccountName) ->
                    fileShares.resourceId (storageAccountName.ResourceName, ResourceName "default", shareName)
                | _ -> ()

            match this.Diagnostics with
            | Some {
                       Workspace = LogAnalyticsWorkspace.WorkspaceResourceId (LinkedResource.Managed (resId))
                   } -> resId
            | _ -> ()
            // If the identity is set, include any dependent identity's resource ID
            yield! this.Identity.Dependencies
            yield! this.Dependencies
        ]

    interface IParameters with
        member this.SecureParameters =
            [
                for credential in this.ImageRegistryCredentials do
                    match credential with
                    | ImageRegistryAuthentication.Credential credential -> credential.Password
                    | ImageRegistryAuthentication.ListCredentials _ -> ()
                    | ImageRegistryAuthentication.ManagedIdentityCredential _ -> ()
                for container in this.ContainerInstances do
                    for envVar in container.EnvironmentVariables do
                        match envVar.Value with
                        | SecureEnvValue p -> p
                        | SecureEnvExpression _ -> ()
                        | EnvValue _ -> ()
                for container in this.InitContainers do
                    for envVar in container.EnvironmentVariables do
                        match envVar.Value with
                        | SecureEnvValue p -> p
                        | SecureEnvExpression _ -> ()
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
                    | Volume.GitRepo _ -> ()
            ]

    /// Creates a version depending on whether this needs the legacy API features.
    member private this.resourceCommonProps =
        match this.NetworkProfile with
        | Some _ -> // Using network profiles, need to use older container groups API
            /// This older API version supports network profiles.
            let legacyContainerGroups =
                ResourceType("Microsoft.ContainerInstance/containerGroups", "2021-03-01")

            legacyContainerGroups.Create(this.Name, this.Location, this.dependencies, this.Tags)
        | None -> // Not using network profiles, use current API version
            containerGroups.Create(this.Name, this.Location, this.dependencies, this.Tags)

    interface IArmResource with
        member this.ResourceId = containerGroups.resourceId this.Name

        member this.JsonModel =
            {| this.resourceCommonProps with
                identity = this.Identity.ToArmJson
                properties =
                    {|
                        containers =
                            this.ContainerInstances
                            |> List.map (fun container ->
                                {|
                                    name = container.Name.Value.ToLowerInvariant()
                                    properties =
                                        {|
                                            image = container.Image.ImageTag
                                            command = container.Command
                                            ports = container.Ports |> Set.map (fun port -> {| port = port |})
                                            environmentVariables =
                                                [
                                                    for key, value in Map.toSeq container.EnvironmentVariables do
                                                        match value with
                                                        | EnvValue value ->
                                                            {|
                                                                name = key
                                                                value = value
                                                                secureValue = null
                                                            |}
                                                        | SecureEnvExpression armExpression ->
                                                            {|
                                                                name = key
                                                                value = null
                                                                secureValue = armExpression.Eval()
                                                            |}
                                                        | SecureEnvValue value ->
                                                            {|
                                                                name = key
                                                                value = null
                                                                secureValue = value.ArmExpression.Eval()
                                                            |}
                                                ]
                                            livenessProbe = ContainerProbe.JsonModel container.LivenessProbe
                                            readinessProbe = ContainerProbe.JsonModel container.ReadinessProbe
                                            resources =
                                                {|
                                                    requests =
                                                        {|
                                                            cpu = container.Cpu
                                                            memoryInGB = container.Memory
                                                            gpu = ContainerInstanceGpu.JsonModel container.Gpu
                                                        |}
                                                |}
                                            volumeMounts =
                                                container.VolumeMounts
                                                |> Seq.map (fun kvp ->
                                                    {|
                                                        name = kvp.Key
                                                        mountPath = kvp.Value
                                                    |})
                                                |> List.ofSeq
                                        |}
                                |})
                        diagnostics = ContainerGroupDiagnostics.JsonModel this.Diagnostics
                        dnsConfig = ContainerGroupDnsConfiguration.JsonModel this.DnsConfig
                        initContainers =
                            this.InitContainers
                            |> List.map (fun container ->
                                {|
                                    name = container.Name.Value.ToLowerInvariant()
                                    properties =
                                        {|
                                            image = container.Image.ImageTag
                                            command = container.Command
                                            environmentVariables =
                                                [
                                                    for key, value in Map.toSeq container.EnvironmentVariables do
                                                        match value with
                                                        | EnvValue value ->
                                                            {|
                                                                name = key
                                                                value = value
                                                                secureValue = null
                                                            |}
                                                        | SecureEnvExpression armExpression ->
                                                            {|
                                                                name = key
                                                                value = null
                                                                secureValue = armExpression.Eval()
                                                            |}
                                                        | SecureEnvValue value ->
                                                            {|
                                                                name = key
                                                                value = null
                                                                secureValue = value.ArmExpression.Eval()
                                                            |}
                                                ]
                                            volumeMounts =
                                                container.VolumeMounts
                                                |> Seq.map (fun kvp ->
                                                    {|
                                                        name = kvp.Key
                                                        mountPath = kvp.Value
                                                    |})
                                                |> List.ofSeq
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
                                match cred with
                                | ImageRegistryAuthentication.Credential cred ->
                                    {|
                                        server = cred.Server
                                        username = cred.Username
                                        password = cred.Password.ArmExpression.Eval()
                                        identity = null
                                    |}
                                | ImageRegistryAuthentication.ListCredentials resourceId ->
                                    {|
                                        server =
                                            ArmExpression
                                                .create(
                                                    $"reference({resourceId.ArmExpression.Value}, '2019-05-01').loginServer"
                                                )
                                                .Eval()
                                        username =
                                            ArmExpression
                                                .create(
                                                    $"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').username"
                                                )
                                                .Eval()
                                        password =
                                            ArmExpression
                                                .create(
                                                    $"listCredentials({resourceId.ArmExpression.Value}, '2019-05-01').passwords[0].value"
                                                )
                                                .Eval()
                                        identity = null
                                    |}
                                | ImageRegistryAuthentication.ManagedIdentityCredential cred ->
                                    {|
                                        server = cred.Server
                                        username = String.Empty
                                        password = null
                                        identity =
                                            cred.Identity.UserAssigned
                                            |> List.tryHead
                                            |> Option.map (fun upn -> upn.ResourceId.ArmExpression.Eval())
                                            |> Option.defaultValue null
                                    |})
                        ipAddress =
                            match this.IpAddress with
                            | Some ipAddresses ->
                                {|
                                    ``type`` =
                                        match ipAddresses.Type with
                                        | PublicAddress
                                        | PublicAddressWithDns _ -> "Public"
                                        | PrivateAddress _ -> "Private"
                                    ports =
                                        [
                                            for port in ipAddresses.Ports do
                                                {|
                                                    protocol = string port.Protocol
                                                    port = port.Port
                                                |}
                                        ]
                                    dnsNameLabel =
                                        match ipAddresses.Type with
                                        | PublicAddressWithDns dnsLabel -> dnsLabel
                                        | _ -> null
                                |}
                                |> box
                            | None -> null
                        networkProfile =
                            this.NetworkProfilePath
                            |> Option.map (fun path -> box {| id = path.Eval() |})
                            |> Option.toObj
                        subnetIds =
                            if this.SubnetIds.IsEmpty then
                                null
                            else
                                this.SubnetIds
                                |> List.map (fun subnetId -> {| id = subnetId.ResourceId.Eval() |})
                                |> box
                        volumes =
                            [
                                for key, value in Map.toSeq this.Volumes do
                                    match key, value with
                                    | volumeName, Volume.AzureFileShare (shareName, accountName) ->
                                        {|
                                            name = volumeName
                                            azureFile =
                                                {|
                                                    shareName = shareName.Value
                                                    storageAccountName = accountName.ResourceName.Value
                                                    storageAccountKey =
                                                        $"[listKeys('Microsoft.Storage/storageAccounts/{accountName.ResourceName.Value}', '2018-07-01').keys[0].value]"
                                                |}
                                            emptyDir = null
                                            gitRepo = Unchecked.defaultof<_>
                                            secret = Unchecked.defaultof<_>
                                        |}
                                    | volumeName, Volume.EmptyDirectory ->
                                        {|
                                            name = volumeName
                                            azureFile = Unchecked.defaultof<_>
                                            emptyDir = obj ()
                                            gitRepo = Unchecked.defaultof<_>
                                            secret = Unchecked.defaultof<_>
                                        |}
                                    | volumeName, Volume.GitRepo (repository, directory, revision) ->
                                        {|
                                            name = volumeName
                                            azureFile = Unchecked.defaultof<_>
                                            emptyDir = null
                                            gitRepo =
                                                {|
                                                    repository = repository
                                                    directory = directory |> Option.toObj
                                                    revision = revision |> Option.toObj
                                                |}
                                            secret = Unchecked.defaultof<_>
                                        |}
                                    | volumeName, Volume.Secret secrets ->
                                        {|
                                            name = volumeName
                                            azureFile = Unchecked.defaultof<_>
                                            emptyDir = null
                                            gitRepo = Unchecked.defaultof<_>
                                            secret =
                                                dict
                                                    [
                                                        for secret in secrets do
                                                            match secret with
                                                            | SecretFileContents (name, secret) ->
                                                                name, Convert.ToBase64String secret
                                                            | SecretFileParameter (name, parameter) ->
                                                                name,
                                                                parameter.ArmExpression.Map(sprintf "base64(%s)").Eval()
                                                    ]
                                        |}
                            ]
                    |}
                zones = this.AvailabilityZone |> Option.map Array.singleton |> Option.defaultValue null
            |}
