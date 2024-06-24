[<AutoOpen>]
module Farmer.Arm.ResourceGroup

open System.Collections.Generic
open Farmer
open Farmer.Writer

let resourceGroupDeployment =
    ResourceType("Microsoft.Resources/deployments", "2020-10-01")

let resourceGroups =
    ResourceType("Microsoft.Resources/resourceGroups", "2021-04-01")

type DeploymentMode =
    | Incremental
    | Complete

type ParameterValue =
    | ParameterValue of Name: string * Value: string
    | KeyVaultReference of Name: string * KeyVaultResourceId: ResourceId * SecretName: string

    /// Gets the key for this parameter in the nested deployment's 'parameters' dictionary.
    member this.Key =
        match this with
        | ParameterValue(name, _) -> name
        | KeyVaultReference(name, _, _) -> name

    /// A parameter key value pair contains parameters objects of arbitrary structure to be
    /// serialized as JSON for template parameters.
    member internal this.ParamValue: IDictionary<string, obj> =
        match this with
        | ParameterValue(_, value) -> dict [ "value", box value ]
        | KeyVaultReference(_, kvResId, secretName) ->
            dict [
                "reference",
                dict [
                    "keyVault", dict [ "id", kvResId.Eval() ] |> box
                    "secretName", secretName |> box
                ]
                |> box
            ]

/// Represents all configuration information to generate an ARM template.
type ResourceGroupDeployment = {
    TargetResourceGroup: ResourceName
    DeploymentName: ResourceName
    Dependencies: ResourceId Set
    Outputs: Map<string, string>
    Location: Location
    Resources: IArmResource list
    /// Parameters provided to the deployment
    ParameterValues: ParameterValue list
    SubscriptionId: System.Guid option
    Mode: DeploymentMode
    Tags: Map<string, string>
} with

    member this.ResourceId = resourceGroupDeployment.resourceId this.DeploymentName

    /// Secure parameters for resources defined in this template.
    member this.Parameters =
        [
            for resource in this.Resources do
                match resource with
                | :? IParameters as p -> yield! p.SecureParameters
                | _ -> ()
        ]
        |> List.distinct

    member this.RequiredResourceGroups =
        let nestedRgs =
            this.Resources
            |> List.collect (function
                | :? ResourceGroupDeployment as rg -> rg.RequiredResourceGroups
                | _ -> [])

        List.distinct (this.TargetResourceGroup.Value :: nestedRgs)

    member this.Template = {
        Parameters = this.Parameters
        Outputs = this.Outputs |> Map.toList
        Resources = this.Resources
    }
    /// Parameters to be emitted by the outer deployment to be passed to this deployment
    interface IParameters with
        /// Secure parameters that are not provided as input on this deployment
        member this.SecureParameters =
            let inputParameterKeys = this.ParameterValues |> Seq.map (fun p -> p.Key) |> Set

            this.Parameters
            |> List.where (fun p -> inputParameterKeys |> Set.contains p.Key |> not)

    interface IArmResource with
        member this.ResourceId = this.ResourceId

        member this.JsonModel = {|
            resourceGroupDeployment.Create(
                this.DeploymentName,
                this.Location,
                dependsOn = this.Dependencies,
                tags = this.Tags
            ) with
                location = null // location is not supported for nested resource groups
                resourceGroup = this.TargetResourceGroup.Value
                subscriptionId = this.SubscriptionId |> Option.map string<System.Guid> |> Option.toObj
                properties = {|
                    template = TemplateGeneration.processTemplate this.Template
                    parameters =
                        let parameters = Dictionary<string, obj>()

                        for secureParam in this.Parameters do
                            parameters.Add(secureParam.Key, dict [ "value", $"[parameters('%s{secureParam.Value}')]" ])
                        // Let input params be used to satisfy parameters emitted for resources in the template
                        for inputParam in this.ParameterValues do
                            parameters.[inputParam.Key] <- inputParam.ParamValue

                        parameters
                    mode =
                        match this.Mode with
                        | Incremental -> "Incremental"
                        | Complete -> "Complete"
                    expressionEvaluationOptions = {| scope = "Inner" |}
                |}
        |}

/// Resource Group as a subscription level resource - only for use in deployments targeting a subscription.
type ResourceGroup = {
    Name: ResourceName
    Dependencies: ResourceId Set
    Location: Location
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.JsonModel = {|
            resourceGroups.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                properties = {| |}
        |}

        member this.ResourceId = resourceGroups.resourceId this.Name