namespace Farmer

/// A Value is a reference to some value used by the ARM template. This can be a literal value,
/// a reference to a parameter, or a reference to a variable.
type Value =
    | Literal of string
    | Parameter of string
    | Variable of string
    member this.Value =
        match this with
        | Literal l -> l
        | Parameter p -> sprintf "parameters('%s')" p
        | Variable v -> sprintf "variables('%s')" v
    member this.QuotedValue =
        match this with
        | Literal l -> sprintf "'%s'" l
        | Parameter p -> sprintf "parameters('%s')" p
        | Variable v -> sprintf "variables('%s')" v
    member this.Command =
        match this with
        | Literal l ->
            l
        | Parameter _
        | Variable _ ->
            sprintf "[%s]" this.Value

[<AutoOpen>]
module ExpressionBuilder =
    let private escaped = function
        | Literal x -> sprintf "'%s'" x
        | x -> x.Value
    let command = sprintf "[%s(%s)]"
    let concat (elements:Value list) =
        elements
        |> List.map escaped
        |> String.concat ", "
        |> command "concat"
    let toLower =
        escaped >> command "toLower"

namespace Farmer.Internal

open Farmer

type Expressions =
    | Concat of Value list
    | ToLower of Value

type WebAppExtensions = AppInsightsExtension
type AppInsights =
    { Name : Value
      Location : Value
      LinkedWebsite: Value }
type StorageAccount =
    { Name : Value
      Location : Value
      Sku : Value }
type ResourceType =
    | ResourceType of path:string

type WebApp =
    { Name : Value
      AppSettings : List<string * Value>
      Extensions : WebAppExtensions Set
      Dependencies : (ResourceType * Value) list }
type ServerFarm =
    { Name : Value
      Location : Value
      Sku:Value
      WebApps : WebApp list }

module ResourceType =
    let ServerFarm = ResourceType "Microsoft.Web/serverfarms"
    let WebSite = ResourceType "Microsoft.Web/sites/"
    let StorageAccount = ResourceType "Microsoft.Storage/storageAccounts"
    let AppInsights = ResourceType "Microsoft.Insights/components"
    let makePath (ResourceType path, value:Value) = sprintf "[resourceId('%s', %s)]" path value.QuotedValue

namespace Farmer

open Farmer.Internal

type ArmTemplate =
    { Parameters : string list
      Variables : (string * string) list
      Outputs : (string * Value) list
      Resources : obj list }