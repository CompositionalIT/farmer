module Farmer.Deploy

open Farmer.ArmBuilder
open Newtonsoft.Json
open System

/// Represents an Azure service principal which has permissions to
/// deploy ARM templates on the supplied Subscription ID.
type AzureCredentials =
    { ClientId : Guid
      ClientSecret : Guid
      TenantId : Guid }

type Outputs = Map<string, string>
type DeploymentRejectionError =
    | CantObtainBearerToken of {| Error : string; Error_Description : string |}
    | CantCreateResourceGroup of string
    | InvalidTemplateRejection of string
type ErrorDetails =
    { Code : string
      Message : string
      Details : {| Code : string
                   Message : string |} array }
type DeploymentFailureError =
    | CantGetStatus of string
    | ProvisioningFailure of ErrorDetails
type DeploymentError =
    | DeploymentRejected of DeploymentRejectionError
    | DeploymentFailed of DeploymentFailureError
type DeploymentResult =
    { DeploymentName : string
      Result : Result<Outputs, DeploymentError> }

type DeploymentStatus =
    | Provisioning of string
    | Provisioned of Outputs

module AzureRest =
    open FsHttp.DslCE
    let toResult (response:FsHttp.Domain.Response) =
        match int response.statusCode with
        | code when code >= 200 && code < 300 -> Ok response
        | _ -> Error response
    let getContent<'T> (response:FsHttp.Domain.Response) =
        response.content.ReadAsStringAsync().Result
        |> JsonConvert.DeserializeObject<'T>
    let getBearerToken tenantId clientId clientSecret =
        http {
            POST (sprintf "https://login.microsoftonline.com/%s/oauth2/token" tenantId)
            body
            formUrlEncoded
                [ "grant_type", "client_credentials"
                  "client_id", clientId
                  "client_secret", clientSecret
                  "resource", "https://management.azure.com" ]
        }
        |> toResult
        |> Result.map getContent<{| access_token:string |}>
        |> Result.mapError getContent<{| Error:string; Error_Description:string |}>
    let createResourceGroup accessToken subscriptionId resourceGroup location =
        http {
            PUT (sprintf "https://management.azure.com/subscriptions/%s/resourcegroups/%s?api-version=2019-05-01" subscriptionId resourceGroup)
            BearerAuth accessToken
            body
            json (sprintf """{ "location": "%s", "tags": { "Deployed with Farmer": "" }}""" location)
        } |> toResult
    let deployTemplate accessToken subscriptionId resourceGroup deployment templateJson =
        http {
            PUT (sprintf "https://management.azure.com/subscriptions/%s/resourcegroups/%s/providers/Microsoft.Resources/deployments/%s?api-version=2019-05-01" subscriptionId resourceGroup deployment)
            BearerAuth accessToken
            body
            json (sprintf """{ "properties": { "mode": "Incremental", "template": %s } }""" templateJson)
        } |> toResult

    open Result

    let getDeploymentStatus accessToken subscriptionId resourceGroup deployment = result {
        let! deploymentDetails =
            http {
                GET (sprintf "https://management.azure.com/subscriptions/%s/resourcegroups/%s/providers/Microsoft.Resources/deployments/%s?api-version=2018-05-01" subscriptionId resourceGroup deployment)
                BearerAuth accessToken
            }
            |> toResult
            |> Result.mapError(fun x -> CantGetStatus (x.content.ReadAsStringAsync().Result))

        let content =
            deploymentDetails
            |> getContent<
                {| Properties :
                    {| ProvisioningState : string
                       Outputs : Map<string, {| value : string |}>
                       Error : obj |}
                |}>

        return!
            match content.Properties.Error, content.Properties.ProvisioningState with
            | null, ("Accepted" | "Running") ->
                content.Properties.ProvisioningState
                |> Provisioning
                |> Ok
            | null, _ ->
                content.Properties.Outputs
                |> Map.map(fun _ v -> v.value)
                |> Provisioned
                |> Ok
            | error, _ ->
                error
                |> string
                |> JsonConvert.DeserializeObject<ErrorDetails>
                |> ProvisioningFailure
                |> Error
    }

