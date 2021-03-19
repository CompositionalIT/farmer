[<AutoOpen>]
module Farmer.Arm.Web

open Farmer
open Farmer.WebApp
open System

let serverFarms = ResourceType ("Microsoft.Web/serverfarms", "2018-02-01")
let sites = ResourceType ("Microsoft.Web/sites", "2020-06-01")
let config = ResourceType ("Microsoft.Web/sites/config", "2016-08-01")
let sourceControls = ResourceType ("Microsoft.Web/sites/sourcecontrols", "2019-08-01")
let staticSites = ResourceType("Microsoft.Web/staticSites", "2019-12-01-preview")
let siteExtensions = ResourceType("Microsoft.Web/sites/siteextensions", "2020-06-01")

type ServerFarm =
    { Name : ResourceName
      Location : Location
      Sku: Sku
      WorkerSize : WorkerSize
      WorkerCount : int
      OperatingSystem : OS
      Tags: Map<string,string> }
    member this.IsDynamic =
        match this.Sku, this.WorkerSize with
        | Isolated "Y1", Serverless -> true
        | _ -> false
    member this.Reserved =
        match this.OperatingSystem with
        | Linux -> true
        | Windows -> false
    member this.Kind =
        match this.OperatingSystem with
        | Linux -> Some "linux"
        | Windows -> None
    member this.Tier =
        match this.Sku with
        | Free -> "Free"
        | Shared -> "Shared"
        | Basic _ -> "Basic"
        | Standard _ -> "Standard"
        | Premium _ -> "Premium"
        | PremiumV2 _ -> "PremiumV2"
        | PremiumV3 _ -> "PremiumV3"
        | Dynamic -> "Dynamic"
        | Isolated _ -> "Isolated"
    interface IArmResource with
        member this.ResourceId = serverFarms.resourceId this.Name
        member this.JsonModel =
            {| serverFarms.Create(this.Name, this.Location, tags = this.Tags) with
                 sku =
                   {| name =
                        match this.Sku with
                        | Free ->
                            "F1"
                        | Shared ->
                            "D1"
                        | Basic sku
                        | Standard sku
                        | Premium sku
                        | PremiumV2 sku
                        | PremiumV3 sku
                        | Isolated sku ->
                            sku
                        | Dynamic ->
                            "Y1"
                      tier = this.Tier
                      size =
                        match this.WorkerSize with
                        | Small -> "0"
                        | Medium -> "1"
                        | Large -> "2"
                        | Serverless -> "Y1"
                      family = if this.IsDynamic then "Y" else null
                      capacity = if this.IsDynamic then 0 else this.WorkerCount |}
                 properties =
                      {| name = this.Name.Value
                         computeMode = if this.IsDynamic then "Dynamic" else null
                         perSiteScaling = if this.IsDynamic then Nullable() else Nullable false
                         reserved = this.Reserved |}
                 kind = this.Kind |> Option.toObj
            |} :> _

module ZipDeploy =
    open System.IO
    open System.IO.Compression

    type ZipDeployTarget =
        | WebApp
        | FunctionApp

    type ZipDeployKind =
        | DeployFolder of string
        | DeployZip of string
        member this.Value = match this with DeployFolder s | DeployZip s -> s
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
                let packageFilename = Path.Combine(targetFolder, (Path.GetFileName appFolder) + ".zip")
                File.Delete packageFilename
                ZipFile.CreateFromDirectory(appFolder, packageFilename)
                packageFilename
            | DeployZip zipFilePath ->
                zipFilePath
