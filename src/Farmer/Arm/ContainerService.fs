[<AutoOpen>]
module Farmer.Arm.ContainerService

open Farmer
open Farmer.Identity
open Farmer.ContainerService
open Farmer.Vm

let managedClusters =
    ResourceType("Microsoft.ContainerService/managedClusters", "2024-02-01")

module AutoUpgradeProfiles =
    type AutoUpgradeProfile = {
        AutoUpgradeChannel: AutoUpgradeChannel
        NodeOSUpgradeChannel: NodeOSUpgradeChannel
    } with

        static member Default = {
            AutoUpgradeChannel = AutoUpgradeChannel.Stable
            NodeOSUpgradeChannel = NodeOSUpgradeChannel.NodeImage
        }

    let toArmJson (config: AutoUpgradeProfile) = {|
        upgradeChannel = config.AutoUpgradeChannel.ArmValue
        nodeOSUpgradeChannel = config.NodeOSUpgradeChannel.ArmValue
    |}

module AddonProfiles =
    type AciConnectorLinux = {
        Status: FeatureFlag
    } with

        member internal this.ToArmJson = {| enabled = this.Status.AsBoolean |}

    type HttpApplicationRouting = {
        Status: FeatureFlag
    } with

        member internal this.ToArmJson = {| enabled = this.Status.AsBoolean |}

    type IngressApplicationGateway = {
        Status: FeatureFlag
        ApplicationGatewayId: ResourceId
        Identity: UserAssignedIdentity option
    } with

        member internal this.ToArmJson = {|
            enabled = this.Status.AsBoolean
            config =
                match this.Status with
                | Disabled -> Unchecked.defaultof<_>
                | Enabled -> {|
                    applicationGatewayId = this.ApplicationGatewayId.Eval()
                  |}
            identity =
                match this.Status, this.Identity with
                | Disabled, _
                | Enabled, None -> Unchecked.defaultof<_>
                | Enabled, Some userIdentity -> {|
                    clientId = userIdentity.ClientId.Eval()
                    objectId = userIdentity.PrincipalId.ArmExpression.Eval()
                    resourceId = this.ApplicationGatewayId.Eval()
                  |}
        |}

    type KubeDashboard = {
        Status: FeatureFlag
    } with

        member internal this.ToArmJson = {| enabled = this.Status.AsBoolean |}

    type OmsAgent = {
        Status: FeatureFlag
        LogAnalyticsWorkspaceId: ResourceId option
    } with

        member internal this.ToArmJson = {|
            enabled = this.Status.AsBoolean
            config =
                match this.Status, this.LogAnalyticsWorkspaceId with
                | Disabled, _
                | Enabled, None -> Unchecked.defaultof<_>
                | Enabled, Some resId -> {|
                    logAnalyticsWorkspaceResourceID = resId.Eval()
                  |}
        |}

    type AzureKeyvaultSecretsProvider = {
        Status: FeatureFlag
        EnableSecretRotation: bool option
        RotationPollInterval: string option
    } with

        member internal this.ToArmJson = {|
            enabled = this.Status.AsBoolean
            config =
                match this.Status with
                | Disabled -> Unchecked.defaultof<_>
                | Enabled -> {|
                    enableSecretRotation = this.EnableSecretRotation |> Option.map string |> Option.defaultValue "false"
                    rotationPollInterval = this.RotationPollInterval |> Option.defaultValue "2m"
                  |}
        |}

    type AddonProfileConfig = {
        AciConnectorLinux: AciConnectorLinux option
        HttpApplicationRouting: HttpApplicationRouting option
        IngressApplicationGateway: IngressApplicationGateway option
        KubeDashboard: KubeDashboard option
        OmsAgent: OmsAgent option
        AzureKeyvaultSecretsProvider: AzureKeyvaultSecretsProvider option
    } with

        static member Default = {
            AciConnectorLinux = None
            HttpApplicationRouting = None
            IngressApplicationGateway = None
            KubeDashboard = None
            OmsAgent = None
            AzureKeyvaultSecretsProvider = None
        }

    let toArmJson (config: AddonProfileConfig) = {|
        aciConnectorLinux =
            match config.AciConnectorLinux with
            | None -> Unchecked.defaultof<_>
            | Some aciConn -> aciConn.ToArmJson
        httpApplicationRouting =
            match config.HttpApplicationRouting with
            | None -> Unchecked.defaultof<_>
            | Some routing -> routing.ToArmJson
        ingressApplicationGateway =
            match config.IngressApplicationGateway with
            | None -> Unchecked.defaultof<_>
            | Some appGateway -> appGateway.ToArmJson
        kubeDashboard =
            match config.KubeDashboard with
            | None -> Unchecked.defaultof<_>
            | Some dashboard -> dashboard.ToArmJson
        omsagent =
            match config.OmsAgent with
            | None -> Unchecked.defaultof<_>
            | Some oms -> oms.ToArmJson
        azureKeyvaultSecretsProvider =
            match config.AzureKeyvaultSecretsProvider with
            | None -> Unchecked.defaultof<_>
            | Some secretsProvider -> secretsProvider.ToArmJson
    |}

