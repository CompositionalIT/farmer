namespace Farmer

/// Represents a name of an ARM resource
type ResourceName<'T> =
    | ResourceName of string
    member this.Value =
        let (ResourceName path) = this
        path
    member this.IfEmpty fallbackValue =
        match this with
        | r when r = ResourceName "" -> ResourceName fallbackValue
        | r -> r
    member this.Map mapper : ResourceName<'T> = match this with ResourceName r -> ResourceName (mapper r)
    member this.TryConvert converter : _ CheckedResourceName = match this with ResourceName r -> r |> converter
    member this.Untyped : ResourceName<unit> = ResourceName this.Value
and CheckedResourceName<'T> = Result<ResourceName<'T>, string>

/// An untyped ResourceName.
type ResourceName = ResourceName<unit>
module ResourceName =
    /// An empty, non-validated ResourceName
    let Empty : ResourceName<_> = ResourceName ""
    let unsafeWrap name = name |> ResourceName |> Ok

type Location =
    | Location of string
    member this.ArmValue = match this with Location location -> location.ToLower()

/// An Azure ARM resource value which can be mapped into an ARM template.
type IArmResource =
    /// The name of the resource, to uniquely identify against other resources in the template.
    abstract member ResourceName : ResourceName
    /// A raw object that is ready for serialization directly to JSON.
    abstract member JsonModel : obj

/// Represents a high-level configuration that can create a set of ARM Resources.
type IBuilder =
    /// Given a location and the currently-built resources, returns a set of resource actions.
    abstract member BuildResources : Location -> IArmResource list
    /// Provides the resource name that other resources should use when depending upon this builder.
    abstract member DependencyName : ResourceName

namespace Farmer.CoreTypes

open Farmer
open System

type ResourceType =
    | ResourceType of string
    /// Returns the ARM resource type string value.
    member this.ArmValue = match this with ResourceType r -> r

/// Represents an expression used within an ARM template
type ArmExpression =
    private | ArmExpression of string
    static member create (rawText:string) =
        if System.Text.RegularExpressions.Regex.IsMatch(rawText, @"^\[.*\]$") then
            failwithf "ARM Expressions should not be wrapped in [ ]; these will automatically be added when the expression is evaluated. Please remove them from '%s'." rawText
        else
            ArmExpression rawText
    /// Gets the raw value of this expression.
    member this.Value = match this with ArmExpression e -> e
    /// Applies a mapping function that itself returns an expression, to this expression.
    member this.Bind mapper : ArmExpression = mapper this.Value
    /// Applies a mapping function to the expression.
    member this.Map mapper = this.Bind (mapper >> ArmExpression)
    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    member this.Eval() = sprintf "[%s]" this.Value

    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    static member Eval (expression:ArmExpression) = expression.Eval()
    static member Empty = ArmExpression ""
    /// Builds a resourceId ARM expression from the parts of a resource ID.
    static member resourceId (ResourceType resourceType, name:ResourceName<_>, ?group:string, ?subscriptionId:string) =
        match name, group, subscriptionId with
        | name, Some group, Some sub -> sprintf "resourceId('%s', '%s', '%s', '%s')" sub group resourceType name.Value
        | name, Some group, None -> sprintf "resourceId('%s', '%s', '%s')" group resourceType name.Value
        | name, _, _ -> sprintf "resourceId('%s', '%s')" resourceType name.Value
        |> ArmExpression.create
    static member resourceId (ResourceType resourceType, [<ParamArray>] resourceSegments:ResourceName<_> []) =
        sprintf
            "resourceId('%s', %s)"
            resourceType
            (resourceSegments |> Array.map (fun r -> sprintf "'%s'" r.Value) |> String.concat ", ")
        |> ArmExpression.create

/// A secure parameter to be captured in an ARM template.
type SecureParameter =
    | SecureParameter of name:string
    member this.Value = match this with SecureParameter value -> value
    /// Gets an ARM expression reference to the parameter e.g. parameters('my-password')
    member this.AsArmRef = sprintf "parameters('%s')" this.Value |> ArmExpression

