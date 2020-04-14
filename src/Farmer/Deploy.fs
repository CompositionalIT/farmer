module Farmer.Deploy

open Farmer.ArmBuilder
open Newtonsoft.Json
open System
open System.Diagnostics
open System.IO

let private deployFolder = ".farmer"
let private prepareDeploymentFolder() =
    if Directory.Exists deployFolder then Directory.Delete(deployFolder, true)
    Directory.CreateDirectory deployFolder |> ignore
let private getDeployNumber =
    let r = Random()
    fun () -> r.Next 10000


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

    let generateParametersFile folder (armTemplate:ArmTemplate) =
        armTemplate.Parameters
        |> List.map(fun (SecureParameter p) -> p, Passwords.generateConformingPassword 24 armTemplate)
        |> toParameters
        |> Writer.TemplateGeneration.serialize
        |> Writer.toFile folder "farmer-deploy-parameters"

/// Provides strongly-typed access to the Azure CLI
module Az =
    /// Executes a generic AZ CLI command.
    let executeAz arguments =
        let outputFile = Path.Combine(deployFolder, "output.txt")
        let p =
            ProcessStartInfo(
                FileName = "az",
                Arguments = arguments + " > " + outputFile,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden)
            |> Process.Start
        p.WaitForExit()
        let response = File.ReadAllText outputFile
        File.Delete outputFile
        match p.ExitCode with
        | 0 -> Ok response
        | _ -> Error response
    let isLoggedIn() = executeAz "account show" |> Result.map ignore
    let login() = executeAz "login" |> Result.map ignore
    let createResourceGroup location resourceGroup = executeAz (sprintf "group create -l %s -n %s" location resourceGroup)
    let deploy resourceGroup deploymentName templateFilename parametersFilename =
        sprintf "group deployment create -g %s -n%s --template-file %s --parameters @%s"
            resourceGroup
            deploymentName
            templateFilename
            parametersFilename
        |> executeAz
    let zipDeploy webAppName (zipDeployKind:ZipDeployKind) resourceGroup =
        let packageFilename = zipDeployKind.GetZipPath deployFolder
        executeAz (sprintf """webapp deployment source config-zip --resource-group "%s" --name "%s" --src %s""" resourceGroup webAppName packageFilename)

/// Executes the supplied Deployment against a resource group using the Azure CLI.
let execute resourceGroupName deployment =
    prepareDeploymentFolder()
    printf "Checking Azure CLI logged in status... "

    match Az.isLoggedIn() with
    | Ok _ -> printfn "You are already logged in!"; Ok()
    | Error _ -> printfn ""; Az.login()
    |> Result.bind(fun _ ->
        printfn "Creating resource group %s..." resourceGroupName
        Az.createResourceGroup deployment.Location.Value resourceGroupName)
    |> Result.bind(fun _ ->
        printfn "Deploying ARM template (please be patient, this can take a while)..."
        let templateFilename = deployment.Template |> Writer.toJson |> Writer.toFile deployFolder "farmer-deploy"
        let parameterFilename = ParameterFile.generateParametersFile deployFolder deployment.Template
        let deploymentName = sprintf "farmer-deploy-%d" (getDeployNumber())
        Az.deploy resourceGroupName deploymentName templateFilename parameterFilename)
    |> Result.map(fun response ->
        // First do any zip deployments
        for (RunFromZip wd) in deployment.PostDeployTasks do
            printfn "Running ZIP deploy for %s" wd.Path.Value
            //TODO: Result.sequence?
            Az.zipDeploy wd.WebApp.Value wd.Path resourceGroupName |> ignore

        printfn "All done, now parsing ARM response to get any outputs..."

        // Now return any ARM outputs from the JSON response.
        let response = response |> JsonConvert.DeserializeObject<{| properties : {| outputs : Map<string, {| value : string |}> |} |}>
        response.properties.outputs
        |> Map.map (fun _ value -> value.value)
    )