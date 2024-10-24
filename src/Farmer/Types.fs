namespace Farmer

open System
open System.Runtime.CompilerServices

/// Common generic functions to support internals
[<AutoOpen>]
module internal Functional =
    let tuple a b = a, b

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

    member this.Map mapper =
        match this with
        | ResourceName r -> ResourceName(mapper r)

    static member (/)(a: ResourceName, b: string) = ResourceName(a.Value + "/" + b)
    static member (/)(a: ResourceName, b: ResourceName) = a / b.Value
    static member (-)(a: ResourceName, b: string) = ResourceName(a.Value + "-" + b)
    static member (-)(a: ResourceName, b: ResourceName) = a - b.Value

[<AutoOpen>]
module ResourceName =
    let (|EmptyResourceName|_|) (r: ResourceName) =
        if r = ResourceName.Empty then
            Some EmptyResourceName
        else
            None

    let (|NullOrEmpty|_|) (text: string) =
        if System.String.IsNullOrEmpty text then
            Some NullOrEmpty
        else
            None

    let (|Parsed|_|) parser (text: string) =
        match parser text with
        | true, x -> Some(Parsed x)
        | false, _ -> None

    let (|Unparsed|_|) parser (text: string) =
        match parser text with
        | true, _ -> None
        | false, _ -> Some Unparsed

type Location =
    | Location of string

    member this.ArmValue =
        match this with
        | Location location -> location.ToLower()

type DataLocation =
    | DataLocation of string

    member this.ArmValue =
        match this with
        | DataLocation dataLocation -> dataLocation

type ResourceType =
    | ResourceType of path: string * version: string

    /// Returns the ARM resource type string value.
    member this.Type =
        match this with
        | ResourceType(p, _) -> p

    member this.ApiVersion =
        match this with
        | ResourceType(_, v) -> v

    /// Empty resource type for default and comparison purposes.
    static member Empty = ResourceType("", "")

type ResourceId = {
    Type: ResourceType
    ResourceGroup: string option
    Subscription: string option
    Name: ResourceName
    Segments: ResourceName list
} with

    static member create(resourceType: ResourceType, name: ResourceName, ?group: string, ?subscription: string) = {
        Type = resourceType
        ResourceGroup = group
        Subscription = subscription
        Name = name
        Segments = []
    }

    static member create
        (resourceType: ResourceType, name: ResourceName, [<ParamArray>] resourceSegments: ResourceName[])
        =
        {
            Type = resourceType
            Name = name
            ResourceGroup = None
            Subscription = None
            Segments = List.ofArray resourceSegments
        }

type ResourceType with

    member this.resourceId(name: ResourceName) =
        match name.Value.Split('/') with
        | [||]
        | [| _ |] -> ResourceId.create (this, name)
        | parts -> ResourceId.create (this, (ResourceName parts[0]), (Array.map ResourceName parts[1..]))

    member this.resourceId name = this.resourceId (ResourceName name)

    member this.resourceId(name, groupName) =
        ResourceId.create (this, ResourceName name, group = groupName)

    member this.resourceId(firstSegment, [<ParamArray>] remainingSegments: ResourceName[]) =
        ResourceId.create (this, firstSegment, remainingSegments)

[<AutoOpen>]
module internal Patterns =
    let (|HasResourceType|_|) (expected: ResourceType) (actual: ResourceId) =
        match actual.Type with
        | t when t = expected -> Some(HasResourceType())
        | _ -> None

/// An Azure ARM resource value which can be mapped into an ARM template.
type IArmResource =
    /// The name of the resource, to uniquely identify against other resources in the template.
    abstract member ResourceId: ResourceId
    /// A raw object that is ready for serialization directly to JSON.
    abstract member JsonModel: obj

/// Represents a high-level configuration that can create a set of ARM Resources.
type IBuilder =
    /// Given a location and the currently-built resources, returns a set of resource actions.
    abstract member BuildResources: Location -> IArmResource list
    /// Provides the ResourceId that other resources should use when depending upon this builder.
    abstract member ResourceId: ResourceId

