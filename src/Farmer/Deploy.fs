module Farmer.Deploy

open Farmer.ArmBuilder
open Newtonsoft.Json
open System
open System.Diagnostics

/// Represents an Azure service principal which has permissions to
/// deploy ARM templates on the supplied Subscription ID.
type AzureCredentials =
    { ClientId : Guid
      ClientSecret : string
      TenantId : Guid }

type Outputs = Map<string, string>
type ValidationError =
    { Code : string
      Message:string
      Details :
        {| Code : string
           Message: string
           Details :
            {| Code : string
               Target : string
               Message : string
            |} array
        |} array
    }

type DeploymentRejectionError =
    | CantObtainBearerToken of {| Error : string; Error_Description : string |}
    | ValidationError of ValidationError
    | CantCreateResourceGroup of string
    | InvalidTemplateRejection of string
type ErrorDetails =
    { Code : string
      Message : string
      Details : {| Code : string; Message : string |} array }
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
type PropertyChangeType =
    | Create of {| Path:string; To: string |}
    | Delete of {| Path:string; From: string |}
    | Modify of {| Path:string; From : string; To:string |}
type ResourceChangeType = Create | Delete | Deploy | Ignore | Modify of PropertyChangeType list | NoChange
type WhatIfResponse =
    { Changes :
       {| ResourceType : string
          ResourceName : string
          ChangeType : ResourceChangeType
       |} array
    }
