namespace Farmer

open System

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

    static member (/) (a:ResourceName, b:string) = ResourceName(a.Value + "/" + b)
    static member (/) (a:ResourceName, b:ResourceName) = a / b.Value
    static member (-) (a:ResourceName, b:string) = ResourceName(a.Value + "-" + b)
    static member (-) (a:ResourceName, b:ResourceName) = a - b.Value

[<AutoOpen>]
module ResourceName =
    let (|EmptyResourceName|_|) (r:ResourceName) = if r = ResourceName.Empty then Some EmptyResourceName else None
    let (|NullOrEmpty|_|) (text:string) = if System.String.IsNullOrEmpty text then Some NullOrEmpty else None
    let (|Parsed|_|) parser (text:string) = match parser text with true, x -> Some (Parsed x) | false, _ -> None
    let (|Unparsed|_|) parser (text:string) = match parser text with true, _ -> None | false, _ -> Some Unparsed
type Location =
    | Location of string
    member this.ArmValue = match this with Location location -> location.ToLower()

type ResourceType =
    | ResourceType of path:string * version:string
    /// Returns the ARM resource type string value.
    member this.Type = match this with ResourceType (p, _) -> p
    member this.ApiVersion = match this with ResourceType (_, v) -> v

type ResourceId =
    { Type : ResourceType
      ResourceGroup : string option
      Name : ResourceName
      Segments : ResourceName list }
    static member create (resourceType:ResourceType, name:ResourceName, ?group:string) =
        { Type = resourceType; ResourceGroup = group; Name = name; Segments = [] }
    static member create (resourceType:ResourceType, name:ResourceName, [<ParamArray>] resourceSegments:ResourceName []) =
        { Type = resourceType; Name = name; ResourceGroup = None; Segments = List.ofArray resourceSegments }

type ResourceType with
    member this.resourceId name = ResourceId.create (this, name)
    member this.resourceId name = this.resourceId (ResourceName name)
    member this.resourceId (firstSegment, [<ParamArray>] remainingSegments:ResourceName []) = ResourceId.create (this, firstSegment, remainingSegments)

[<AutoOpen>]
module internal Patterns =
    let (|HasResourceType|_|) (expected:ResourceType) (actual:ResourceId) =
        match actual.Type with
        | t when t = expected -> Some (HasResourceType())
        | _ -> None

/// An Azure ARM resource value which can be mapped into an ARM template.
type IArmResource =
    /// The name of the resource, to uniquely identify against other resources in the template.
    abstract member ResourceId : ResourceId
    /// A raw object that is ready for serialization directly to JSON.
    abstract member JsonModel : obj

/// Represents a high-level configuration that can create a set of ARM Resources.
type IBuilder =
    /// Given a location and the currently-built resources, returns a set of resource actions.
    abstract member BuildResources : Location -> IArmResource list
    /// Provides the ResourceId that other resources should use when depending upon this builder.
    abstract member ResourceId : ResourceId

