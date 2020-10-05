[<AutoOpen>]
module Farmer.Arm.Web

open Farmer
open Farmer.CoreTypes
open Farmer.WebApp
open System

let serverFarms = ResourceType ("Microsoft.Web/serverfarms", "2018-02-01")
let sites = ResourceType ("Microsoft.Web/sites", "2016-08-01")
let config = ResourceType ("Microsoft.Web/sites/config", "2016-08-01")
let sourceControls = ResourceType ("Microsoft.Web/sites/sourcecontrols", "2019-08-01")

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
        | _ -> None
    member this.Tier =
        match this.Sku with
        | Free -> "Free"
        | Shared -> "Shared"
        | Basic _ -> "Basic"
        | Standard _ -> "Standard"
        | Premium _ -> "Premium"
        | PremiumV2 _ -> "PremiumV2"
        | Dynamic -> "Dynamic"
        | Isolated _ -> "Isolated"
    interface IArmResource with
        member this.ResourceName = this.Name
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
      ServicePlan : ResourceName
      AppSettings : Map<string, Setting>
      ConnectionStrings : Map<string, (Setting * ConnectionStringKind)>
      AlwaysOn : bool
      HTTPSOnly : bool
      HTTP20Enabled : bool option
      ClientAffinityEnabled : bool option
      WebSocketsEnabled : bool option
      Cors : Cors option
      Dependencies : ResourceId list
      Kind : string
      Identity : FeatureFlag option
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
                        failwithf "Path '%s' must either be a folder to be zipped, or an existing zip." path)
                printfn "Running ZIP deploy for %s" path.Value
                Some (match target with
                        | ZipDeploy.ZipDeployTarget.WebApp -> Deploy.Az.zipDeployWebApp name.Value path.GetZipPath resourceGroupName
                        | ZipDeploy.ZipDeployTarget.FunctionApp -> Deploy.Az.zipDeployFunctionApp name.Value path.GetZipPath resourceGroupName)
            | _ ->
                None
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| sites.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                 kind = this.Kind
                 identity =
                   match this.Identity with
                   | Some Enabled -> box {| ``type`` = "SystemAssigned" |}
                   | Some Disabled -> box {| ``type`` = "None" |}
                   | None -> null
                 properties =
                      {| serverFarmId = this.ServicePlan.Value
                         httpsOnly = this.HTTPSOnly
                         clientAffinityEnabled = match this.ClientAffinityEnabled with Some v -> box v | None -> null
                         siteConfig =
                          {| alwaysOn = this.AlwaysOn
                             appSettings = this.AppSettings |> Map.toList |> List.map(fun (k,v) -> {| name = k; value = v.Value |})
                             connectionStrings = this.ConnectionStrings |> Map.toList |> List.map(fun (k,(v, t)) -> {| name = k; connectionString = v.Value; ``type`` = t.ToString() |})
                             linuxFxVersion = this.LinuxFxVersion |> Option.toObj
                             appCommandLine = this.AppCommandLine |> Option.toObj
                             netFrameworkVersion = this.NetFrameworkVersion |> Option.toObj
                             javaVersion = this.JavaVersion |> Option.toObj
                             javaContainer = this.JavaContainer |> Option.toObj
                             javaContainerVersion = this.JavaContainerVersion |> Option.toObj
                             phpVersion = this.PhpVersion |> Option.toObj
                             pythonVersion = this.PythonVersion |> Option.toObj
                             http20Enabled = this.HTTP20Enabled |> Option.toNullable
                             webSocketsEnabled = this.WebSocketsEnabled |> Option.toNullable
                             metadata = this.Metadata |> List.map(fun (k,v) -> {| name = k; value = v |})
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
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| sourceControls.Create(this.Name, this.Location, [ ResourceId.create this.Website ]) with
                    properties =
                        {| repoUrl = this.Repository.ToString()
                           branch = this.Branch
                           isManualIntegration = this.ContinuousIntegration.AsBoolean |> not |}
                |} :> _