module RestDeployment =
    let getDeployNumber =
        let r = Random()
        fun () -> r.Next 10000
    type ProgressResult = Result<DeploymentStatus, DeploymentFailureError>
    /// Represents the "raw" result of a deployment, which is result of result. The "top" level result
    /// is the initial stage of deployment. If this succeeds, a sequence of results are provided back
    /// representing the ongoing polling of the deployment.
    type RawDeploymentResult =
        {| DeploymentName : string
           Result : Result<ProgressResult seq, DeploymentRejectionError> |}

    /// Deploys a template using the Rest API.
    open Result

    let deployTemplate (credentials:AzureCredentials) subscriptionId (armTemplateJson:string, location:string, resourceGroup:string) : RawDeploymentResult =
        let deploymentName = sprintf "FarmerDeploy%d" (getDeployNumber())
        let deploymentResult = result {
            let! bearerToken =
                AzureRest.getBearerToken (string credentials.TenantId) (string credentials.ClientId) (string credentials.ClientSecret)
                |> Result.mapError CantObtainBearerToken
                |> Result.map(fun response -> response.access_token)

            do!
                AzureRest.createResourceGroup bearerToken subscriptionId resourceGroup location
                |> Result.mapError(fun response -> CantCreateResourceGroup (response.content.ReadAsStringAsync().Result))
                |> Result.ignore

            do!
                armTemplateJson
                |> AzureRest.deployTemplate bearerToken subscriptionId resourceGroup deploymentName
                |> Result.mapError(fun e -> InvalidTemplateRejection (e.content.ReadAsStringAsync().Result))
                |> Result.ignore

            return
                Seq.initInfinite (fun _ ->
                    Async.Sleep 5000 |> Async.RunSynchronously
                    AzureRest.getDeploymentStatus bearerToken subscriptionId resourceGroup deploymentName)
                |> Seq.distinct
        }

        {| DeploymentName = deploymentName
           Result = deploymentResult |}

    /// Gets the final deployment result once a deployment has started.
    let getDeploymentResult statuses =
        statuses
        |> Seq.choose(function
            | Ok (Provisioning _) -> None
            | Ok (Provisioned outputs) -> Some (Ok outputs)
            | Error error -> Some (Error(DeploymentFailed error)))
        |> Seq.tryHead
        |> Option.defaultValue (Error (DeploymentFailed (CantGetStatus "Could not get any deployment status.")))

    /// Monitors an ARM template with optional progress reports.
    let reportDeploymentProgress onStatus (deployment: RawDeploymentResult) : DeploymentResult =
        let output =
            deployment.Result
            |> Result.mapError DeploymentRejected
            |> Result.bind(fun statuses ->
                statuses
                |> Seq.map (fun status ->
                    match status with
                    | Ok (Provisioning s) -> onStatus s
                    | _ -> ()
                    status)
                |> getDeploymentResult)

        { DeploymentName = deployment.DeploymentName
          Result = output }

/// Executes the supplied Deployment against a resource group using the Azure REST API.
/// It requires a service principle containing a client id, secret and tenant ID. Use this API for unattended installs e.g. continuous deployment etc. 
let fullDeploy credentials (subscriptionId:Guid) resourceGroupName deployment =
    let armTemplateJson = deployment.Template |> Writer.toJson

    (armTemplateJson, deployment.Location.Value, resourceGroupName)
    |> RestDeployment.deployTemplate credentials (string subscriptionId)
    |> RestDeployment.reportDeploymentProgress (printfn "%s")