/// Represents an expression used within an ARM template
type ArmExpression =
    private | ArmExpression of expression:string * owner:ResourceId option
    static member create (rawText:string, ?owner) =
        if System.Text.RegularExpressions.Regex.IsMatch(rawText, @"^\[.*\]$") then
            failwith $"ARM Expressions should not be wrapped in [ ]; these will automatically be added when the expression is evaluated. Please remove them from '{rawText}'."
        else
            ArmExpression(rawText, owner)
    /// Gets the raw value of this expression.
    member this.Value = match this with ArmExpression (e, _) -> e
    /// Tries to get the owning resource of this expression.
    member this.Owner = match this with ArmExpression (_, o) -> o
    /// Applies a mapping function to the expression.
    member this.Map mapper = match this with ArmExpression (e, r) -> ArmExpression(mapper e, r)
    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    member this.Eval() =
        let specialCases = [ @"string\(\'[^\']*\'\)", 8, 10; @"^'\w*'$", 1, 2 ]
        match specialCases |> List.tryFind(fun (case, _, _) -> System.Text.RegularExpressions.Regex.IsMatch(this.Value, case)) with
        | Some (_, start, finish) -> this.Value.Substring(start, this.Value.Length - finish)
        | None -> $"[{this.Value}]"
    /// Sets the owning resource on this ARM Expression.
    member this.WithOwner(owner:ResourceId) = match this with ArmExpression (e, _) -> ArmExpression(e, Some owner)
    // /// Sets the owning resource on this ARM Expression.
    // member this.WithOwner(owner:ResourceName) = this.WithOwner(ResourceId.create owner)

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
        match this.Type.Type with
        | "" ->
            $"string('{this.Name.Value}')"
            |> ArmExpression.create
        | _ ->
            [ yield! Option.toList this.ResourceGroup
              this.Type.Type
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
        ArmExpression.create($"reference({resourceId.ArmExpression.Value}, '{resourceType.ApiVersion}')")
                     .WithOwner(resourceId)

type ResourceType with
    member this.Create(name:ResourceName, ?location:Location, ?dependsOn:ResourceId seq, ?tags:Map<string,string>) =
        match this with
        | ResourceType (path, version) ->
            {| ``type`` = path
               apiVersion = version
               name = name.Value
               location = location |> Option.map(fun r -> r.ArmValue) |> Option.toObj
               dependsOn =
                dependsOn
                |> Option.map (Seq.map(fun r -> r.Eval()) >> Seq.toArray >> box)
                |> Option.toObj
               tags = tags |> Option.map box |> Option.toObj |}

type ITaggable<'TConfig> =
    abstract member Add : 'TConfig -> list<string * string> -> 'TConfig
type IDependable<'TConfig> =
    abstract member Add : 'TConfig -> ResourceId Set -> 'TConfig

[<AutoOpen>]
module Extensions =
    module Map =
        let merge newValues map =
            (map, newValues)
            ||> List.fold (fun map (key, value) -> Map.add key value map)

    type ITaggable<'T> with
        /// Adds the provided set of tags to the builder.
        [<CustomOperation "add_tags">]
        member this.Tags(state:'T, pairs) =
            this.Add state pairs

        /// Adds the provided tag to the builder.
        [<CustomOperation "add_tag">]
        member this.Tag(state:'T, key, value) = this.Tags(state, [ key, value ])

    type IDependable<'TConfig> with
        [<CustomOperation "depends_on">]
        member this.DependsOn(state:'TConfig, builder:IBuilder) = this.DependsOn (state, builder.ResourceId)
        member this.DependsOn(state:'TConfig, builders:IBuilder list) = this.DependsOn (state, builders |> List.map (fun x -> x.ResourceId))
        member this.DependsOn(state:'TConfig, resource:IArmResource) = this.DependsOn (state, resource.ResourceId)
        member this.DependsOn(state:'TConfig, resources:IArmResource list) = this.DependsOn (state, resources |> List.map (fun x -> x.ResourceId))
        member this.DependsOn (state:'TConfig, resourceId:ResourceId) = this.DependsOn(state, [ resourceId ])
        member this.DependsOn (state:'TConfig, resourceIds:ResourceId list) = this.Add state (Set resourceIds)

/// A secure parameter to be captured in an ARM template.
type SecureParameter =
    | SecureParameter of name:string
    member this.Value = match this with SecureParameter value -> value
    /// Gets an ARM expression reference to the parameter e.g. parameters('my-password')
    member this.ArmExpression = $"parameters('{this.Value}')" |> ArmExpression.create

/// Exposes parameters which are required by a specific IArmResource.
type IParameters =
    abstract member SecureParameters : SecureParameter list

/// An action that needs to be run after the ARM template has been deployed.
type IPostDeploy =
    abstract member Run : resourceGroupName:string -> Option<Result<string, string>>

/// A functional equivalent of the IBuilder's BuildResources method.
type Builder = Location -> IArmResource list

/// A related resource that will automatically be created by Farmer as part of another resource (typically within a Builder).
type AutoGeneratedResource<'TConfig> =
    /// A resource that will automatically be created by Farmer with an explicit (user-defined) name.
    | Named of ResourceId
    /// A resource that will automatically be created by Farmer with a name that is derived based on the configuration.
    | Derived of ('TConfig -> ResourceId)
    member this.resourceId config =
        match this with
        | Named r -> r
        | Derived derive -> derive config

/// A related resource that is created externally to this Farmer builder. It may be created elsewhere in the Farmer template,
/// or it may already exist in Azure.
type LinkedResource =
    /// The id of a resource that will be created by Farmer, but is explicitly linked by the user.
    | Managed of ResourceId
    /// A id of a resource that is created externally from Farmer and already exists in Azure.
    | Unmanaged of ResourceId

/// A reference to another Azure resource that may or may not be created by Farmer.
type ResourceRef<'TConfig> =
    | AutoGeneratedResource of AutoGeneratedResource<'TConfig>
    | LinkedResource of LinkedResource
    member this.resourceId config =
        match this with
        | LinkedResource (Managed r) -> r
        | LinkedResource (Unmanaged r) -> r
        | AutoGeneratedResource r -> r.resourceId config

[<AutoOpen>]
module ResourceRef =
    /// Creates a ResourceRef which is automatically created and derived from the supplied config.
    let derived derivation = AutoGeneratedResource(Derived derivation)
    let named (resourceType:ResourceType) (name:ResourceName) = AutoGeneratedResource(Named(resourceType.resourceId name))
    let managed (resourceType:ResourceType) (name:ResourceName) = LinkedResource(Managed(resourceType.resourceId name))
    let unmanaged resourceId = LinkedResource(Unmanaged resourceId)
    /// An active pattern that returns the resource name if the resource should be set as a dependency.
    /// In other words, all cases except External Unmanaged.
    let (|DependableResource|_|) config = function
        | AutoGeneratedResource r -> Some(DependableResource(r.resourceId config))
        | LinkedResource (Managed r) -> Some (DependableResource r)
        | LinkedResource (Unmanaged _) -> None
    /// An active pattern that returns the resource name if the resource should be deployed. In other
    /// words, AutoCreate only.
    let (|DeployableResource|_|) config = function
        | AutoGeneratedResource c -> Some (DeployableResource(c.resourceId config))
        | LinkedResource _ -> None
    /// An active pattern that returns the resource name if the resource if external.
    let (|ExternalResource|_|) = function
        | AutoGeneratedResource _ ->
            None
        | LinkedResource (Managed r)
        | LinkedResource (Unmanaged r) ->
            Some r

/// Whether a specific feature is active or not.
type FeatureFlag =
    | Enabled | Disabled
    member this.AsBoolean = match this with Enabled -> true | Disabled -> false
    member this.ArmValue = match this with Enabled -> "Enabled" | Disabled -> "Disabled"

module FeatureFlag =
    let ofBool enabled = if enabled then Enabled else Disabled

/// A Principal ID represents an Identity, typically either a system or user generated Identity.
type PrincipalId =
    | PrincipalId of ArmExpression member this.ArmExpression = match this with PrincipalId e -> e

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

module internal DeterministicGuid =
    open System.Security.Cryptography
    open System.Text

    let private swapBytes(guid:byte array, left, right) =
        let temp = guid.[left]
        guid.[left] <- guid.[right]
        guid.[right] <- temp

    let private swapByteOrder guid =
        swapBytes(guid, 0, 3)
        swapBytes(guid, 1, 2)
        swapBytes(guid, 4, 5)
        swapBytes(guid, 6, 7)

    let namespaceBytes = Guid.Parse("92f3929f-622a-4149-8f39-83a4bcd385c8").ToByteArray()
    swapByteOrder namespaceBytes

    let create(source:string) =
        let source = Encoding.UTF8.GetBytes source

        let hash =
            use algorithm = SHA1.Create()
            algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0) |> ignore
            algorithm.TransformFinalBlock(source, 0, source.Length) |> ignore
            algorithm.Hash

        let newGuid = Array.zeroCreate<byte> 16
        Array.Copy(hash, 0, newGuid, 0, 16)

        newGuid.[6] <- ((newGuid.[6] &&& 0x0Fuy) ||| (5uy <<< 4))
        newGuid.[8] <- ((newGuid.[8] &&& 0x3Fuy) ||| 0x80uy)

        swapByteOrder newGuid
        Guid newGuid

module internal AssemblyInfo =
    open System.Runtime.CompilerServices
    [<assembly: InternalsVisibleTo "Tests">]
    do()