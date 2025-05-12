[<AutoOpen>]
module Farmer.Arm.Web

open Farmer
open Farmer.Identity
open Farmer.WebApp
open System

let serverFarms = ResourceType("Microsoft.Web/serverfarms", "2018-02-01")
let sites = ResourceType("Microsoft.Web/sites", "2021-03-01")
let config = ResourceType("Microsoft.Web/sites/config", "2016-08-01")

let sourceControls =
    ResourceType("Microsoft.Web/sites/sourcecontrols", "2019-08-01")

let staticSites = ResourceType("Microsoft.Web/staticSites", "2019-12-01-preview")

let staticSitesConfig =
    ResourceType("Microsoft.Web/staticSites/config", "2020-09-01")

let siteExtensions =
    ResourceType("Microsoft.Web/sites/siteextensions", "2020-06-01")

let slots = ResourceType("Microsoft.Web/sites/slots", "2020-09-01")
let certificates = ResourceType("Microsoft.Web/certificates", "2019-08-01")

let hostNameBindings =
    ResourceType("Microsoft.Web/sites/hostNameBindings", "2020-12-01")

let virtualNetworkConnections =
    ResourceType("Microsoft.Web/sites/virtualNetworkConnections", "2021-03-01")

let siteFunctions = ResourceType("Microsoft.Web/sites/functions", "2021-03-01")

let slotsVirtualNetworkConnections =
    ResourceType("Microsoft.Web/sites/slots/virtualNetworkConnections", "2021-03-01")

let private mapOrNull f =
    Option.map (Map.toList >> List.map f)
    >> Option.defaultValue Unchecked.defaultof<_>

type ServerFarm = {
    Name: ResourceName
    Location: Location
    Sku: Sku
    WorkerSize: WorkerSize
    WorkerCount: int
    MaximumElasticWorkerCount: int option
    OperatingSystem: OS
    ZoneRedundant: FeatureFlag option
    Tags: Map<string, string>
} with

    member this.IsDynamic =
        match this.Sku, this.WorkerSize with
        | Isolated "Y1", Serverless -> true
        | _ -> false

    member this.Reserved =
        match this.OperatingSystem with
        | Linux -> true
        | Windows -> false

    member this.Kind =
        [
            match this.Sku with
            | Shared
            | Free
            | Basic _
            | Standard _
            | Premium _
            | PremiumV2 _
            | PremiumV3 _
            | Isolated _
            | Dynamic -> ()
            | ElasticPremium _ -> "elastic"

            match this.OperatingSystem with
            | Linux -> "linux"
            | Windows -> ()
        ]
        |> function
            | [] -> None
            | kinds -> kinds |> String.concat "," |> Some

    member this.Tier =
        match this.Sku with
        | Free -> "Free"
        | Shared -> "Shared"
        | Basic _ -> "Basic"
        | Standard _ -> "Standard"
        | Premium _ -> "Premium"
        | PremiumV2 _ -> "PremiumV2"
        | PremiumV3 _ -> "PremiumV3"
        | ElasticPremium _ -> "ElasticPremium"
        | Dynamic -> "Dynamic"
        | Isolated _ -> "Isolated"

    interface IArmResource with
        member this.ResourceId = serverFarms.resourceId this.Name

        member this.JsonModel = {|
            serverFarms.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {|
                    name =
                        match this.Sku with
                        | Free -> "F1"
                        | Shared -> "D1"
                        | Basic sku
                        | Standard sku
                        | Premium sku
                        | PremiumV2 sku
                        | PremiumV3 sku
                        | ElasticPremium sku
                        | Isolated sku -> sku
                        | Dynamic -> "Y1"
                    tier = this.Tier
                    size =
                        match this.WorkerSize with
                        | Small -> "0"
                        | Medium -> "1"
                        | Large -> "2"
                        | Serverless -> "Y1"
                    family = if this.IsDynamic then "Y" else null
                    capacity = if this.IsDynamic then 0 else this.WorkerCount
                |}
                properties = {|
                    name = this.Name.Value
                    computeMode = if this.IsDynamic then "Dynamic" else null
                    perSiteScaling = if this.IsDynamic then Nullable() else Nullable false
                    reserved = this.Reserved
                    maximumElasticWorkerCount = this.MaximumElasticWorkerCount |> Option.toNullable
                    zoneRedundant = this.ZoneRedundant |> Option.map (fun f -> f.AsBoolean) |> Option.toNullable
                |}
                kind = this.Kind |> Option.toObj
        |}