/// Exposes parameters which are required by a specific IArmResource.
type IParameters =
    abstract member SecureParameters : SecureParameter list

/// An action that needs to be run after the ARM template has been deployed.
type IPostDeploy =
    abstract member Run : resourceGroupName:string -> Option<Result<string, string>>

/// A functional equivalent of the IBuilder's BuildResources method.
type Builder = Location -> IArmResource list

[<AutoOpen>]
module ArmExpression =
    /// A helper function used when building complex ARM expressions; lifts a literal string into a
    /// quoted ARM expression e.g. text becomes 'text'. This is useful for working with functions
    /// that can mix literal values and parameters.
    let literal = sprintf "'%s'" >> ArmExpression
    /// Generates an ARM expression for concatination.
    let concat values =
        values
        |> Seq.map(fun (r:ArmExpression) -> r.Value)
        |> String.concat ", "
        |> sprintf "concat(%s)"
        |> ArmExpression.create

/// A ResourceRef represents a linked resource; typically this will be for two resources that have a relationship
/// such as AppInsights on WebApp. WebApps can automatically create and configure an AI instance for the webapp,
/// or configure the web app to an existing AI instance, or do nothing.
type AutoCreationKind<'TConfig, 'TResourceType> =
    | Named of ResourceName<'TResourceType>
    | Derived of ('TConfig -> ResourceName<'TResourceType>)
    member this.CreateResourceName config =
        match this with
        | Named r -> r
        | Derived f -> f config
type ExternalKind<'TResourceType> = Managed of ResourceName<'TResourceType> | Unmanaged of ResourceName<'TResourceType>
type ResourceRef<'TConfig, 'TResourceType> =
    | AutoCreate of AutoCreationKind<'TConfig, 'TResourceType>
    | External of ExternalKind<'TResourceType>
    member this.CreateResourceName config =
        match this with
        | External (Managed r | Unmanaged r) -> r
        | AutoCreate r -> r.CreateResourceName config
[<AutoOpen>]
module ResourceRef =
    /// Creates a ResourceRef which is automatically created and derived from the supplied config.
    let derived derivation = derivation |> Derived |> AutoCreate
    /// An active pattern that returns the resource name if the resource should be set as a dependency.
    /// In other words, all cases except External Unmanaged.
    let (|DependableResource|_|) config = function
        | External (Managed r) -> Some (DependableResource r)
        | AutoCreate r -> Some(DependableResource(r.CreateResourceName config))
        | External (Unmanaged _) -> None
    /// An active pattern that returns the resource name if the resource should be deployed. In other
    /// words, AutoCreate only.
    let (|DeployableResource|_|) config = function
        | AutoCreate c -> Some (DeployableResource(c.CreateResourceName config))
        | External _ -> None

/// Whether a specific feature is active or not.
type FeatureFlag = Enabled | Disabled member this.AsBoolean = match this with Enabled -> true | Disabled -> false

module FeatureFlag =
    let ofBool enabled = if enabled then Enabled else Disabled

/// Represents an ARM expression that evaluates to a principal ID.
type PrincipalId = PrincipalId of ArmExpression member this.ArmValue = match this with PrincipalId e -> e
type ObjectId = ObjectId of Guid

/// Represents a secret to be captured either via an ARM expression or a secure parameter.
type SecretValue =
    | ParameterSecret of SecureParameter
    | ExpressionSecret of ArmExpression
    member this.Value =
        match this with
        | ParameterSecret secureParameter -> secureParameter.AsArmRef.Eval()
        | ExpressionSecret armExpression -> armExpression.Eval()

type Setting =
    | ParameterSetting of SecureParameter
    | LiteralSetting of string
    member this.Value =
        match this with
        | ParameterSetting secureParameter -> secureParameter.AsArmRef.Eval()
        | LiteralSetting value -> value
    static member AsLiteral (a,b) = a, LiteralSetting b

type ArmTemplate =
    { Parameters : SecureParameter list
      Outputs : (string * string) list
      Resources : IArmResource list }

type Deployment =
    { Location : Location
      Template : ArmTemplate
      PostDeployTasks : IPostDeploy list }