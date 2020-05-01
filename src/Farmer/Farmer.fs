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

type IResource =
    abstract member ResourceName : ResourceName
    abstract member ToArmObject : unit -> obj

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

type SecureParameter =
    | SecureParameter of name:string
    member this.Value = match this with SecureParameter value -> value
    /// Gets an ARM expression reference to the password e.g. parameters('my-password')
    member this.AsArmRef = sprintf "parameters('%s')" this.Value |> ArmExpression

type IParameters =
    abstract member SecureParameters : SecureParameter list

type IPostDeploy =
    abstract member Run : resourceGroupName:string -> Option<Result<string, string>>

type ResourceAction =
    | NewResource of IResource
    | MergedResource of old:IResource * replacement:IResource
    | CouldNotLocate of ResourceName
    | NotSet

type IResourceBuilder =
    abstract member BuildResources : Location -> IResource list -> ResourceAction list

[<AutoOpen>]
module ArmExpression =
    /// A helper function used when building complex ARM expressions; lifts a literal string into a quoted ARM expression
    /// e.g. text becomes 'text'. This is useful for working with functions that can mix literal values and parameters.
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

namespace Farmer.Resources

open Farmer

/// The consistency policy of a CosmosDB database.
type ConsistencyPolicy = Eventual | ConsistentPrefix | Session | BoundedStaleness of maxStaleness:int * maxIntervalSeconds : int | Strong
/// The failover policy of a CosmosDB database.
type FailoverPolicy = NoFailover | AutoFailover of secondaryLocation:Location | MultiMaster of secondaryLocation:Location
/// The kind of index to use on a CosmoDB container.
type CosmosDbIndexKind = Hash | Range
/// The datatype for the key of index to use on a CosmoDB container.
type CosmosDbIndexDataType = Number | String
/// Whether a specific feature is active or not.
type FeatureFlag = Enabled | Disabled member this.AsBoolean = match this with Enabled -> true | Disabled -> false
/// The type of disk to use.
type DiskType = StandardSSD_LRS | Standard_LRS | Premium_LRS member this.ArmValue = match this with x -> x.ToString()
/// Represents a disk in a VM.
type DiskInfo = { Size : int; DiskType : DiskType }

namespace Farmer.Models

type StorageContainerAccess =
    | Private
    | Container
    | Blob

namespace Farmer

type SecretValue =
    | ParameterSecret of SecureParameter
    | ExpressionSecret of ArmExpression
    member this.Value =
        match this with
        | ParameterSecret secureParameter -> secureParameter.AsArmRef.Eval()
        | ExpressionSecret armExpression -> armExpression.Eval()

type ArmTemplate =
    { Parameters : SecureParameter list
      Outputs : (string * string) list
      Resources : IResource list }