module ZipDeploy =
    open System.IO
    open System.IO.Compression

    type ZipDeploySlot =
        | ProductionSlot
        | NamedSlot of name: string

        member this.ToOption =
            match this with
            | ProductionSlot -> None
            | NamedSlot n -> Some n

    type ZipDeployTarget =
        | WebApp
        | FunctionApp

    type ZipDeployKind =
        | DeployFolder of string
        | DeployZip of string

        member this.Value =
            match this with
            | DeployFolder s
            | DeployZip s -> s

        /// Tries to create a ZipDeployKind from a string path.
        static member TryParse path =
            if (File.GetAttributes path).HasFlag FileAttributes.Directory then
                Some(DeployFolder path)
            else if Path.GetExtension path = ".zip" then
                Some(DeployZip path)
            else
                None

        /// Processes a ZipDeployKind and returns the filename of the zip file.
        /// If the ZipDeployKind is a DeployFolder, the folder will be zipped first and the generated zip file returned.
        member this.GetZipPath targetFolder =
            match this with
            | DeployFolder appFolder ->
                let packageFilename =
                    Path.Combine(targetFolder, (Path.GetFileName appFolder) + ".zip")

                File.Delete packageFilename
                ZipFile.CreateFromDirectory(appFolder, packageFilename)
                packageFilename
            | DeployZip zipFilePath -> zipFilePath

type SiteType =
    | Slot of ResourceName
    | Site of WebAppName

    member this.ResourceName =
        match this with
        | Slot r -> r
        | Site r -> r.ResourceName

    member this.ResourceType =
        match this with
        | Slot _ -> slots
        | Site _ -> sites

[<RequireQualifiedAccess>]
type FTPState =
    | AllAllowed
    | FtpsOnly
    | Disabled