/// Represents an expression used within an ARM template
type ArmExpression =
    private
    | ArmExpression of expression: string * owner: ResourceId option

    static member create(rawText: string, ?owner) =
        if System.Text.RegularExpressions.Regex.IsMatch(rawText, @"^\[.*\]$") then
            raiseFarmer
                $"ARM Expressions should not be wrapped in [ ]; these will automatically be added when the expression is evaluated. Please remove them from '{rawText}'."
        else
            ArmExpression(rawText, owner)

    /// Gets the raw value of this expression.
    member this.Value =
        match this with
        | ArmExpression(e, _) -> e

    /// Tries to get the owning resource of this expression.
    member this.Owner =
        match this with
        | ArmExpression(_, o) -> o

    /// Applies a mapping function to the expression.
    member this.Map mapper =
        match this with
        | ArmExpression(e, r) -> ArmExpression(mapper e, r)

    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    member this.Eval() =
        let specialCases = [ @"string\(\'[^\']*\'\)", 8, 10; @"^'[^']*'$", 1, 2 ]

        match
            specialCases
            |> List.tryFind (fun (case, _, _) -> System.Text.RegularExpressions.Regex.IsMatch(this.Value, case))
        with
        | Some(_, start, finish) -> this.Value.Substring(start, this.Value.Length - finish)
        | None -> $"[{this.Value}]"

    /// Sets the owning resource on this ARM Expression.
    member this.WithOwner(owner: ResourceId) =
        match this with
        | ArmExpression(e, _) -> ArmExpression(e, Some owner)
    // /// Sets the owning resource on this ARM Expression.
    // member this.WithOwner(owner:ResourceName) = this.WithOwner(ResourceId.create owner)

    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    static member Eval(expression: ArmExpression) = expression.Eval()
    static member Empty = ArmExpression("", None)
    /// A helper function used when building complex ARM expressions; lifts a literal string into a
    /// quoted ARM expression e.g. text becomes 'text'. This is useful for working with functions
    /// that can mix literal values and parameters.
    static member literal = sprintf "'%s'" >> ArmExpression.create

    /// Generates an ARM expression for concatination.
    static member concat(values: ArmExpression seq) =
        values
        |> Seq.map _.Value
        |> String.concat ", "
        |> sprintf "concat(%s)"
        |> ArmExpression.create

    static member string(value: ArmExpression) =
        value.Value |> sprintf "string(%s)" |> ArmExpression.create

type ResourceId with

    member this.ArmExpression =
        match this.Type.Type with
        | "" -> $"string('{this.Name.Value}')" |> ArmExpression.create
        | _ ->
            [
                yield! Option.toList this.Subscription
                yield! Option.toList this.ResourceGroup
                this.Type.Type
                this.Name.Value
                for segment in this.Segments do
                    segment.Value
            ]
            |> List.map (fun x -> if x.StartsWith("[") then x.Trim('[', ']') else $"'%s{x}'") // Fixes case where a template e.g. [resourceGroup().name] is used.
            |> String.concat ", "
            |> sprintf "resourceId(%s)"
            |> ArmExpression.create

    /// Evaluates the expression for emitting into an ARM template. That is, wraps it in [].
    member this.Eval() = this.ArmExpression.Eval()
    static member Eval(resourceId: ResourceId) = resourceId.ArmExpression.Eval()

    /// Empty ResourceId for default and comparison purposes.
    static member Empty = ResourceId.create (ResourceType.Empty, ResourceName.Empty)

    static member internal AsIdObject(resourceId: ResourceId) = {| id = resourceId.Eval() |}

type ArmExpression with

    static member reference(resourceId: ResourceId) =
        ArmExpression
            .create($"reference({resourceId.ArmExpression.Value}, '{resourceId.Type.ApiVersion}')")
            .WithOwner(resourceId)

    static member reference(resourceType: ResourceType, resourceId: ResourceId) =
        ArmExpression
            .create($"reference({resourceId.ArmExpression.Value}, '{resourceType.ApiVersion}')")
            .WithOwner(resourceId)

    static member listKeys(resourceType: ResourceType, resourceId: ResourceId) =
        ArmExpression
            .create($"listkeys({resourceId.ArmExpression.Value}, '{resourceType.ApiVersion}')")
            .WithOwner(resourceId)

