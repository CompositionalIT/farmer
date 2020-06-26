namespace Farmer

/// Represents a name of an ARM resource
type ResourceName =
    | ResourceName of string
    static member Empty = ResourceName ""
    member this.Value =
        let (ResourceName path) = this
        path
    member this.IfEmpty fallbackValue =
        match this with
        | r when r = ResourceName.Empty -> ResourceName fallbackValue
        | r -> r
    member this.Map mapper = match this with ResourceName r -> ResourceName (mapper r)

/// An Azure ARM resource value which can be mapped into an ARM template.
type IArmResource =
    /// The name of the resource, to uniquely identify against other resources in the template.
    abstract member ResourceName : ResourceName
    /// A raw object that is ready for serialization directly to JSON.
    abstract member JsonModel : obj

/// Represents a high-level configuration that can create a set of ARM Resources.
type IBuilder =
    /// Given a location and the currently-built resources, returns a set of resource actions.
    abstract member BuildResources : Location -> IArmResource list -> IArmResource list
    /// Provides the resource name that other resources should use when depending upon this builder.
    abstract member DependencyName : ResourceName

namespace Farmer.CoreTypes

open Farmer
open System

/// Represents an expression used within an ARM template
type ArmExpression =
    | ArmExpression of string
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
type Builder = Location -> IArmResource list -> IArmResource list

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
        |> ArmExpression

/// A ResourceRef represents a linked resource; typically this will be for two resources that have a relationship
/// such as AppInsights on WebApp. WebApps can automatically create and configure an AI instance for the webapp,
/// or configure the web app to an existing AI instance, or do nothing.
type ResourceRef =
      /// The resource has been created externally.
    | External of ResourceName
      /// The resource will be automatically created and its name be automatically generated.
    | AutomaticPlaceholder
      /// The resource will be automatically created using the supplied name.
    | AutomaticallyCreated of ResourceName
    member this.ResourceNameOpt = match this with External r | AutomaticallyCreated r -> Some r | AutomaticPlaceholder -> None
    member this.ResourceName = this.ResourceNameOpt |> Option.defaultValue ResourceName.Empty

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