type AgentPoolMode =
    | System
    | User

/// Additional identity settings for the managed cluster, such as the identity for kubelet to pull container images.
type ManagedClusterIdentityProfile = {
    KubeletIdentity: LinkedResource option
} with

    member internal this.ToArmJson = {|
        kubeletIdentity =
            match this.KubeletIdentity with
            | Some kubeletIdentity -> {|
                resourceId = kubeletIdentity.ResourceId.Eval()
                clientId =
                    ArmExpression
                        .reference(kubeletIdentity.ResourceId.Type, kubeletIdentity.ResourceId)
                        .Map(fun r -> r + ".clientId")
                        .Eval()
                objectId =
                    ArmExpression
                        .reference(kubeletIdentity.ResourceId.Type, kubeletIdentity.ResourceId)
                        .Map(fun r -> r + ".principalId")
                        .Eval()
              |}
            | None -> Unchecked.defaultof<_>
    |}

    member internal this.Dependencies =
        match this.KubeletIdentity with
        | Some(Managed kubeletIdentity) -> [ kubeletIdentity ]
        | Some(Unmanaged _)
        | None -> []

type OidcIssuerProfile = { Enabled: FeatureFlag }

type KubeStateMetrics = {
    MetricLabelsAllowList: string option
    MetricAnnotationsAllowList: string option
} with

    static member Default = {
        MetricLabelsAllowList = None
        MetricAnnotationsAllowList = None
    }

type AzureMonitorProfile = {
    Metrics: {|
        Enabled: FeatureFlag
        KubeStateMetrics: KubeStateMetrics option
    |}
}

type SecurityProfileSettings = {
    Defender:
        {|
            SecurityMonitoring: FeatureFlag
            LogAnalyticsResourceId: ResourceId option
        |} option
    // intervalHours: minimum 24 hours, default one week, maximum 3 months
    ImageCleanerSettings:
        {|
            Enabled: FeatureFlag
            Interval: System.TimeSpan
        |} option
    WorkloadIdentity: FeatureFlag option
} with

    static member Default = {
        Defender = None
        ImageCleanerSettings = None
        WorkloadIdentity = None
    }

type ScaleDownMode =
    | Delete
    | Deallocate

