[<AutoOpen>]
module Farmer.Builders.ContainerGroups

open Farmer
open Farmer.Arm
open Farmer.Builders
open Farmer.ContainerGroup
open Farmer.Identity
open Farmer.Arm.ContainerInstance
open Farmer.Arm.Network
open System.Text

//TODO: I think we should rename these to standard F# naming conventioned e.g. VolumeMount, EmptyDir etc.
//TODO: Indeed, this should either be made into a module with let-bound functions, or make use of static
//members by using e.g. optional parameters or overloading.
type volume_mount =
    static member empty_dir volumeName = volumeName, Volume.EmptyDirectory

    static member azureFile volumeName shareName (storageAccountName: string) =
        volumeName,
        Volume.AzureFileShare(ResourceName shareName, Storage.StorageAccountName.Create(storageAccountName).OkValue)

    static member git_repo volumeName repository =
        volumeName, Volume.GitRepo(repository, None, None)

    static member git_repo_directory volumeName repository directory =
        volumeName, Volume.GitRepo(repository, Some directory, None)

    static member git_repo_directory_revision volumeName repository directory revision =
        volumeName, Volume.GitRepo(repository, Some directory, Some revision)

    static member secret volumeName (file: string) (secret: byte array) =
        volumeName, Volume.Secret [ SecretFileContents(file, secret) ]

    static member secrets volumeName (secrets: (string * byte array) list) =
        volumeName, secrets |> List.map SecretFileContents |> Volume.Secret

    static member secret_string volumeName (file: string) (secret: string) =
        volumeName, Volume.Secret [ SecretFileContents(file, Encoding.UTF8.GetBytes secret) ]

    static member secret_parameter volumeName (file: string) (secretParameterName: string) =
        volumeName, Volume.Secret [ SecretFileParameter(file, SecureParameter secretParameterName) ]

/// Represents configuration for a single Container.
type ContainerInstanceConfig =
    {
        /// The name of the container instance
        Name: ResourceName
        /// The container instance image
        Image: Containers.DockerImage option
        /// The commands to execute within the container instance in exec form
        Command: string list
        /// List of ports the container instance listens on
        Ports: Map<uint16, PortAccess>
        /// Max number of CPU cores the container instance may use
        Cpu: float
        /// Max gigabytes of memory the container instance may use
        Memory: float<Gb>
        // Container instances gpu
        Gpu: ContainerInstanceGpu option
        /// Environment variables for the container
        EnvironmentVariables: Map<string, EnvVar>
        /// Liveness probe for checking the container's health.
        LivenessProbe: ContainerProbe option
        /// Readiness probe to wait for the container to be ready to accept requests.
        ReadinessProbe: ContainerProbe option
        /// Volume mounts for the container
        VolumeMounts: Map<string, string>
    }

/// Represents configuration for an init container that runs on container group startup.
type InitContainerConfig =
    {
        /// The name of the container instance
        Name: ResourceName
        /// The container instance image
        Image: Containers.DockerImage option
        /// The commands to execute within the container instance in exec form
        Command: string list
        /// Environment variables for the container
        EnvironmentVariables: Map<string, EnvVar>
        /// Volume mounts for the container
        VolumeMounts: Map<string, string>
    }