type Site = {
    SiteType: SiteType
    Location: Location
    ServicePlan: ResourceId
    AppSettings: Map<string, Setting> option
    ConnectionStrings: Map<string, (Setting * ConnectionStringKind)> option
    AlwaysOn: bool
    WorkerProcess: Bitness option
    HTTPSOnly: bool
    FTPState: FTPState option
    HTTP20Enabled: bool option
    ClientAffinityEnabled: bool option
    WebSocketsEnabled: bool option
    Cors: Cors option
    Dependencies: ResourceId Set
    Kind: string
    Identity: Identity.ManagedIdentity
    KeyVaultReferenceIdentity: UserAssignedIdentity option
    LinuxFxVersion: string option
    AppCommandLine: string option
    NetFrameworkVersion: string option
    JavaVersion: string option
    JavaContainer: string option
    JavaContainerVersion: string option
    PhpVersion: string option
    PythonVersion: string option
    Tags: Map<string, string>
    Metadata: List<string * string>
    AutoSwapSlotName: string option
    ZipDeployPath: (string * ZipDeploy.ZipDeployTarget * ZipDeploy.ZipDeploySlot) option
    HealthCheckPath: string option
    IpSecurityRestrictions: IpSecurityRestriction list
    LinkToSubnet: SubnetReference option
    VirtualApplications: Map<string, VirtualApplication>
    FunctionAppScaleLimit: int option
} with

    /// Shorthand for SiteType.ResourceType
    member this.ResourceType = this.SiteType.ResourceType
    /// Shorthand for SiteType.ResourceName
    member this.Name = this.SiteType.ResourceName

    interface IParameters with
        member this.SecureParameters =
            let optMapToList map =
                map |> Option.defaultValue Map.empty |> Map.toList

            optMapToList this.AppSettings
            @ (optMapToList this.ConnectionStrings |> List.map (fun (k, (v, _)) -> k, v))
            |> List.choose (
                snd
                >> function
                    | ParameterSetting s -> Some s
                    | ExpressionSetting _
                    | LiteralSetting _ -> None
            )

    interface IPostDeploy with
        member this.Run resourceGroupName =
            match this with
            | {
                  ZipDeployPath = Some(path, target, slot)
                  SiteType = siteType
              } ->
                let path =
                    ZipDeploy.ZipDeployKind.TryParse path
                    |> Option.defaultWith (fun () ->
                        raiseFarmer $"Path '{path}' must either be a folder to be zipped, or an existing zip.")

                let slotName = slot.ToOption
                printfn "Running ZIP deploy to %s for %s" (slotName |> Option.defaultValue "WebApp") path.Value

                Some(
                    match target with
                    | ZipDeploy.WebApp ->
                        Deploy.Az.zipDeployWebApp siteType.ResourceName.Value path.GetZipPath resourceGroupName slotName
                    | ZipDeploy.FunctionApp ->
                        Deploy.Az.zipDeployFunctionApp
                            siteType.ResourceName.Value
                            path.GetZipPath
                            resourceGroupName
                            slotName
                )
            | _ -> None

    interface IArmResource with
        member this.ResourceId = sites.resourceId this.Name

        member this.JsonModel =
            let dependencies =
                this.Dependencies
                + (Set this.Identity.Dependencies)
                + (this.LinkToSubnet
                   |> Option.bind (fun x -> x.Dependency)
                   |> Option.toList
                   |> Set.ofList)

            let keyvaultId =
                match (this.KeyVaultReferenceIdentity, this.Identity) with
                | Some x, _
                // If there is no managed identity and only one user-assigned identity, we should use that be default
                | None,
                  {
                      SystemAssigned = Disabled
                      UserAssigned = [ x ]
                  } -> x.ResourceId.Eval()
                | _ -> null

            {|
                this.ResourceType.Create(this.Name, this.Location, dependencies, this.Tags) with
                    kind = this.Kind
                    identity =
                        if this.Identity = ManagedIdentity.Empty then
                            Unchecked.defaultof<_>
                        else
                            this.Identity.ToArmJson
                    properties = {|
                        serverFarmId = this.ServicePlan.Eval()
                        httpsOnly = this.HTTPSOnly
                        clientAffinityEnabled =
                            match this.ClientAffinityEnabled with
                            | Some v -> box v
                            | None -> null
                        keyVaultReferenceIdentity = keyvaultId
                        virtualNetworkSubnetId =
                            match this.LinkToSubnet with
                            | None -> null
                            | Some id -> id.ResourceId.ArmExpression.Eval()
                        siteConfig = {|
                            alwaysOn = this.AlwaysOn
                            appSettings = this.AppSettings |> mapOrNull (fun (k, v) -> {| name = k; value = v.Value |})
                            connectionStrings =
                                this.ConnectionStrings
                                |> mapOrNull (fun (k, (v, t)) -> {|
                                    name = k
                                    connectionString = v.Value
                                    ``type`` = t.ToString()
                                |})
                            ftpsState =
                                match this.FTPState with
                                | Some FTPState.AllAllowed -> "AllAllowed"
                                | Some FTPState.FtpsOnly -> "FtpsOnly"
                                | Some FTPState.Disabled -> "Disabled"
                                | None -> null
                            linuxFxVersion = this.LinuxFxVersion |> Option.toObj
                            appCommandLine = this.AppCommandLine |> Option.toObj
                            netFrameworkVersion = this.NetFrameworkVersion |> Option.toObj
                            use32BitWorkerProcess =
                                this.WorkerProcess
                                |> Option.map (function
                                    | Bits32 -> true
                                    | Bits64 -> false)
                                |> Option.toNullable
                            javaVersion = this.JavaVersion |> Option.toObj
                            javaContainer = this.JavaContainer |> Option.toObj
                            javaContainerVersion = this.JavaContainerVersion |> Option.toObj
                            phpVersion = this.PhpVersion |> Option.toObj
                            ipSecurityRestrictions =
                                match this.IpSecurityRestrictions with
                                | [] -> null
                                | restrictions ->
                                    restrictions
                                    |> List.mapi (fun index restriction -> {|
                                        ipAddress = IPAddressCidr.format restriction.IpAddressCidr
                                        name = restriction.Name
                                        action = restriction.Action.ToString()
                                        priority = index + 1
                                    |})
                                    |> box
                            pythonVersion = this.PythonVersion |> Option.toObj
                            http20Enabled = this.HTTP20Enabled |> Option.toNullable
                            webSocketsEnabled = this.WebSocketsEnabled |> Option.toNullable
                            metadata = [
                                for key, value in this.Metadata do
                                    {| name = key; value = value |}
                            ]
                            cors =
                                this.Cors
                                |> Option.map (function
                                    | AllOrigins -> box {| allowedOrigins = [ "*" ] |}
                                    | SpecificOrigins(origins, credentials) ->
                                        box {|
                                            allowedOrigins = origins
                                            supportCredentials = credentials |> Option.toNullable
                                        |})
                                |> Option.toObj
                            healthCheckPath = this.HealthCheckPath |> Option.toObj
                            autoSwapSlotName = this.AutoSwapSlotName |> Option.toObj
                            vnetName =
                                this.LinkToSubnet
                                |> Option.map (fun x -> x.ResourceId.Segments[0].Value)
                                |> Option.toObj
                            vnetRouteAllEnabled =
                                this.LinkToSubnet
                                |> function
                                    | Some _ -> Nullable true
                                    | None -> Nullable()
                            virtualApplications =
                                if this.VirtualApplications.IsEmpty then
                                    null
                                else
                                    this.VirtualApplications
                                    |> Seq.map (fun virtualAppKvp -> {|
                                        virtualPath = virtualAppKvp.Key
                                        physicalPath = virtualAppKvp.Value.PhysicalPath
                                        preloadEnabled = virtualAppKvp.Value.PreloadEnabled |> Option.toNullable
                                    |})
                                    |> box
                            functionAppScaleLimit =
                                match this.FunctionAppScaleLimit with
                                | Some limit -> box limit
                                | None -> null
                        |}
                    |}
            |}

