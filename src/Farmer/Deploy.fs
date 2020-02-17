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
    | Provisioning of {| OperationsCompleted : int; OperationsRemaining : int |}
    | Provisioned of Outputs

module AzureRest =
    open FsHttp.DslCE
    type DeploymentOperationResult =
        { value :
            {| properties :
                {| ProvisioningState : string
                   TargetResource : {| ResourceType : string; ResourceName : string |}
                |}
            |} array }

    let toResult (response:FsHttp.Domain.Response) =
        match int response.statusCode with
        | code when code >= 200 && code < 300 -> Ok response
        | _ -> Error response
    let getContent<'T> (response:FsHttp.Domain.Response) =
        response.content.ReadAsStringAsync().Result
        |> JsonConvert.DeserializeObject<'T>
    let private defaultTimeout = TimeSpan.FromSeconds 120.
    type TemplateDeployer(accessToken:string, subscriptionId, resourceGroup) =
        static member Create(tenantId:Guid, clientId:Guid, clientSecret:Guid, subscriptionId:Guid, resourceGroup) =
            http {
                POST (sprintf "https://login.microsoftonline.com/%O/oauth2/token" tenantId)
                timeout defaultTimeout
                body
                formUrlEncoded
                    [ "grant_type", "client_credentials"
                      "client_id", string clientId
                      "client_secret", string clientSecret
                      "resource", "https://management.azure.com" ]
            }
            |> toResult
            |> Result.map (fun response ->
                let bearer = response |> getContent<{| access_token:string |}>
                TemplateDeployer(bearer.access_token, subscriptionId, resourceGroup))
            |> Result.mapError getContent<{| Error:string; Error_Description:string |}>
        member _.CreateResourceGroup location =
            http {
                PUT (sprintf "https://management.azure.com/subscriptions/%O/resourcegroups/%s?api-version=2019-05-01" subscriptionId resourceGroup)
                timeout defaultTimeout
                BearerAuth accessToken
                body
                json (sprintf """{ "location": "%s", "tags": { "Deployed with Farmer": "" }}""" location)
            } |> toResult
        member _.DeployTemplate parameters deploymentName templateJson =
            let parameters = parameters |> List.map(fun (k, v) -> k, {| value = v |}) |> Map |> JsonConvert.SerializeObject
            http {
                PUT (sprintf "https://management.azure.com/subscriptions/%O/resourcegroups/%s/providers/Microsoft.Resources/deployments/%s?api-version=2019-05-01" subscriptionId resourceGroup deploymentName)
                timeout defaultTimeout
                BearerAuth accessToken
                body
                json (sprintf """{ "properties": { "mode": "Incremental", "template": %s, "parameters" : %s } }""" templateJson parameters)
            } |> toResult

        member _.GetDeploymentStatus deploymentName = Result.result {
            let! deploymentDetails =
                http {
                    GET (sprintf "https://management.azure.com/subscriptions/%O/resourcegroups/%s/providers/Microsoft.Resources/deployments/%s?api-version=2018-05-01" subscriptionId resourceGroup deploymentName)
                    timeout defaultTimeout
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
                    let details =
                        http {
                            GET (sprintf "https://management.azure.com/subscriptions/%O/resourcegroups/%s/deployments/%s/operations?api-version=2019-10-01" subscriptionId resourceGroup deploymentName)
                            BearerAuth accessToken
                        } |> getContent<DeploymentOperationResult>

                    let complete, remaining =
                        details.value
                        |> Array.partition(fun r ->
                            match r.properties.ProvisioningState with
                            | "Failed" | "Succeeded" -> true
                            | _ -> false)
                        |> fun (complete, remaining) ->
                            complete.Length, remaining.Length
                    {| OperationsCompleted = complete
                       OperationsRemaining = remaining |}
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
    let deployTemplate (credentials:AzureCredentials) subscriptionId logMessage (armTemplateJson:string, parameters : (string * string) list, location:string, resourceGroupName:string) : RawDeploymentResult =
        let deploymentName = sprintf "FarmerDeploy%d" (getDeployNumber())
        let deploymentResult = Result.result {
            logMessage "Getting authorisation token..."
            let! armDeploy =
                AzureRest.TemplateDeployer.Create(credentials.TenantId, credentials.ClientId, credentials.ClientSecret, subscriptionId, resourceGroupName)
                |> Result.mapError CantObtainBearerToken

            logMessage (sprintf "Creating resource group %s..." resourceGroupName)
            do!
                armDeploy.CreateResourceGroup location
                |> Result.mapError(fun response -> CantCreateResourceGroup (response.content.ReadAsStringAsync().Result))
                |> Result.ignore

            logMessage "Starting template deployment..."
            do!
                armTemplateJson
                |> armDeploy.DeployTemplate parameters deploymentName
                |> Result.mapError(fun e -> InvalidTemplateRejection (e.content.ReadAsStringAsync().Result))
                |> Result.ignore
            logMessage "Deployment accepted."

            return
                Seq.initInfinite (fun _ ->
                    Async.Sleep 5000 |> Async.RunSynchronously
                    armDeploy.GetDeploymentStatus deploymentName)
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
let fullDeploy credentials (subscriptionId:Guid) resourceGroupName parameters deployment =
    let armTemplateJson = deployment.Template |> Writer.toJson

    (armTemplateJson, parameters, deployment.Location.Value, resourceGroupName)
    |> RestDeployment.deployTemplate credentials subscriptionId (printfn "%s")
    |> RestDeployment.reportDeploymentProgress (fun stats -> printfn "In progress (%d / %d operations)..." stats.OperationsCompleted (stats.OperationsCompleted + stats.OperationsRemaining))

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
let quick resourceGroupName deployment =
    AzureCli.generateDeployScript resourceGroupName deployment
    |> System.Diagnostics.Process.Start
    |> ignore