type ContainerGroupConfig =
    {
        /// The name of the container group.
        Name: ResourceName
        /// Availability zone where the container group should be deployed.
        AvailabilityZone: string option
        /// Diagnostics and logging for the container group
        Diagnostics: ContainerGroupDiagnostics option
        /// DNS configuration for the container group
        DnsConfig: ContainerGroupDnsConfiguration option
        /// Container group OS.
        OperatingSystem: OS
        /// Restart policy for the container group.
        RestartPolicy: RestartPolicy
        /// Credentials for image registries used by containers in this group.
        ImageRegistryCredentials: ImageRegistryAuthentication list
        /// IP address for the container group.
        IpAddress: ContainerGroupIpAddress option
        /// The init containers in this container group.
        InitContainers: InitContainerConfig list
        /// The instances in this container group.
        Instances: ContainerInstanceConfig list
        /// Name of the network profile for this container's group - not supported when specifying the availability zone.
        NetworkProfile: ResourceName option
        /// Resource ID of the virtual network where this container group should be attached.
        VirtualNetwork: LinkedResource option
        /// Name of the subnet where this container group should be attached.
        SubnetName: ResourceName option
        /// Volumes to mount on the container group.
        Volumes: Map<string, Volume>
        /// Managed identity for the container group.
        Identity: ManagedIdentity
        /// Tags for the container group.
        Tags: Map<string, string>
        /// Additional dependencies.
        Dependencies: Set<ResourceId>
    }

    member private this.ResourceId = containerGroups.resourceId this.Name
    member this.SystemIdentity = SystemIdentity this.ResourceId

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location =
            [
                {
                    Location = location
                    Name = this.Name
                    AvailabilityZone =
                        if this.AvailabilityZone.IsSome && this.NetworkProfile.IsSome then
                            raiseFarmer $"Cannot specify availability zone when using network profiles."
                        else
                            this.AvailabilityZone
                    ContainerInstances =
                        [
                            for instance in this.Instances do
                                match instance.Image with
                                | None -> raiseFarmer $"Missing image tag for container named '{instance.Name}'."
                                | Some image ->
                                    {|
                                        Name = instance.Name
                                        Image = image
                                        Command = instance.Command
                                        Ports = instance.Ports |> Map.toSeq |> Seq.map fst |> Set
                                        Cpu = instance.Cpu
                                        Memory = instance.Memory
                                        Gpu = instance.Gpu
                                        EnvironmentVariables = instance.EnvironmentVariables
                                        LivenessProbe = instance.LivenessProbe
                                        ReadinessProbe = instance.ReadinessProbe
                                        VolumeMounts = instance.VolumeMounts
                                    |}
                        ]
                    Diagnostics = this.Diagnostics
                    DnsConfig =
                        if this.DnsConfig.IsSome && this.NetworkProfile.IsNone then
                            raiseFarmer "DNS configuration can only be set when attached to a virtual network."
                        else
                            this.DnsConfig
                    OperatingSystem = this.OperatingSystem
                    RestartPolicy = this.RestartPolicy
                    Identity = this.Identity
                    ImageRegistryCredentials = this.ImageRegistryCredentials
                    InitContainers =
                        [
                            for initContainer in this.InitContainers do
                                match initContainer.Image with
                                | None ->
                                    raiseFarmer $"Missing image tag for initContainer named '{initContainer.Name}'."
                                | Some image ->
                                    {|
                                        Name = initContainer.Name
                                        Image = image
                                        Command = initContainer.Command
                                        EnvironmentVariables = initContainer.EnvironmentVariables
                                        VolumeMounts = initContainer.VolumeMounts
                                    |}
                        ]
                    IpAddress = this.IpAddress
                    NetworkProfile =
                        if
                            this.NetworkProfile.IsSome
                            && (this.VirtualNetwork.IsSome || this.SubnetName.IsSome)
                        then
                            raiseFarmer
                                $"Should not set network profile on container group '{this.Name.Value}' when using vnet and subnet."
                        else
                            this.NetworkProfile
                    SubnetIds =
                        match this.VirtualNetwork, this.SubnetName with
                        | None, None -> []
                        | Some (Managed vnetId), Some subnet ->
                            { vnetId with
                                Type = subnets
                                Segments = [ subnet ]
                            }
                            |> Managed
                            |> List.singleton
                        | Some (Unmanaged vnetId), Some subnet ->
                            { vnetId with
                                Type = subnets
                                Segments = [ subnet ]
                            }
                            |> Unmanaged
                            |> List.singleton
                        | Some vnetId, None ->
                            raiseFarmer
                                $"Missing subnet for attaching container group '{this.Name.Value}' to vnet '{vnetId.Name.Value}'."
                        | None, subnetName ->
                            raiseFarmer
                                $"Missing vnet for attaching container group '{this.Name.Value}' to subnet '{subnetName.Value}'."
                    Volumes = this.Volumes
                    Tags = this.Tags
                    Dependencies = this.Dependencies
                }
            ]

type ContainerGpuConfig = { Count: int; Sku: Gpu.Sku }

type ContainerProbeType =
    | LivenessProbe
    | ReadinessProbe

type ContainerProbeConfig =
    {
        ProbeType: ContainerProbeType
        Probe: ContainerProbe
    }

type ContainerNetworkInterfaceIpConfig = { Name: ResourceName; Subnet: string }