/// Provides the low-level API to the Azure REST API.
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
        static member Create(tenantId:Guid, clientId:Guid, clientSecret:string, subscriptionId:Guid, resourceGroup) =
            http {
                POST (sprintf "https://login.microsoftonline.com/%O/oauth2/token" tenantId)
                timeout defaultTimeout
                body
                formUrlEncoded
                    [ "grant_type", "client_credentials"
                      "client_id", string clientId
                      "client_secret", clientSecret
                      "resource", "https://management.azure.com" ]
            }
            |> toResult
            |> Result.map (fun response ->
                let bearer = response |> getContent<{| access_token:string |}>
                TemplateDeployer(bearer.access_token, subscriptionId, resourceGroup))
            |> Result.mapError getContent<{| Error:string; Error_Description:string |}>
        member __.CreateResourceGroup location =
            http {
                PUT (sprintf "https://management.azure.com/subscriptions/%O/resourcegroups/%s?api-version=2019-05-01" subscriptionId resourceGroup)
                timeout defaultTimeout
                BearerAuth accessToken
                body
                json (sprintf """{ "location": "%s", "tags": { "Deployed with": "Farmer" }}""" location)
            } |> toResult
        member __.DeployTemplate parameters deploymentName templateJson =
            let parameters = parameters |> List.map(fun (k, v) -> k, {| value = v |}) |> Map |> JsonConvert.SerializeObject
            http {
                PUT (sprintf "https://management.azure.com/subscriptions/%O/resourcegroups/%s/providers/Microsoft.Resources/deployments/%s?api-version=2019-05-01" subscriptionId resourceGroup deploymentName)
                timeout defaultTimeout
                BearerAuth accessToken
                body
                json (sprintf """{ "properties": { "mode": "Incremental", "template": %s, "parameters" : %s } }""" templateJson parameters)
            } |> toResult
        member __.ValidateTemplate parameters deploymentName templateJson =
            let parameters = parameters |> List.map(fun (k, v) -> k, {| value = v |}) |> Map |> JsonConvert.SerializeObject
            http {
                POST (sprintf "https://management.azure.com/subscriptions/%O/resourcegroups/%s/providers/Microsoft.Resources/deployments/%s/validate?api-version=2019-10-01" subscriptionId resourceGroup deploymentName)
                timeout defaultTimeout
                BearerAuth accessToken
                body
                json (sprintf """{ "properties": { "mode": "Incremental", "template": %s, "parameters" : %s } }""" templateJson parameters)
            }
            |> toResult
            |> Result.ignore
            |> Result.mapError getContent<{| Error: ValidationError |}>
        member __.WhatIf parameters deploymentName templateJson  : Result<WhatIfResponse, _> =
            let parameters = parameters |> List.map(fun (k, v) -> k, {| value = v |}) |> Map |> JsonConvert.SerializeObject
            let rec tryGetUpdate (url:System.Uri) = async {
                let response = http {
                    GET (url.ToString())
                    BearerAuth accessToken
                }
                match response.statusCode with
                | System.Net.HttpStatusCode.Accepted ->
                    do! Async.Sleep 5000
                    return! tryGetUpdate url
                | _ ->
                    let! body = response.content.ReadAsStringAsync() |> Async.AwaitTask
                    let statusData = body |> JsonConvert.DeserializeObject<{|status:string|}>
                    match statusData.status with
                    | "Failed" ->
                        return Error (sprintf "Unknown error occurred during what-if: %s" (response.content.ReadAsStringAsync().Result))
                    | _ ->
                        return Ok response
            }

            let initialResponse = http {
                POST (sprintf "https://management.azure.com/subscriptions/%O/resourcegroups/%s/providers/Microsoft.Resources/deployments/%s/whatIf?api-version=2019-10-01" subscriptionId resourceGroup deploymentName)
                BearerAuth accessToken
                body
                json (sprintf """{ "properties": { "mode": "Incremental", "template": %s, "parameters" : %s } }""" templateJson parameters)
            }

            match initialResponse.statusCode with
            | System.Net.HttpStatusCode.Accepted -> tryGetUpdate initialResponse.headers.Location |> Async.RunSynchronously
            | System.Net.HttpStatusCode.OK -> Ok initialResponse
            | _ -> Error (sprintf "Unknown error occurred during what-if: %s" (initialResponse.content.ReadAsStringAsync().Result))
            |> Result.map(fun response ->
                let content =
                    response
                    |> getContent<
                        {| properties :
                            {| changes :
                                {| ResourceId : string
                                   ChangeType : string
                                   Delta : {| Path : string; PropertyChangeType : string; Before : obj; After : obj |} array
                                |} array
                            |}
                        |}>
                { WhatIfResponse.Changes =
                    [| for change in content.properties.changes do
                        let resourcePath = change.ResourceId.[change.ResourceId.IndexOf "providers/" + "providers/".Length..]
                        let splitIndex = resourcePath.LastIndexOf '/'
                        {| ResourceType = resourcePath.[0..splitIndex - 1]
                           ResourceName = resourcePath.[splitIndex + 1..]
                           ChangeType =
                                match change.ChangeType with
                                | "Create" -> Create
                                | "Delete" -> Delete
                                | "Deploy" -> Deploy
                                | "Ignore" -> Ignore
                                | "NoChange" -> NoChange
                                | "Modify" ->
                                    Modify [
                                        for delta in change.Delta do
                                            match delta.PropertyChangeType with
                                            | "Create" -> PropertyChangeType.Create {| Path = delta.Path; To = JsonConvert.SerializeObject delta.After |}
                                            | "Delete" -> PropertyChangeType.Delete {| Path = delta.Path; From = JsonConvert.SerializeObject delta.Before |}
                                            | "Modify" -> PropertyChangeType.Modify {| Path = delta.Path; From = JsonConvert.SerializeObject delta.Before; To = JsonConvert.SerializeObject delta.After |}
                                            | c -> failwithf "Unknown property change type %s" c
                                    ]
                                | c -> failwithf "Unknown change type %s" c
                        |}
                    |]
                }
            )
        member __.GetDeploymentStatus deploymentName = Result.result {
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

/// Manages the full REST API deployment process.
module RestDeployment =
    open System.Net
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

    /// Performs a "what if" analysis on a deployment against a subscription.
    let whatIf (credentials:AzureCredentials) subscriptionId (armTemplateJson:string, parameters : (string * string) list, resourceGroupName:string) =
        let deploymentName = sprintf "FarmerDeploy%d" (getDeployNumber())
        AzureRest.TemplateDeployer.Create(credentials.TenantId, credentials.ClientId, credentials.ClientSecret, subscriptionId, resourceGroupName)
        |> Result.mapError(fun x -> x.Error)
        |> Result.bind(fun deployer -> deployer.WhatIf parameters deploymentName armTemplateJson)

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

            logMessage "Performing basic ARM validation..."
            do!
                armTemplateJson
                |> armDeploy.ValidateTemplate parameters deploymentName
                |> Result.mapError(fun e -> ValidationError e.Error)

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
                    try armDeploy.GetDeploymentStatus deploymentName |> Some
                    with _ -> None)
                |> Seq.choose id
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

    type TimeoutWebClient() =
        inherit System.Net.WebClient()
        override _.GetWebRequest uri =
            let request = base.GetWebRequest uri
            request.Timeout <- 30 * 60 * 1000
            request

    let deployApp websiteName (password:string) zipFile =
        let destinationUri = sprintf "https://%s.scm.azurewebsites.net/api/zipdeploy" websiteName
        let client = new TimeoutWebClient(Credentials = NetworkCredential("$" + websiteName, password))
        printfn "Uploading %s to %s" zipFile destinationUri
        client.UploadData(destinationUri, IO.File.ReadAllBytes zipFile) |> ignore