type KubernetesVersion = {
    Major: int
    Minor: int
    //Latest patch version selected if not specified
    Patch: int option
} with

    static member Create(version: string) =
        let parts = version.Split('.')

        match parts with
        | [| majorString; minorString |] ->
            match System.Int32.TryParse(majorString), System.Int32.TryParse(minorString) with
            | (false, _), _ -> Result.Error $"Invalid major version: {majorString}."
            | _, (false, _) -> Result.Error $"Invalid minor version: {minorString}."
            | (true, major), (true, minor) ->
                {
                    Major = major
                    Minor = minor
                    Patch = None
                }
                |> Result.Ok
        | [| major; minor; patch |] ->
            match System.Int32.TryParse(major), System.Int32.TryParse(minor), System.Int32.TryParse(patch) with
            | (false, _), _, _ -> Result.Error $"Invalid major version: {major}."
            | _, (false, _), _ -> Result.Error $"Invalid minor version: {minor}."
            | _, _, (false, _) -> Result.Error $"Invalid patch version: {patch}."
            | (true, major), (true, minor), (true, patch) ->
                {
                    Major = major
                    Minor = minor
                    Patch = Some patch
                }
                |> Result.Ok
        | _ -> Result.Error $"Invalid Kubernetes version format: {version}. Expected format is 'major.minor[.patch]'."

    member this.Value =
        match this.Patch with
        | Some patch -> $"%i{this.Major}.%i{this.Minor}.%i{patch}"
        | None -> $"%i{this.Major}.%i{this.Minor}"