type ResourceType with

    member this.Create
        (name: ResourceName, ?location: Location, ?dependsOn: ResourceId seq, ?tags: Map<string, string>)
        =
        match this with
        | ResourceType(path, version) -> {|
            ``type`` = path
            apiVersion = version
            name = name.Value
            location = location |> Option.map _.ArmValue |> Option.toObj
            dependsOn =
                dependsOn
                |> Option.map (Seq.map (fun r -> r.Eval()) >> Seq.toArray >> box)
                |> Option.toObj
            tags = tags |> Option.map box |> Option.toObj
          |}

/// A secure parameter to be captured in an ARM template.
type SecureParameter =
    | SecureParameter of name: string

    member this.Value =
        match this with
        | SecureParameter value -> value

    /// Gets an ARM expression reference to the parameter e.g. parameters('my-password')
    member this.ArmExpression = ArmExpression.create $"parameters('{this.Value}')"

    /// Gets the key for this parameter in the ARM template 'parameters' dictionary.
    member this.Key =
        match this with
        | SecureParameter name -> name

/// Exposes parameters which are required by a specific IArmResource.
type IParameters =
    abstract member SecureParameters: SecureParameter list

/// An action that needs to be run after the ARM template has been deployed.
type IPostDeploy =
    abstract member Run: resourceGroupName: string -> Option<Result<string, string>>

/// A functional equivalent of the IBuilder's BuildResources method.
type Builder = Location -> IArmResource list
/// A sentinel value to use when specifying a property should be set to an automatically-generated value
type Automatic = Automatic

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

    member this.ResourceId =
        match this with
        | Managed resId
        | Unmanaged resId -> resId

    member this.Name = this.ResourceId.Name

    static member addToSetIfManaged =
        function
        | Managed x -> Set.add x
        | _ -> id

    static member internal AsIdObject(linkedResource: LinkedResource) =
        linkedResource.ResourceId |> ResourceId.AsIdObject


/// A reference to another Azure resource that may or may not be created by Farmer.
type ResourceRef<'TConfig> =
    | AutoGeneratedResource of AutoGeneratedResource<'TConfig>
    | LinkedResource of LinkedResource

    member this.resourceId config =
        match this with
        | LinkedResource r -> r.ResourceId
        | AutoGeneratedResource r -> r.resourceId config

    member this.toLinkedResource config =
        match this with
        | LinkedResource r -> r
        | AutoGeneratedResource r -> Managed(r.resourceId config)

//Choose whether you'd like an auto generated app service managed certificate or if you have your own custom certificate of
type CertificateOptions =
    | AppManagedCertificate
    | CustomCertificate of thumbprint: ArmExpression

type DomainConfig =
    | SecureDomain of domain: string * cert: CertificateOptions
    | InsecureDomain of domain: string

    member this.DomainName =
        match this with
        | SecureDomain(domainName, _)
        | InsecureDomain(domainName) -> domainName


[<AutoOpen>]
module ResourceRef =
    /// Creates a ResourceRef which is automatically created and derived from the supplied config.
    let derived derivation =
        AutoGeneratedResource(Derived derivation)

    let named (resourceType: ResourceType) (name: ResourceName) =
        AutoGeneratedResource(Named(resourceType.resourceId name))

    let managed (resourceType: ResourceType) (name: ResourceName) =
        LinkedResource(Managed(resourceType.resourceId name))

    let unmanaged resourceId = LinkedResource(Unmanaged resourceId)

    /// An active pattern that returns the resource name if the resource should be set as a dependency.
    /// In other words, all cases except External Unmanaged.
    let (|DependableResource|_|) config =
        function
        | AutoGeneratedResource r -> Some(DependableResource(r.resourceId config))
        | LinkedResource(Managed r) -> Some(DependableResource r)
        | LinkedResource(Unmanaged _) -> None

    /// An active pattern that returns the resource name if the resource should be deployed. In other
    /// words, AutoCreate only.
    let (|DeployableResource|_|) config =
        function
        | AutoGeneratedResource c -> Some(DeployableResource(c.resourceId config))
        | LinkedResource _ -> None

    /// An active pattern that returns the resource name if the resource if external.
    let (|ExternalResource|_|) =
        function
        | AutoGeneratedResource _ -> None
        | LinkedResource(Managed r)
        | LinkedResource(Unmanaged r) -> Some r