type ContainerNetworkInterfaceConfiguration =
    {
        IpConfigs: ContainerNetworkInterfaceIpConfig list
    }

type NetworkProfileConfig =
    {
        Name: ResourceName
        ContainerNetworkInterfaceConfigurations: ContainerNetworkInterfaceConfiguration list
        VirtualNetwork: LinkedResource
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = networkProfiles.resourceId this.Name

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    Dependencies =
                        [
                            match this.VirtualNetwork with
                            | Managed resId -> resId // Only generate dependency if this is managed by Farmer (same template)
                            | _ -> ()
                        ]
                        |> Set.ofList
                    ContainerNetworkInterfaceConfigurations =
                        this.ContainerNetworkInterfaceConfigurations
                        |> List.map (fun ifconfig ->
                            {|
                                IpConfigs =
                                    ifconfig.IpConfigs
                                    |> List.map (fun ipConfig ->
                                        {|
                                            Name = ipConfig.Name
                                            SubnetName = ResourceName ipConfig.Subnet
                                        |})
                            |})
                    VirtualNetwork =
                        match this.VirtualNetwork with
                        | Managed resId
                        | Unmanaged resId -> resId
                    Tags = this.Tags
                }
            ]

type ContainerGroupBuilder() =
    member private _.AddPort(state, portType, port) : ContainerGroupConfig =
        { state with
            IpAddress =
                match state.IpAddress with
                | Some ipAddresses ->
                    { ipAddresses with
                        Ports = ipAddresses.Ports.Add {| Protocol = portType; Port = port |}
                    }
                    |> Some
                | None ->
                    {
                        Type = IpAddressType.PublicAddress
                        Ports = [ {| Protocol = portType; Port = port |} ] |> Set.ofList
                    }
                    |> Some
        }

    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Diagnostics = None
            DnsConfig = None
            OperatingSystem = Linux
            RestartPolicy = AlwaysRestart
            Identity = ManagedIdentity.Empty
            ImageRegistryCredentials = []
            InitContainers = []
            IpAddress = None
            NetworkProfile = None
            SubnetName = None
            VirtualNetwork = None
            Instances = []
            Volumes = Map.empty
            AvailabilityZone = None
            Tags = Map.empty
            Dependencies = Set.empty
        }

    member this.Run(state: ContainerGroupConfig) =
        // Automatically apply all public-facing ports to the container group itself.
        state.Instances
        |> Seq.collect (fun i ->
            i.Ports
            |> Map.toSeq
            |> Seq.choose (function
                | (port, PublicPort) -> Some port
                | _, InternalPort -> None))
        |> Seq.fold (fun (state: ContainerGroupConfig) port -> this.AddPort(state, TCP, port)) state

    member this.AddTcpPort(state: ContainerGroupConfig, port) = this.AddPort(state, TCP, port)

    /// Sets the name of the container group.
    [<CustomOperation "name">]
    member _.Name(state: ContainerGroupConfig, name) = { state with Name = name }

    member this.Name(state: ContainerGroupConfig, name) = this.Name(state, ResourceName name)

    /// Sets the OS type (default Linux)
    [<CustomOperation "operating_system">]
    member _.OsType(state: ContainerGroupConfig, os) = { state with OperatingSystem = os }

    /// Sets the restart policy (default Always)
    [<CustomOperation "restart_policy">]
    member _.RestartPolicy(state: ContainerGroupConfig, restartPolicy) =
        { state with
            RestartPolicy = restartPolicy
        }

    member private _.SetIpAddress(state: ContainerGroupConfig, ipAddressType, ports) =
        { state with
            IpAddress =
                {
                    Type = ipAddressType
                    Ports =
                        ports
                        |> Seq.map (fun (protocol, port) -> {| Protocol = protocol; Port = port |})
                        |> Set
                }
                |> Some
        }

    /// Sets the IP address to a public address with a DNS label
    [<CustomOperation "public_dns">]
    member this.PublicDns(state, dnsLabel, ports) =
        this.SetIpAddress(state, PublicAddressWithDns dnsLabel, ports)

    /// Sets the IP address to a private address assigned by the vnet
    [<CustomOperation "private_ip">]
    member this.PrivateIp(state: ContainerGroupConfig, ports) =
        this.SetIpAddress(state, PrivateAddress, ports)

    /// Sets a network profile for the container's group.
    [<CustomOperation "network_profile">]
    member _.NetworkProfile(state: ContainerGroupConfig, networkProfileName: string) =
        { state with
            NetworkProfile = Some(ResourceName networkProfileName)
        }

    member _.NetworkProfile(state: ContainerGroupConfig, networkProfile: NetworkProfileConfig) =
        { state with
            NetworkProfile = Some networkProfile.Name
        }

    /// Sets the name of a virtual network where this container group should be attached.
    [<CustomOperation "vnet">]
    member _.VNetId(state: ContainerGroupConfig, vnetId: ResourceId) =
        { state with
            VirtualNetwork = Some(Managed vnetId)
        }

    member _.VNetId(state: ContainerGroupConfig, vnetName: string) =
        { state with
            VirtualNetwork = Some(Managed(virtualNetworks.resourceId (ResourceName vnetName)))
        }

    member _.VNetId(state: ContainerGroupConfig, vnetName: ResourceName) =
        { state with
            VirtualNetwork = Some(Managed(virtualNetworks.resourceId vnetName))
        }

    /// Sets the name of a virtual network where this container group should be attached.
    [<CustomOperation "link_to_vnet">]
    member _.LinkToVNetId(state: ContainerGroupConfig, vnetId: ResourceId) =
        { state with
            VirtualNetwork = Some(Unmanaged vnetId)
        }

    member _.LinkToVNetId(state: ContainerGroupConfig, vnetName: string) =
        { state with
            VirtualNetwork = Some(Unmanaged(virtualNetworks.resourceId (ResourceName vnetName)))
        }

    member _.LinkToVNetId(state: ContainerGroupConfig, vnetName: ResourceName) =
        { state with
            VirtualNetwork = Some(Unmanaged(virtualNetworks.resourceId vnetName))
        }

    [<CustomOperation "subnet">]
    member _.Subnet(state: ContainerGroupConfig, subnetName: string) =
        { state with
            SubnetName = Some(ResourceName subnetName)
        }

    member _.Subnet(state: ContainerGroupConfig, subnetName: ResourceName) =
        { state with
            SubnetName = Some subnetName
        }

    /// Adds a UDP port to be externally accessible
    [<CustomOperation "add_udp_port">]
    member this.AddUdpPort(state: ContainerGroupConfig, port) = this.AddPort(state, UDP, port)

    /// Adds container image registry credentials for images in this container group.
    [<CustomOperation "add_registry_credentials">]
    member _.AddRegistryCredentials(state: ContainerGroupConfig, credentials) =
        { state with
            ImageRegistryCredentials =
                state.ImageRegistryCredentials
                @ (credentials |> List.map ImageRegistryAuthentication.Credential)
        }

    /// References one or more container image registries to get credentials for images in this container group.
    [<CustomOperation "reference_registry_credentials">]
    member _.ReferenceRegistryCredentials(state: ContainerGroupConfig, resourceIds) =
        { state with
            ImageRegistryCredentials =
                state.ImageRegistryCredentials
                @ (resourceIds |> List.map ImageRegistryAuthentication.ListCredentials)
        }

    /// Adds container image registry managed identity credentials for images in this container group.
    [<CustomOperation "add_managed_identity_registry_credentials">]
    member _.ManagedIdentityRegistryCredentials(state: ContainerGroupConfig, credentials) =
        { state with
            ImageRegistryCredentials =
                state.ImageRegistryCredentials
                @ (credentials |> List.map ImageRegistryAuthentication.ManagedIdentityCredential)
        }

    /// Adds a collection of init containers to this group that run once on startup before other containers in the group.
    [<CustomOperation "add_init_containers">]
    member _.AddInitContainers(state: ContainerGroupConfig, initContainers) =
        { state with
            InitContainers = state.InitContainers @ (Seq.toList initContainers)
        }

    /// Adds a collection of container instances to this group
    [<CustomOperation "add_instances">]
    member _.AddInstances(state: ContainerGroupConfig, instances) =
        { state with
            Instances = state.Instances @ (Seq.toList instances)
        }

    /// Adds volumes to the container group so they can be mounted on containers.
    [<CustomOperation "add_volumes">]
    member _.AddVolumes(state: ContainerGroupConfig, volumes) =
        let newVolumes = volumes |> Map.ofSeq

        let updatedVolumes =
            state.Volumes
            |> Map.fold (fun current key vol -> Map.add key vol current) newVolumes

        { state with Volumes = updatedVolumes }

    /// Specify the availability zone for the container group.
    [<CustomOperation "availability_zone">]
    member _.AvailabilityZones(state: ContainerGroupConfig, zone: string) =
        { state with
            AvailabilityZone = Some zone
        }

    [<CustomOperation "diagnostics_workspace">]
    member _.EnableDiagnostics(state: ContainerGroupConfig, logType: LogType, workspaceBuilder: WorkspaceConfig) =
        { state with
            Diagnostics =
                {
                    LogType = logType
                    Workspace =
                        LogAnalyticsWorkspace.WorkspaceResourceId(Managed((workspaceBuilder :> IBuilder).ResourceId))
                }
                |> Some
        }

    member _.EnableDiagnostics(state: ContainerGroupConfig, logType: LogType, workspaceResourceId: ResourceId) =
        { state with
            Diagnostics =
                {
                    LogType = logType
                    Workspace = LogAnalyticsWorkspace.WorkspaceResourceId(Managed(workspaceResourceId))
                }
                |> Some
        }

    [<CustomOperation "diagnostics_workspace_key">]
    member _.EnableDiagnosticsWorkspace
        (
            state: ContainerGroupConfig,
            logType: LogType,
            workspaceId: string,
            workspaceKey: string
        ) =
        { state with
            Diagnostics =
                {
                    LogType = logType
                    Workspace = LogAnalyticsWorkspace.WorkspaceKey(workspaceId, workspaceKey)
                }
                |> Some
        }

    [<CustomOperation "link_to_diagnostics_workspace">]
    member _.LinkToDiagnosticsWorkspace
        (
            state: ContainerGroupConfig,
            logType: LogType,
            workspaceResourceId: ResourceId
        ) =
        { state with
            Diagnostics =
                {
                    LogType = logType
                    Workspace = LogAnalyticsWorkspace.WorkspaceResourceId(Unmanaged(workspaceResourceId))
                }
                |> Some
        }

    /// Specify DNS nameservers for the containers in the container group.
    [<CustomOperation "dns_nameservers">]
    member _.DnsNameServers(state: ContainerGroupConfig, nameServers: string list) =
        let dns =
            match state.DnsConfig with
            | None ->
                {
                    NameServers = nameServers
                    Options = []
                    SearchDomains = []
                }
            | Some dnsConfig ->
                { dnsConfig with
                    NameServers = nameServers
                }

        { state with DnsConfig = Some dns }

    /// Specify DNS options (e.g. 'ndots:2') for the containers in the container group.
    [<CustomOperation "dns_options">]
    member _.DnsOptions(state: ContainerGroupConfig, options: string list) =
        let dns =
            match state.DnsConfig with
            | None ->
                {
                    NameServers = []
                    Options = options
                    SearchDomains = []
                }
            | Some dnsConfig -> { dnsConfig with Options = options }

        { state with DnsConfig = Some dns }

    /// Specify DNS search domains for the containers in the container group.
    [<CustomOperation "dns_search_domains">]
    member _.DnsSearchDomains(state: ContainerGroupConfig, searchDomains: string list) =
        let dns =
            match state.DnsConfig with
            | None ->
                {
                    NameServers = []
                    Options = []
                    SearchDomains = searchDomains
                }
            | Some dnsConfig ->
                { dnsConfig with
                    SearchDomains = searchDomains
                }

        { state with DnsConfig = Some dns }

    interface IIdentity<ContainerGroupConfig> with
        member _.Add state updater =
            { state with
                Identity = updater state.Identity
            }

    interface ITaggable<ContainerGroupConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    interface IDependable<ContainerGroupConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