/// Executes the supplied Deployment against a resource group using the Azure REST API.
/// It requires a service principle containing a client id, secret and tenant ID. Use this API for unattended installs e.g. continuous deployment etc.
let fullDeploy credentials (subscriptionId:Guid) resourceGroupName parameters deployment =
    /// First add password outputs for each required web app.
    let deployment =
        { deployment with
            Template =
                let webDeployTasks = deployment.PostDeployTasks |> List.choose(function RunFromZip wd -> Some wd)
                (deployment.Template, webDeployTasks)
                ||> List.fold(fun (template:ArmTemplate) item ->
                    let password = Resources.WebApp.publishingPassword item.WebApp
                    let newOutput = ("farmer-" + item.WebApp.Value + "-deploy"), password.Eval()
                    { template with Outputs = newOutput :: deployment.Template.Outputs }) }

    let armTemplateJson = deployment.Template |> Writer.toJson

    let deploymentResult =
        (armTemplateJson, parameters, deployment.Location.Value, resourceGroupName)
        |> RestDeployment.deployTemplate credentials subscriptionId (printfn "%s")
        |> RestDeployment.reportDeploymentProgress (fun stats -> printfn "In progress (%d / %d operations)..." stats.OperationsCompleted (stats.OperationsCompleted + stats.OperationsRemaining))

    // If the deployment succeeded, upload all zip files for each web app.
    match deploymentResult.Result with
    | Ok outputs ->
        for (RunFromZip wd) in deployment.PostDeployTasks do
            let password = outputs.["farmer-" + wd.WebApp.Value + "-deploy"]
            let zipFilePath = wd.Path.GetZipPath()
            RestDeployment.deployApp wd.WebApp.Value password zipFilePath
    | Error _ ->
        ()

    deploymentResult

/// Provides access to the what-if Azure API
let whatIf credentials (subscriptionId:Guid) resourceGroupName parameters deployment =
    let armTemplateJson = deployment.Template |> Writer.toJson

    (armTemplateJson, parameters, resourceGroupName)
    |> RestDeployment.whatIf credentials subscriptionId

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

    let toAzureCliCmd resourceGroupName (Location location) templateFilename parametersFilename deployCommands =
        let deploymentName = sprintf "farmer-deploy-%d" (RestDeployment.getDeployNumber())
        let commands =
            [ "az login"
              sprintf "az group create -l %s -n %s" location resourceGroupName
              sprintf "az group deployment create -g %s -n%s --template-file %s --parameters @%s"
                  resourceGroupName
                  deploymentName
                  templateFilename
                  parametersFilename ] @ deployCommands

        commands
        |> String.concat " && "

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

    let prepareWebDeploy webAppName (zipDeployKind:ZipDeployKind) resourceGroupName =
        let packageFilename = zipDeployKind.GetZipPath()
        (sprintf """az webapp deployment source config-zip --resource-group "%s" --name "%s" --src %s""" resourceGroupName webAppName packageFilename)

    let generateDeployScript resourceGroupName (deployment:Deployment) =
        let templateName = "farmer-deploy"
        let templateFilename = deployment.Template |> Writer.toJson |> Writer.toFile templateName
        let parameterFilename = deployment.Template |> ParameterFile.generateParametersFile

        let webDeploys = [
            for (RunFromZip wd) in deployment.PostDeployTasks do
                prepareWebDeploy wd.WebApp.Value wd.Path resourceGroupName
        ]

        let script =
            toAzureCliCmd resourceGroupName deployment.Location templateFilename parameterFilename webDeploys
            |> toScriptFile templateName

        script

/// Executes the supplied Deployment against a resource group using a locally-installed Azure CLI.
let quick resourceGroupName deployment =
    deployment
    |> AzureCli.generateDeployScript resourceGroupName
    |> Process.Start
    |> fun deployment -> deployment.WaitForExit()