module ParameterFile =
    module Passwords =
        open System
        let lowerCaseLetters = String [|'a'..'z'|]
        let upperCaseLetters = String [|'A'..'Z'|]
        let digits = String [|'0' .. '9'|]
        let special = "!�$%^&*()_-+="
        let allCharacters = lowerCaseLetters + upperCaseLetters + digits + special

        let isValid (s:string) =
            let isInString (src:string) = s |> Seq.exists (string >> src.Contains)
            isInString lowerCaseLetters && isInString upperCaseLetters && isInString digits && isInString special
            
        let generatePassword randomNumber length =
            Seq.init length (fun _ -> allCharacters.[randomNumber allCharacters.Length])
            |> Seq.toArray
            |> String

        /// Creates a password that is known to conform to lower, upper and numeric constraints.
        let generateConformingPassword length template =
            let rnd = Random (template.GetHashCode())

            Seq.initInfinite (fun _ -> generatePassword rnd.Next length)
            |> Seq.take 100
            |> Seq.filter isValid
            |> Seq.tryHead
            |> function
            | None -> failwith "Unable to generate a valid password that meet the requested requirements!"
            | Some password -> password

    let toParameters parameters =
        {| ``$schema`` = "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#"
           contentVersion = "1.0.0.0"
           parameters =
                parameters
                |> List.map(fun (name, value) -> name, {| value = value |})
                |> Map.ofList
        |}     
       
    let generateParametersFile (armTemplate:ArmTemplate) =
        armTemplate.Parameters
        |> List.map(fun (SecureParameter p) -> p, Passwords.generateConformingPassword 24 armTemplate)
        |> toParameters
        |> Writer.TemplateGeneration.serialize
        |> Writer.toFile "farmer-deploy-parameters"

module AzureCli =
    open System.IO
    open System.Runtime.InteropServices

    let setLinuxExecutePermissions filename =
        let command = sprintf "chmod +x %s" filename
        let startInfo = 
            System.Diagnostics.ProcessStartInfo( 
                FileName = "/bin/bash",
                Arguments = "-c \""+ command + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true )

        use proc = new System.Diagnostics.Process(StartInfo = startInfo)
        proc.Start() |> ignore
        proc.WaitForExit() |> ignore
        filename

    let toAzureCliCmd resourceGroupName (Location location) templateFilename parametersFilename =
        sprintf """az login && az group create -l %s -n %s && az group deployment create -g %s --template-file %s --parameters @%s"""
            location
            resourceGroupName
            resourceGroupName
            templateFilename
            parametersFilename

    let (|OperatingSystem|_|) platform () =
        if RuntimeInformation.IsOSPlatform platform then Some() else None

    let toScriptFile armTemplateName azureCliCmd =
        match () with
        | OperatingSystem OSPlatform.Windows ->
            let scriptFilename = sprintf "%s.bat" armTemplateName
            File.WriteAllText(scriptFilename, azureCliCmd)
            scriptFilename
        | OperatingSystem OSPlatform.OSX
        | OperatingSystem OSPlatform.Linux ->
            let bashHeader = "#!/bin/bash\n"
            let scriptFilename = sprintf "%s.sh" armTemplateName
            File.WriteAllText(scriptFilename, bashHeader + azureCliCmd)
            setLinuxExecutePermissions scriptFilename
        | _ ->
            RuntimeInformation.OSDescription 
            |> sprintf "OSPlatform: %s not supported" 
            |> System.NotImplementedException 
            |> raise

    let generateDeployScript resourceGroupName (deployment:Deployment) =
        let templateName = "farmer-deploy"
        let templateFilename = deployment.Template |> Writer.toJson |> Writer.toFile templateName
        let parameterFilename = deployment.Template |> ParameterFile.generateParametersFile

        toAzureCliCmd resourceGroupName deployment.Location templateFilename parameterFilename
        |> toScriptFile templateName

/// Executes the supplied Deployment against a resource group using a locally-installed Azure CLI.
let localDeploy resourceGroupName deployment =
    AzureCli.generateDeployScript resourceGroupName deployment
    |> System.Diagnostics.Process.Start
    |> ignore