module Sites =
    type SourceControl = {
        Website: ResourceName
        Location: Location
        Repository: Uri
        Branch: string
        ContinuousIntegration: FeatureFlag
    } with

        member this.Name = this.Website.Map(sprintf "%s/web")

        interface IArmResource with
            member this.ResourceId = sourceControls.resourceId this.Name

            member this.JsonModel = {|
                sourceControls.Create(this.Name, this.Location, [ sites.resourceId this.Website ]) with
                    properties = {|
                        repoUrl = this.Repository.ToString()
                        branch = this.Branch
                        isManualIntegration = this.ContinuousIntegration.AsBoolean |> not
                    |}
            |}

type VirtualNetworkConnection = {
    Site: Site
    Subnet: ResourceId
    Dependencies: ResourceId list
} with

    member this.Name = this.Site.Name / this.Subnet.Name
    member this.SiteId = this.Site.ResourceType.resourceId this.Site.Name

    interface IArmResource with
        member this.ResourceId = virtualNetworkConnections.resourceId this.Name

        member this.JsonModel =
            let resourceType =
                match this.Site.SiteType with
                | Site _ -> virtualNetworkConnections
                | Slot _ -> slotsVirtualNetworkConnections

            {|
                resourceType.Create(this.Name, dependsOn = [ this.SiteId; yield! this.Dependencies ]) with
                    properties = {|
                        vnetResourceId = this.Subnet.ArmExpression.Eval()
                        isSwift = true
                    |}
            |}
            :> _