type ManagedCluster = {
    Name: ResourceName
    Sku: ContainerServiceSku
    Location: Location
    Dependencies: ResourceId Set
    /// Dependencies that are expressed in ARM functions instead of a resource Id
    DependencyExpressions: ArmExpression Set
    AddOnProfiles: AddonProfiles.AddonProfileConfig option
    AgentPoolProfiles:
        {|
            Name: ResourceName
            Count: int
            EnableFIPS: FeatureFlag option
            MaxPods: int option
            Mode: AgentPoolMode
            OsDiskSize: int<Gb>
            OsType: OS
            OsSKU: string option
            VmSize: VMSize
            AvailabilityZones: ZoneSelection
            VirtualNetworkName: ResourceName option
            SubnetName: ResourceName option
            PodSubnetName: ResourceName option
            AutoscaleSetting: FeatureFlag option
            ScaleDownMode: ScaleDownMode option
            MinCount: int option
            MaxCount: int option
            NodeTaints: string list option
        |} list
    DnsPrefix: string
    EnableRBAC: bool
    KubernetesVersion: KubernetesVersion option
    AutoUpgradeProfile: AutoUpgradeProfiles.AutoUpgradeProfile option
    Identity: ManagedIdentity
    IdentityProfile: ManagedClusterIdentityProfile option
    ApiServerAccessProfile:
        {|
            AuthorizedIPRanges: string list
            EnablePrivateCluster: bool option
        |} option
    LinuxProfile:
        {|
            AdminUserName: string
            PublicKeys: string list
        |} option
    NetworkProfile:
        {|
            NetworkPlugin: ContainerService.NetworkPlugin option
            DnsServiceIP: System.Net.IPAddress option
            LoadBalancerSku: LoadBalancer.Sku option
            ServiceCidr: IPAddressCidr option
        |} option
    OidcIssuerProfile: OidcIssuerProfile option
    AzureMonitorProfile: AzureMonitorProfile option
    SecurityProfile: SecurityProfileSettings option
    WindowsProfile:
        {|
            AdminUserName: string
            AdminPassword: SecureParameter
        |} option
    ServicePrincipalProfile: {|
        ClientId: string
        ClientSecret: SecureParameter option
    |}
    NodeResourceGroup: ResourceName option
} with

    interface IParameters with
        member this.SecureParameters = [
            yield! this.ServicePrincipalProfile.ClientSecret |> Option.mapList id
            yield! this.WindowsProfile |> Option.mapList (fun wp -> wp.AdminPassword)
        ]

    interface IArmResource with
        member this.ResourceId = managedClusters.resourceId this.Name

        member this.JsonModel =
            let dependencies =
                [
                    this.AgentPoolProfiles
                    |> List.choose (fun pool -> pool.VirtualNetworkName)
                    |> List.map virtualNetworks.resourceId
                    this.Identity.Dependencies
                    this.IdentityProfile
                    |> Option.map (fun identityProfile -> identityProfile.Dependencies)
                    |> Option.defaultValue []
                ]
                |> Seq.concat
                |> Set.ofSeq
                |> Set.union this.Dependencies

            {|
                managedClusters.Create(this.Name, this.Location) with
                    dependsOn =
                        [
                            dependencies |> Seq.map (fun r -> r.Eval())
                            this.DependencyExpressions |> Seq.map (fun r -> r.Eval())
                        ]
                        |> Seq.concat
                    sku = {|
                        name = this.Sku.Name.ArmValue
                        tier = this.Sku.Tier.ArmValue
                    |}
                    identity = // If using MSI but no identity was set, then enable the system identity like the CLI
                        if
                            this.ServicePrincipalProfile.ClientId = "msi"
                            && this.Identity.SystemAssigned = FeatureFlag.Disabled
                            && this.Identity.UserAssigned.Length = 0
                        then
                            {
                                SystemAssigned = Enabled
                                UserAssigned = []
                            }
                                .ToArmJson
                        else
                            this.Identity.ToArmJson
                    properties = {|
                        addonProfiles =
                            this.AddOnProfiles
                            |> Option.map AddonProfiles.toArmJson
                            |> Option.defaultValue Unchecked.defaultof<_>
                        agentPoolProfiles =
                            this.AgentPoolProfiles
                            |> List.mapi (fun idx agent -> {|
                                name =
                                    if agent.Name = ResourceName.Empty then
                                        $"nodepool{idx + 1}"
                                    else
                                        agent.Name.Value.ToLowerInvariant()
                                count = agent.Count
                                enableFIPS = agent.EnableFIPS |> Option.mapBoxed _.AsBoolean
                                maxPods = agent.MaxPods |> Option.toNullable
                                mode = agent.Mode |> string
                                osDiskSizeGB = agent.OsDiskSize
                                osType = string agent.OsType
                                osSKU = agent.OsSKU
                                vmSize = agent.VmSize.ArmValue
                                availabilityZones = agent.AvailabilityZones.ArmValue
                                vnetSubnetID =
                                    match agent.VirtualNetworkName, agent.SubnetName with
                                    | Some vnet, Some subnet -> subnets.resourceId(vnet, subnet).Eval()
                                    | _ -> null
                                podSubnetID =
                                    match agent.VirtualNetworkName, agent.PodSubnetName with
                                    | Some vnet, Some pod_subnet -> subnets.resourceId(vnet, pod_subnet).Eval()
                                    | _ -> null
                                enableAutoScaling = agent.AutoscaleSetting |> Option.mapBoxed _.AsBoolean
                                scaleDownMode =
                                    match agent.ScaleDownMode with
                                    | Some scaledownmode -> string scaledownmode
                                    | _ -> null
                                minCount = agent.MinCount |> Option.toNullable
                                maxCount = agent.MaxCount |> Option.toNullable
                                nodeTaints = agent.NodeTaints
                            |})
                        dnsPrefix = this.DnsPrefix
                        enableRBAC = this.EnableRBAC
                        kubernetesVersion =
                            match this.KubernetesVersion with
                            | Some version -> version.Value
                            | None -> null
                        autoUpgradeProfile = this.AutoUpgradeProfile |> Option.map AutoUpgradeProfiles.toArmJson
                        identityProfile =
                            match this.IdentityProfile with
                            | Some identityProfile -> identityProfile.ToArmJson
                            | None -> Unchecked.defaultof<_>
                        apiServerAccessProfile =
                            match this.ApiServerAccessProfile with
                            | Some apiServerProfile -> {|
                                authorizedIPRanges = apiServerProfile.AuthorizedIPRanges
                                enablePrivateCluster =
                                    apiServerProfile.EnablePrivateCluster |> Option.map box |> Option.toObj
                              |}
                            | None -> Unchecked.defaultof<_>
                        linuxProfile =
                            match this.LinuxProfile with
                            | Some linuxProfile -> {|
                                adminUsername = linuxProfile.AdminUserName
                                ssh = {|
                                    publicKeys = linuxProfile.PublicKeys |> List.map (fun k -> {| keyData = k |})
                                |}
                              |}
                            | None -> Unchecked.defaultof<_>
                        networkProfile =
                            match this.NetworkProfile with
                            | Some networkProfile -> {|
                                dnsServiceIP = networkProfile.DnsServiceIP |> Option.map string |> Option.toObj
                                loadBalancerSku =
                                    networkProfile.LoadBalancerSku
                                    |> Option.map (fun sku -> sku.ArmValue)
                                    |> Option.toObj
                                networkPlugin =
                                    networkProfile.NetworkPlugin
                                    |> Option.map (fun plugin -> plugin.ArmValue)
                                    |> Option.toObj
                                serviceCidr =
                                    networkProfile.ServiceCidr |> Option.map IPAddressCidr.format |> Option.toObj
                              |}
                            | None -> Unchecked.defaultof<_>
                        oidcIssuerProfile =
                            match this.OidcIssuerProfile with
                            | None -> Unchecked.defaultof<_>
                            | Some oidc -> {| enabled = oidc.Enabled.AsBoolean |}
                        azureMonitorProfile =
                            match this.AzureMonitorProfile with
                            | None -> Unchecked.defaultof<_>
                            | Some monitorProfile -> {|
                                metrics = {|
                                    enabled = monitorProfile.Metrics.Enabled.AsBoolean
                                    kubeStateMetrics =
                                        monitorProfile.Metrics.KubeStateMetrics
                                        |> Option.map (fun kubeStateMetrics -> {|
                                            metricLabelsAllowList = kubeStateMetrics.MetricLabelsAllowList
                                            metricAnnotationsAllowList = kubeStateMetrics.MetricAnnotationsAllowList
                                        |})
                                |}
                              |}
                        securityProfile =
                            match this.SecurityProfile with
                            | None -> Unchecked.defaultof<_>
                            | Some profile -> {|
                                defender =
                                    profile.Defender
                                    |> Option.map (fun defender -> {|
                                        logAnalyticsWorkspaceResourceId =
                                            defender.LogAnalyticsResourceId
                                            |> Option.map (_.ArmExpression.Eval())
                                            |> Option.toObj
                                        securityMonitoring = {|
                                            enabled = defender.SecurityMonitoring.AsBoolean
                                        |}
                                    |})
                                    |> Option.defaultValue Unchecked.defaultof<_>
                                imageCleaner =
                                    profile.ImageCleanerSettings
                                    |> Option.map (fun imageCleaner -> {|
                                        enabled = imageCleaner.Enabled.AsBoolean
                                        intervalHours = imageCleaner.Interval.TotalHours
                                    |})
                                    |> Option.defaultValue Unchecked.defaultof<_>
                                workloadIdentity =
                                    profile.WorkloadIdentity
                                    |> Option.map (fun workloadIdentity -> {|
                                        enabled = workloadIdentity.AsBoolean
                                    |})
                                    |> Option.defaultValue Unchecked.defaultof<_>
                              |}
                        servicePrincipalProfile = {|
                            clientId = this.ServicePrincipalProfile.ClientId
                            secret =
                                this.ServicePrincipalProfile.ClientSecret
                                |> Option.map (fun clientSecret -> clientSecret.ArmExpression.Eval())
                                |> Option.toObj
                        |}
                        windowsProfile =
                            match this.WindowsProfile with
                            | Some winProfile -> {|
                                adminUsername = winProfile.AdminUserName
                                adminPassword = winProfile.AdminPassword.ArmExpression.Eval()
                              |}
                            | None -> Unchecked.defaultof<_>
                        nodeResourceGroup = this.NodeResourceGroup |> Option.map (fun rg -> rg.Value)
                    |}
            |}