/// Creates an image registry credential with a generated SecureParameter for the password.
let registry (server: string) (username: string) (managedIdentity: ManagedIdentity) =
    {
        Server = server
        Username = username
        Password = SecureParameter $"{server}-password"
        Identity = managedIdentity
    }

type ContainerInstanceBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Image = None
            Command = List.empty
            Ports = Map.empty
            Cpu = 1.0
            Memory = 1.5<Gb>
            Gpu = None
            EnvironmentVariables = Map.empty
            LivenessProbe = None
            ReadinessProbe = None
            VolumeMounts = Map.empty
        }

    /// Sets the name of the container instance.
    [<CustomOperation "name">]
    member _.Name(state: ContainerInstanceConfig, name) = { state with Name = name }

    member this.Name(state: ContainerInstanceConfig, name) = this.Name(state, ResourceName name)

    /// Sets the image of the container instance as a docker image tag.
    [<CustomOperation "image">]
    member _.Image(state: ContainerInstanceConfig, image: string) =
        { state with
            Image = Some(Containers.DockerImage.Parse image)
        }

    /// Sets the image to a private docker image.
    [<CustomOperation "private_docker_image">]
    member _.PrivateDockerImage
        (
            state: ContainerInstanceConfig,
            registry: string,
            containerName: string,
            version: string
        ) =
        { state with
            Image =
                Containers.DockerImage.PrivateImage(registry, containerName, Some version)
                |> Some
        }

    /// Sets the image to a public docker image.
    [<CustomOperation "public_docker_image">]
    member _.PublicDockerImage(state: ContainerInstanceConfig, containerName: string, version: string) =
        { state with
            Image = Containers.DockerImage.PublicImage(containerName, Some version) |> Some
        }

    static member private AddPorts(state: ContainerInstanceConfig, accessibility, ports) =
        { state with
            Ports = ports |> Seq.fold (fun all port -> all.Add(port, accessibility)) state.Ports
        }

    /// Sets the ports the container instance exposes. These will automatically be applied to the container group.
    [<CustomOperation "add_public_ports">]
    member _.PublicPorts(state: ContainerInstanceConfig, ports) =
        ContainerInstanceBuilder.AddPorts(state, PublicPort, ports)

    /// Sets the ports the container instance exposes. These will automatically be applied to the container group.
    [<CustomOperation "add_internal_ports">]
    member _.InternalPorts(state: ContainerInstanceConfig, ports) =
        ContainerInstanceBuilder.AddPorts(state, InternalPort, ports)

    /// Sets the ports the container instance exposes. These will automatically be applied to the container group.
    [<CustomOperation "add_ports">]
    member _.Ports(state: ContainerInstanceConfig, accessibility, ports) =
        ContainerInstanceBuilder.AddPorts(state, accessibility, ports)

    /// Sets the maximum CPU cores the container instance may use
    [<CustomOperationAttribute "cpu_cores">]
    member _.CpuCount(state: ContainerInstanceConfig, cpuCount: float) = { state with Cpu = cpuCount }

    member _.CpuCount(state: ContainerInstanceConfig, cpuCount: int) = { state with Cpu = float (cpuCount) }

    /// Sets the maximum gigabytes of memory the container instance may use
    [<CustomOperationAttribute "memory">]
    member _.Memory(state: ContainerInstanceConfig, memory) = { state with Memory = memory }

    /// Enables container instances with gpus
    [<CustomOperationAttribute "gpu">]
    member _.Gpu(state: ContainerInstanceConfig, (gpu: ContainerGpuConfig)) =
        { state with
            Gpu = Some { Count = gpu.Count; Sku = gpu.Sku }
        }

    [<CustomOperation "env_vars">]
    member _.EnvironmentVariables(state: ContainerInstanceConfig, envVars) =
        { state with
            EnvironmentVariables = Map.ofList envVars
        }

    member this.EnvironmentVariables(state, envVars) =
        this.EnvironmentVariables(state, envVars |> List.map (fun (k, v) -> k, EnvValue v))

    /// Adds a volume mount to the container
    [<CustomOperation "add_volume_mount">]
    member _.AddVolumeMount(state: ContainerInstanceConfig, volumeName, mountPath) =
        { state with
            VolumeMounts = state.VolumeMounts |> Map.add volumeName mountPath
        }

    /// Adds commands to execute within the container instance
    [<CustomOperation "command_line">]
    member _.CommandLine(state: ContainerInstanceConfig, command) =
        { state with
            Command = state.Command @ command
        }

    /// Set readiness and liveness probes on the container.
    [<CustomOperation "probes">]
    member _.Probes(state: ContainerInstanceConfig, probes: (ContainerProbeConfig) seq) =
        { state with
            LivenessProbe =
                probes
                |> Seq.tryFind (fun p -> p.ProbeType = ContainerProbeType.LivenessProbe)
                |> Option.map (fun p -> p.Probe)
            ReadinessProbe =
                probes
                |> Seq.tryFind (fun p -> p.ProbeType = ContainerProbeType.ReadinessProbe)
                |> Option.map (fun p -> p.Probe)
        }