type StaticSite = {
    Name: ResourceName
    Location: Location
    Repository: Uri
    Branch: string
    RepositoryToken: SecureParameter
    AppLocation: string
    ApiLocation: string option
    AppArtifactLocation: string option
} with

    interface IArmResource with
        member this.ResourceId = staticSites.resourceId this.Name

        member this.JsonModel = {|
            staticSites.Create(this.Name, this.Location) with
                properties = {|
                    repositoryUrl = this.Repository.ToString()
                    branch = this.Branch
                    repositoryToken = this.RepositoryToken.ArmExpression.Eval()
                    buildProperties = {|
                        appLocation = this.AppLocation
                        apiLocation = this.ApiLocation |> Option.toObj
                        appArtifactLocation = this.AppArtifactLocation |> Option.toObj
                    |}
                |}
                sku = {| Tier = "Free"; Name = "Free" |}
        |}

    interface IParameters with
        member this.SecureParameters = [ this.RepositoryToken ]

module StaticSites =
    type Config = {
        StaticSite: ResourceName
        Properties: Map<string, string>
    } with

        member this.ResourceName = this.StaticSite / "appsettings"

        interface IArmResource with
            member this.ResourceId = staticSitesConfig.resourceId this.ResourceName

            member this.JsonModel: obj = {|
                staticSitesConfig.Create(
                    this.ResourceName,
                    dependsOn = [ ResourceId.create (staticSites, this.StaticSite) ]
                ) with
                    properties = this.Properties
            |}

type SslState =
    | SslDisabled
    | SniBased of thumbprint: ArmExpression

type HostNameBinding = {
    Location: Location
    SiteId: LinkedResource
    DomainName: string
    SslState: SslState
} with

    member this.SiteResourceId = this.SiteId.Name
    member this.ResourceName = this.SiteResourceId / this.DomainName

    member this.ResourceId =
        hostNameBindings.resourceId (this.SiteResourceId, ResourceName this.DomainName)

    interface IArmResource with
        member this.ResourceId = hostNameBindings.resourceId this.ResourceName

        member this.JsonModel = {|
            hostNameBindings.Create(this.ResourceName, this.Location) with
                properties =
                    match this.SslState with
                    | SniBased thumbprint ->
                        {|
                            sslState = "SniEnabled"
                            thumbprint = thumbprint.Eval()
                        |}
                        :> obj
                    | SslDisabled -> {| |} :> obj
        |}

type Certificate = {
    Location: Location
    SiteId: LinkedResource
    ServicePlanId: LinkedResource
    DomainName: string
} with

    member this.ResourceName = ResourceName this.DomainName
    member this.Thumbprint = this.GetThumbprintReference None

    member this.GetThumbprintReference certificateResourceGroup =
        ArmExpression
            .reference(
                {
                    certificates.resourceId this.ResourceName with
                        ResourceGroup = certificateResourceGroup
                }
            )
            .Map(sprintf "%s.Thumbprint")

    interface IArmResource with
        member this.ResourceId = certificates.resourceId this.ResourceName

        member this.JsonModel =
            let dependencies =
                match this.SiteId with
                | Managed r -> [
                    r
                    {
                        hostNameBindings.resourceId (r.Name, ResourceName this.DomainName) with
                            ResourceGroup = r.ResourceGroup
                    }
                  ]
                | _ -> []

            {|
                certificates.Create(this.ResourceName, this.Location, dependencies) with
                    properties = {|
                        serverFarmId = this.ServicePlanId.ResourceId.Eval()
                        canonicalName = this.DomainName
                    |}
            |}

[<AutoOpen>]
module SiteExtensions =
    type SiteExtension = {
        Name: ResourceName
        SiteName: ResourceName
        Location: Location
    } with

        interface IArmResource with
            member this.ResourceId = siteExtensions.resourceId (this.SiteName / this.Name)

            member this.JsonModel =
                siteExtensions.Create(this.SiteName / this.Name, this.Location, [ sites.resourceId this.SiteName ])