/// Whether a specific feature is active or not.
type FeatureFlag =
    | Enabled
    | Disabled

    member this.AsBoolean =
        match this with
        | Enabled -> true
        | Disabled -> false

    member this.BooleanValue =
        match this with
        | Enabled -> "true"
        | Disabled -> "false"

    member this.ArmValue =
        match this with
        | Enabled -> "Enabled"
        | Disabled -> "Disabled"

[<AbstractClass; Sealed; Extension>]
type FeatureFlagExtensions =

    [<Extension>]
    static member AsBoolean (featureFlag : FeatureFlag option) =
        featureFlag
        |> Option.map (fun f -> f.AsBoolean)
        |> Option.toNullable

    [<Extension>]
    static member BooleanValue (featureFlag : FeatureFlag option) =
        featureFlag
        |> Option.map (fun f -> f.BooleanValue)
        |> Option.toObj

    [<Extension>]
    static member ArmValue (featureFlag : FeatureFlag option) =
        featureFlag
        |> Option.map (fun f -> f.ArmValue)
        |> Option.toObj

module FeatureFlag =
    let ofBool enabled = if enabled then Enabled else Disabled
    let toBool (flag: FeatureFlag) = flag.AsBoolean

    let invert flag =
        match flag with
        | Enabled -> Disabled
        | Disabled -> Enabled

/// A Principal ID represents an Identity, typically either a system or user generated Identity.
type PrincipalId =
    | PrincipalId of ArmExpression

    member this.ArmExpression =
        match this with
        | PrincipalId e -> e

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

    static member AsLiteral(a, b) = a, LiteralSetting b

type ArmTemplate = {
    Parameters: SecureParameter list
    Outputs: (string * string) list
    Resources: IArmResource list
}

type Deployment = {
    Location: Location
    Template: ArmTemplate
    PostDeployTasks: IPostDeploy list
    RequiredResourceGroups: string list
    Tags: Map<string, string>
} with

    interface IDeploymentSource with
        member this.Deployment = this

and IDeploymentSource =
    abstract member Deployment: Deployment

module internal DeterministicGuid =
    open System.Security.Cryptography
    open System.Text

    let private swapBytes (guid: byte array, left, right) =
        let temp = guid[left]
        guid[left] <- guid[right]
        guid[right] <- temp

    let private swapByteOrder guid =
        swapBytes (guid, 0, 3)
        swapBytes (guid, 1, 2)
        swapBytes (guid, 4, 5)
        swapBytes (guid, 6, 7)

    let namespaceBytes =
        Guid.Parse("92f3929f-622a-4149-8f39-83a4bcd385c8").ToByteArray()

    swapByteOrder namespaceBytes

    let create (source: string) =
        let source = Encoding.UTF8.GetBytes source

        let hash =
            use algorithm = SHA1.Create()

            algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0)
            |> ignore

            algorithm.TransformFinalBlock(source, 0, source.Length) |> ignore
            algorithm.Hash

        let newGuid = Array.zeroCreate<byte> 16
        Array.Copy(hash, 0, newGuid, 0, 16)

        newGuid[6] <- ((newGuid[6] &&& 0x0Fuy) ||| (5uy <<< 4))
        newGuid[8] <- ((newGuid[8] &&& 0x3Fuy) ||| 0x80uy)

        swapByteOrder newGuid
        Guid newGuid

module internal AssemblyInfo =
    open System.Runtime.CompilerServices

    [<assembly: InternalsVisibleTo "Tests">]
    do ()