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

    static member (+) (a:ResourceName, b:string) = ResourceName(a.Value + "/" + b)
    static member (+) (a:ResourceName, b:ResourceName) = a + b.Value
    static member (/) (a:ResourceName, b:string) = a + b
    static member (/) (a:ResourceName, b:ResourceName) = a + b.Value

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
    | ResourceType of path:string * version:string
    /// Returns the ARM resource type string value.
    member this.Type = match this with ResourceType (p, _) -> p
    member this.ApiVersion = match this with ResourceType (_, v) -> v

type ResourceId =
    { Type : ResourceType option
      ResourceGroup : string option
      Name : ResourceName
      Segments : ResourceName list }
    static member Empty = { Type = None; ResourceGroup = None; Name = ResourceName.Empty; Segments = [] }
    member this.WithType resourceType = { this with Type = Some resourceType }
    static member create (name:ResourceName, ?group) =
        { ResourceId.Empty with Name = name; ResourceGroup = group }
    static member create (name:string, ?group) =
        ResourceId.create (ResourceName name, ?group = group)
    static member create (resourceType:ResourceType, name:ResourceName, ?group:string) =
        { ResourceId.Empty with Type = Some resourceType; ResourceGroup = group; Name = name }
    static member create (resourceType:ResourceType, name:ResourceName, [<ParamArray>] resourceSegments:ResourceName []) =
        { ResourceId.Empty with Type = Some resourceType; Name = name; Segments = List.ofArray resourceSegments }

/// Represents an expression used within an ARM template
type ArmExpression =
    private | ArmExpression of expression:string * owner:ResourceId option
    static member create (rawText:string, ?owner) =
        if System.Text.RegularExpressions.Regex.IsMatch(rawText, @"^\[.*\]$") then
            failwithf "ARM Expressions should not be wrapped in [ ]; these will automatically be added when the expression is evaluated. Please remove them from '%s'." rawText
        else
            ArmExpression(rawText, owner)
    /// Gets the raw value of this expression.
    member this.Value = match this with ArmExpression (e, _) -> e
    /// Tries to get the owning resource of this expression.
    member this.Owner = match this with ArmExpression (_, o) -> o
    /// Applies a mapping function to the expression.
    member this.Map mapper = match this with ArmExpression (e, r) -> ArmExpression(mapper e, r)
    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    member this.Eval() = sprintf "[%s]" this.Value
    /// Sets the owning resource on this ARM Expression.
    member this.WithOwner(owner:ResourceId) = match this with ArmExpression (e, _) -> ArmExpression(e, Some owner)
    /// Sets the owning resource on this ARM Expression.
    member this.WithOwner(owner:ResourceName) = this.WithOwner(ResourceId.create owner)

    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    static member Eval (expression:ArmExpression) = expression.Eval()
    static member Empty = ArmExpression ("", None)
    /// A helper function used when building complex ARM expressions; lifts a literal string into a
    /// quoted ARM expression e.g. text becomes 'text'. This is useful for working with functions
    /// that can mix literal values and parameters.
    static member literal = sprintf "'%s'" >> ArmExpression.create
    /// Generates an ARM expression for concatination.
    static member concat values =
        values
        |> Seq.map(fun (r:ArmExpression) -> r.Value)
        |> String.concat ", "
        |> sprintf "concat(%s)"
        |> ArmExpression.create

type ResourceId with
    member this.ArmExpression =
        match this with
        | { Type = None } ->
            this.Name.Value |> sprintf "string('%s')" |> ArmExpression.create
        | { Type = Some resourceType } ->
            [ match this.ResourceGroup with Some rg -> rg | None -> ()
              resourceType.Type
              this.Name.Value
              for segment in this.Segments do segment.Value ]
            |> List.map (sprintf "'%s'")
            |> String.concat ", "
            |> sprintf "resourceId(%s)"
            |> ArmExpression.create
    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    member this.Eval() = this.ArmExpression.Eval()

type ArmExpression with
    static member reference (resourceType:ResourceType, resourceId:ResourceId) =
        sprintf "reference(%s, '%s')" resourceId.ArmExpression.Value resourceType.ApiVersion
        |> ArmExpression.create

type ResourceType with
    member this.Create(name:ResourceName, ?location:Location, ?dependsOn:ResourceId list, ?tags:Map<string,string>) =
        match this with
        | ResourceType (path, version) ->
            {| ``type`` = path
               apiVersion = version
               name = name.Value
               location = location |> Option.map(fun r -> r.ArmValue) |> Option.toObj
               dependsOn =
                dependsOn
                |> Option.map (List.map(fun r -> r.Eval()) >> box)
                |> Option.toObj
               tags = tags |> Option.map box |> Option.toObj |}


/// A secure parameter to be captured in an ARM template.
type SecureParameter =
    | SecureParameter of name:string
    member this.Value = match this with SecureParameter value -> value
    /// Gets an ARM expression reference to the parameter e.g. parameters('my-password')
    member this.ArmExpression = sprintf "parameters('%s')" this.Value |> ArmExpression.create

/// Exposes parameters which are required by a specific IArmResource.
type IParameters =
    abstract member SecureParameters : SecureParameter list

/// An action that needs to be run after the ARM template has been deployed.
type IPostDeploy =
    abstract member Run : resourceGroupName:string -> Option<Result<string, string>>

/// A functional equivalent of the IBuilder's BuildResources method.
type Builder = Location -> IArmResource list

/// A resource that will automatically be created by Farmer.
type AutoCreationKind<'TConfig> =
    /// A resource that will automatically be created by Farmer with an explicit (user-defined) name.
    | Named of ResourceName
    /// A resource that will automatically be created by Farmer with a name that is derived based on the configuration.
    | Derived of ('TConfig -> ResourceName)
    member this.CreateResourceName config =
        match this with
        | Named r -> r
        | Derived f -> f config

/// A related resource that is created externally to this Farmer resource.
type ExternalKind =
    /// The name of the resource that will be created by Farmer, but is explicitly linked by the user.
    | Managed of ResourceName
    /// A Resource Id that is created externally from Farmer and already exists in Azure.
    | Unmanaged of ResourceId

/// A reference to another Azure resource that may or may not be created by Farmer.
type ResourceRef<'TConfig> =
    | AutoCreate of AutoCreationKind<'TConfig>
    | External of ExternalKind
    member this.CreateResourceId config =
        match this with
        | External (Managed r) -> ResourceId.create r
        | External (Unmanaged r) -> r
        | AutoCreate r -> r.CreateResourceName config |> ResourceId.create

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
    /// An active pattern that returns the resource name if the resource if external.
    let (|ExternalResource|_|) = function
        | AutoCreate c -> None
        | External (Managed r) -> Some (ResourceId.create r)
        | External (Unmanaged r) -> Some r

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
        | ParameterSecret secureParameter -> secureParameter.ArmExpression.Eval()
        | ExpressionSecret armExpression -> armExpression.Eval()

type Setting =
    | ParameterSetting of SecureParameter
    | LiteralSetting of string
    | ExpressionSetting of ArmExpression
    member this.Value =
        match this with
        | ParameterSetting secureParameter -> secureParameter.ArmExpression.Eval()
        | LiteralSetting value -> value
        | ExpressionSetting expr -> expr.Eval()
    static member AsLiteral (a,b) = a, LiteralSetting b

type ArmTemplate =
    { Parameters : SecureParameter list
      Outputs : (string * string) list
      Resources : IArmResource list }

type Deployment =
    { Location : Location
      Template : ArmTemplate
      PostDeployTasks : IPostDeploy list }