type ProbeBuilder(probeType: ContainerProbeType) =
    member _.Yield _ =
        {
            ProbeType = probeType
            Probe =
                {
                    Exec = []
                    HttpGet = None
                    InitialDelaySeconds = None
                    PeriodSeconds = None
                    FailureThreshold = None
                    SuccessThreshold = None
                    TimeoutSeconds = None
                }
        }

    /// The URI for a GET request for a health or readiness check on this container. The hostname in the URI is ignored.
    [<CustomOperation "http">]
    member _.HttpGet(state: (ContainerProbeConfig), uri: string) =
        { state with
            Probe =
                { state.Probe with
                    HttpGet = uri |> System.Uri |> Some
                }
        }

    /// A command to execute on this container to check its health or readiness.
    [<CustomOperation "exec">]
    member _.Exec(state: (ContainerProbeConfig), commands: string list) =
        { state with
            Probe = { state.Probe with Exec = commands }
        }

    member _.Exec(state: (ContainerProbeConfig), command: string) =
        { state with
            Probe = { state.Probe with Exec = [ command ] }
        }

    /// The probe will not run until this delay after container startup. Default is 0 - runs immediately.
    [<CustomOperation "initial_delay_seconds">]
    member _.InitialDelay(state: (ContainerProbeConfig), delay: int) =
        { state with
            Probe =
                { state.Probe with
                    InitialDelaySeconds = delay |> Some
                }
        }

    /// How often to execute the probe against the container - default is 10 seconds.
    [<CustomOperation "period_seconds">]
    member _.PeriodSeconds(state: (ContainerProbeConfig), delay: int) =
        { state with
            Probe =
                { state.Probe with
                    PeriodSeconds = delay |> Some
                }
        }

    /// Number of failures before this container is considered unhealthy - default is 3.
    [<CustomOperation "failure_threshold">]
    member _.FailureThreshold(state: (ContainerProbeConfig), delay: int) =
        { state with
            Probe =
                { state.Probe with
                    FailureThreshold = delay |> Some
                }
        }

    /// Number of successes before this container is considered healthy - default is 1.
    [<CustomOperation "success_threshold">]
    member _.SuccessThreshold(state: (ContainerProbeConfig), delay: int) =
        { state with
            Probe =
                { state.Probe with
                    SuccessThreshold = delay |> Some
                }
        }

    /// Number of seconds for the probe to run before failing due to a timeout - default is 1 second.
    [<CustomOperation "timeout_seconds">]
    member _.TimeoutSeconds(state: (ContainerProbeConfig), delay: int) =
        { state with
            Probe =
                { state.Probe with
                    TimeoutSeconds = delay |> Some
                }
        }