type Site =
    { Name : ResourceName
      Location : Location
      ServicePlan : ResourceId
      AppSettings : Map<string, Setting>
      ConnectionStrings : Map<string, (Setting * ConnectionStringKind)>
      AlwaysOn : bool
      WorkerProcess : Bitness option
      HTTPSOnly : bool
      HTTP20Enabled : bool option
      ClientAffinityEnabled : bool option
      WebSocketsEnabled : bool option
      Cors : Cors option
      Dependencies : ResourceId Set
      Kind : string
      Identity : Identity.ManagedIdentity
      LinuxFxVersion : string option
      AppCommandLine : string option
      NetFrameworkVersion : string option
      JavaVersion : string option
      JavaContainer : string option
      JavaContainerVersion : string option
      PhpVersion : string option
      PythonVersion : string option
      Tags : Map<string, string>
      Metadata : List<string * string>
      ZipDeployPath : (string * ZipDeploy.ZipDeployTarget) option }
    interface IParameters with
        member this.SecureParameters =
            Map.toList this.AppSettings
            @ (Map.toList this.ConnectionStrings |> List.map(fun (k, (v,_)) -> k, v))
            |> List.choose(snd >> function
                | ParameterSetting s -> Some s
                | ExpressionSetting _ | LiteralSetting _ -> None)

    interface IPostDeploy with
        member this.Run resourceGroupName =
            match this with
            | { ZipDeployPath = Some (path, target); Name = name } ->
                let path =
                    ZipDeploy.ZipDeployKind.TryParse path
                    |> Option.defaultWith (fun () ->
                        failwith $"Path '{path}' must either be a folder to be zipped, or an existing zip.")
                printfn "Running ZIP deploy for %s" path.Value
                Some (match target with
                      | ZipDeploy.WebApp -> Deploy.Az.zipDeployWebApp name.Value path.GetZipPath resourceGroupName
                      | ZipDeploy.FunctionApp -> Deploy.Az.zipDeployFunctionApp name.Value path.GetZipPath resourceGroupName)
            | _ ->
                None
    interface IArmResource with
        member this.ResourceId = sites.resourceId this.Name
        member this.JsonModel =
            let dependencies = this.Dependencies + (Set this.Identity.Dependencies)
            {| sites.Create(this.Name, this.Location, dependencies, this.Tags) with
                 kind = this.Kind
                 identity = this.Identity |> ManagedIdentity.toArmJson
                 properties =
                    {| serverFarmId = this.ServicePlan.Eval()
                       httpsOnly = this.HTTPSOnly
                       clientAffinityEnabled = match this.ClientAffinityEnabled with Some v -> box v | None -> null
                       siteConfig =
                        {| alwaysOn = this.AlwaysOn
                           appSettings = this.AppSettings |> Map.toList |> List.map(fun (k,v) -> {| name = k; value = v.Value |})
                           connectionStrings = this.ConnectionStrings |> Map.toList |> List.map(fun (k,(v, t)) -> {| name = k; connectionString = v.Value; ``type`` = t.ToString() |})
                           linuxFxVersion = this.LinuxFxVersion |> Option.toObj
                           appCommandLine = this.AppCommandLine |> Option.toObj
                           netFrameworkVersion = this.NetFrameworkVersion |> Option.toObj
                           use32BitWorkerProcess = this.WorkerProcess |> Option.map (function Bits32 -> true | Bits64 -> false) |> Option.toNullable
                           javaVersion = this.JavaVersion |> Option.toObj
                           javaContainer = this.JavaContainer |> Option.toObj
                           javaContainerVersion = this.JavaContainerVersion |> Option.toObj
                           phpVersion = this.PhpVersion |> Option.toObj
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
                                | AllOrigins ->
                                    box {| allowedOrigins = [ "*" ] |}
                                | SpecificOrigins (origins, credentials) ->
                                    box {| allowedOrigins = origins
                                           supportCredentials = credentials |> Option.toNullable |})
                            |> Option.toObj
                        |}
                    |}
            |} :> _

module Sites =
    type SourceControl =
        { Website : ResourceName
          Location : Location
          Repository : Uri
          Branch : string
          ContinuousIntegration : FeatureFlag }
        member this.Name = this.Website.Map(sprintf "%s/web")
        interface IArmResource with
            member this.ResourceId = sourceControls.resourceId this.Name
            member this.JsonModel =
                {| sourceControls.Create(this.Name, this.Location, [ sites.resourceId this.Website ]) with
                    properties =
                        {| repoUrl = this.Repository.ToString()
                           branch = this.Branch
                           isManualIntegration = this.ContinuousIntegration.AsBoolean |> not |}
                |} :> _

type StaticSite =
    { Name : ResourceName
      Location : Location
      Repository : Uri
      Branch : string
      RepositoryToken : SecureParameter
      AppLocation : string
      ApiLocation : string option
      AppArtifactLocation : string option }
    interface IArmResource with
        member this.ResourceId = staticSites.resourceId this.Name
        member this.JsonModel =
            {| staticSites.Create(this.Name, this.Location) with
                properties =
                 {| repositoryUrl = this.Repository.ToString()
                    branch = this.Branch
                    repositoryToken = this.RepositoryToken.ArmExpression.Eval()
                    buildProperties =
                     {| appLocation = this.AppLocation
                        apiLocation = this.ApiLocation |> Option.toObj
                        appArtifactLocation = this.AppArtifactLocation |> Option.toObj |}
                 |}
                sku =
                 {| Tier = "Free"
                    Name = "Free" |}
            |} :> _
    interface IParameters with
        member this.SecureParameters = [
            this.RepositoryToken
        ]

[<AutoOpen>]
module SiteExtensions =
    type SiteExtension =
        { Name : ResourceName
          SiteName : ResourceName
          Location : Location }
        interface IArmResource with
            member this.ResourceId = siteExtensions.resourceId(this.SiteName/this.Name)
            member this.JsonModel =
                siteExtensions.Create(this.SiteName/this.Name, this.Location, [ sites.resourceId this.SiteName ]) :> _