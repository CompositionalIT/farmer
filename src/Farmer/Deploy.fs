﻿module Farmer.Deploy

open Farmer.ArmBuilder
open Newtonsoft.Json
open System
open System.IO

let deployFolder = ".farmer"
let prepareDeploymentFolder() =
    if Directory.Exists deployFolder then Directory.Delete(deployFolder, true)
    Directory.CreateDirectory deployFolder |> ignore
let generateDeployNumber =
    let r = Random()
    fun () -> r.Next 10000

/// Provides strongly-typed access to the Azure CLI
module Az =
    open System.Runtime.InteropServices
    open CliWrap
    open CliWrap.Buffered

    [<AutoOpen>]
    module AzHelpers =
        let (|OperatingSystem|_|) platform () =
            if RuntimeInformation.IsOSPlatform platform then Some() else None

        let findAz =
            match () with
            | OperatingSystem OSPlatform.Windows ->
                Environment.GetEnvironmentVariable "PATH"
                |> fun s -> s.Split Path.PathSeparator
                |> Seq.map (fun s -> Path.Combine(s, "az.cmd"))
                |> Seq.tryFind File.Exists
                |> function Some s -> s | None -> invalidOp "Can't find Azure CLI"
            | OperatingSystem OSPlatform.Linux
            | OperatingSystem OSPlatform.OSX ->
                "az"
            | _ ->
                failwithf "OSPlatform: %s not supported" RuntimeInformation.OSDescription

    /// Executes a generic AZ CLI command.
    let executeAz (arguments : string) =
        let cmd = Cli.Wrap(findAz)
                     .WithArguments(arguments)
                     .WithValidation(CommandResultValidation.None)

        let result = cmd.ExecuteBufferedAsync().Task |> Async.AwaitTask |> Async.RunSynchronously

        match result.ExitCode with
        | 0 -> Ok result.StandardOutput
        | _ -> Error result.StandardError

    /// Tests if the Az CLI has logged in credentials.
    let isLoggedIn() = executeAz "account show" |> function Ok _ -> true | Error _ -> false
    /// Logs you into Az CLI interactively.
    let login() = executeAz "login" |> Result.ignore
    /// Logs you into the Az CLI using the supplied service principal credentials.
    let loginWithCredentials appId secret tenantId = executeAz (sprintf "login --service-principal --username %s --password %s --tenant %s" appId secret tenantId)
    /// Creates a resource group.
    let createResourceGroup location resourceGroup =
        executeAz (sprintf "group create -l %s -n %s" location resourceGroup) |> Result.ignore
    /// Deploys an ARM template to an existing resource group.
    let deploy resourceGroup deploymentName templateFilename parameters =
        let parametersArgument =
            match parameters with
            | [] -> ""
            | parameters -> sprintf "--parameters %s" (parameters |> List.map(fun (a,b) -> sprintf "%s=%s" a b) |> String.concat " ")
        executeAz (sprintf "group deployment create -g %s -n %s --template-file %s %s" resourceGroup deploymentName templateFilename parametersArgument)
    /// Deploys a zip file to a web app using the Zip Deploy mechanism.
    let zipDeploy webAppName (zipDeployKind:ZipDeployKind) resourceGroup =
        let packageFilename = zipDeployKind.GetZipPath deployFolder
        executeAz (sprintf """webapp deployment source config-zip --resource-group "%s" --name "%s" --src %s""" resourceGroup webAppName packageFilename)

type OutputKey = string
type OutputValue = string
type OutputMap = Map<OutputKey, OutputValue>
type Subscription = { ID : Guid; Name : string; IsDefault : bool }

/// Authenticates the Az CLI using the supplied ApplicationId, Client Secret and Tenant Id.
/// Returns the list of subscriptions, including which one the default is.
let authenticate appId secret tenantId =
    Az.loginWithCredentials appId secret tenantId
    |> Result.map (JsonConvert.DeserializeObject<Subscription []>)

let validateParameters suppliedParameters deployment =
    let expected = deployment.Template.Parameters |> List.map(fun (SecureParameter p) -> p) |> Set
    match (expected - (suppliedParameters |> List.map fst |> Set)) |> Seq.toList with
    | [] -> Ok ()
    | missingParameters -> Error (sprintf "The following parameters are missing: %s." (missingParameters |> String.concat ", "))

/// Executes the supplied Deployment against a resource group using the Azure CLI.
/// If successful, returns a Map of the output keys and values.
let execute resourceGroupName parameters deployment : Result<OutputMap, _> = result {
    prepareDeploymentFolder()
    do! deployment |> validateParameters parameters
    do!
        printf "Checking Azure CLI logged in status... "
        if Az.isLoggedIn() then printfn "you are already logged in, nothing to do."; Ok()
        else printfn "logging you in."; Az.login()

    printfn "Creating resource group %s..." resourceGroupName
    do! Az.createResourceGroup deployment.Location.Value resourceGroupName

    printfn "Deploying ARM template (please be patient, this can take a while)..."
    let! response =
        let deploymentName = sprintf "farmer-deploy-%d" (generateDeployNumber())
        let templateFilename = deployment.Template |> Writer.toJson |> Writer.toFile deployFolder "farmer-deploy"
        //let parameters = parameters |> List.map(fun (key:string, value:string) -> key, {| value = value |}) |> Map.ofList |> JsonConvert.SerializeObject
        Az.deploy resourceGroupName deploymentName templateFilename parameters

    do!
        [ for (RunFromZip wd) in deployment.PostDeployTasks do
            printfn "Running ZIP deploy for %s" wd.Path.Value
            Az.zipDeploy wd.WebApp.Value wd.Path resourceGroupName ]
        |> Result.sequence
        |> Result.ignore

    printfn "All done, now parsing ARM response to get any outputs..."
    let response = response |> JsonConvert.DeserializeObject<{| properties : {| outputs : Map<string, {| value : string |}> |} |}>
    return response.properties.outputs |> Map.map (fun _ value -> value.value)
}