let liveness = ProbeBuilder(LivenessProbe)
let readiness = ProbeBuilder(ReadinessProbe)

[<System.Obsolete "Compatibility only due to spelling error - please use 'liveness'">]
let liveliness = ProbeBuilder(LivenessProbe)

type GpuBuilder() =
    member _.Yield _ : ContainerGpuConfig = { Count = 1; Sku = Gpu.Sku.K80 }

    [<CustomOperation "count">]
    member _.Count(state: ContainerGpuConfig, count) = { state with Count = count }

    [<CustomOperation "sku">]
    member _.Sku(state: ContainerGpuConfig, sku) = { state with Sku = sku }

let containerInstanceGpu = GpuBuilder()

type InitContainerBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Image = None
            Command = List.empty
            EnvironmentVariables = Map.empty
            VolumeMounts = Map.empty
        }

    /// Sets the name of the init container.
    [<CustomOperation "name">]
    member _.Name(state: InitContainerConfig, name) = { state with Name = name }

    member this.Name(state: InitContainerConfig, name) = this.Name(state, ResourceName name)

    /// Sets the image of the init container.
    [<CustomOperation "image">]
    member _.Image(state: InitContainerConfig, image: string) =
        { state with
            Image = Some(Containers.DockerImage.Parse image)
        }

    /// Sets the image to a private docker image.
    [<CustomOperation "private_docker_image">]
    member _.PrivateDockerImage(state: InitContainerConfig, registry: string, containerName: string, version: string) =
        { state with
            Image =
                Containers.DockerImage.PrivateImage(registry, containerName, Some version)
                |> Some
        }

    /// Sets the image to a public docker image.
    [<CustomOperation "public_docker_image">]
    member _.PublicDockerImage(state: InitContainerConfig, containerName: string, version: string) =
        { state with
            Image = Containers.DockerImage.PublicImage(containerName, Some version) |> Some
        }

    /// Sets the environment variables for the init container.
    [<CustomOperation "env_vars">]
    member _.EnvironmentVariables(state: InitContainerConfig, envVars) =
        { state with
            EnvironmentVariables = Map.ofList envVars
        }

    member this.EnvironmentVariables(state, envVars) =
        this.EnvironmentVariables(state, envVars |> List.map (fun (k, v) -> k, EnvValue v))

    /// Adds a volume mount to the init container
    [<CustomOperation "add_volume_mount">]
    member _.AddVolumeMount(state: InitContainerConfig, volumeName, mountPath) =
        { state with
            VolumeMounts = state.VolumeMounts |> Map.add volumeName mountPath
        }

    /// Adds commands to execute within the init container
    [<CustomOperation "command_line">]
    member _.CommandLine(state: InitContainerConfig, command) =
        { state with
            Command = state.Command @ command
        }

let containerGroup = ContainerGroupBuilder()
let containerInstance = ContainerInstanceBuilder()
let initContainer = InitContainerBuilder()

type NetworkProfileBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            ContainerNetworkInterfaceConfigurations = []
            VirtualNetwork = Managed(virtualNetworks.resourceId ResourceName.Empty)
            Tags = Map.empty
        }

    /// Sets the name of the network profile instance
    [<CustomOperation "name">]
    member _.Name(state: NetworkProfileConfig, name) = { state with Name = ResourceName name }

    /// Sets a single target subnet for the network profile (typical case of single subnet)
    [<CustomOperation "subnet">]
    member _.SubnetName(state: NetworkProfileConfig, subnet) =
        { state with
            ContainerNetworkInterfaceConfigurations =
                [
                    {
                        IpConfigs =
                            [
                                {
                                    Name = ResourceName "ipconfig"
                                    Subnet = subnet
                                }
                            ]
                    }
                ]
        }

    /// Sets a single target named ip configuration for the network profile (typical case of single subnet)
    [<CustomOperation "ip_config">]
    member _.IpConfig(state: NetworkProfileConfig, ipConfigName: string, subnetName: string) =
        { state with
            ContainerNetworkInterfaceConfigurations =
                [
                    {
                        IpConfigs =
                            [
                                {
                                    Name = ResourceName ipConfigName
                                    Subnet = subnetName
                                }
                            ]
                    }
                ]
        }

    /// Sets multiple subnet IP configs for the network profile to connect to multiple subnets.
    [<CustomOperation "add_ip_configs">]
    member _.AddIpConfigs(state: NetworkProfileConfig, configs) =
        { state with
            ContainerNetworkInterfaceConfigurations = state.ContainerNetworkInterfaceConfigurations @ configs
        }

    /// Sets the virtual network for the profile
    [<CustomOperation "vnet">]
    member _.VirtualNetwork(state: NetworkProfileConfig, vnet) =
        { state with
            VirtualNetwork = Managed(virtualNetworks.resourceId (ResourceName vnet))
        }

    /// Links to an existing vnet.
    [<CustomOperation "link_to_vnet">]
    member _.LinkToVirtualNetwork(state: NetworkProfileConfig, vnet) =
        { state with
            VirtualNetwork = Unmanaged(virtualNetworks.resourceId (ResourceName vnet))
        }

    member _.LinkToVirtualNetwork(state: NetworkProfileConfig, resourceId) =
        { state with
            VirtualNetwork = Unmanaged resourceId
        }

    interface ITaggable<NetworkProfileConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

let networkProfile = NetworkProfileBuilder()
