namespace Farmer

type ResourceName =
    | ResourceName of string
    member this.Value =
        let (ResourceName path) = this
        path
namespace Farmer.Internal

open Farmer

/// A type of ARM resource e.g. Microsoft.Web/serverfarms
type ResourceType =
    | ResourceType of path:string
    member this.Value =
        let (ResourceType path) = this
        path

type WebAppExtensions = AppInsightsExtension
type AppInsights =
    { Name : ResourceName 
      Location : string
      LinkedWebsite: ResourceName }
type StorageAccount =
    { Name : ResourceName 
      Location : string
      Sku : string }
type WebApp =
    { Name : ResourceName 
      AppSettings : List<string * string>
      Extensions : WebAppExtensions Set
      Dependencies : ResourceName list }
type ServerFarm =
    { Name : ResourceName 
      Location : string
      Sku:string
      WebApps : WebApp list }

module ResourceType =
    let ServerFarm = ResourceType "Microsoft.Web/serverfarms"
    let WebSite = ResourceType "Microsoft.Web/sites"
    let StorageAccount = ResourceType "Microsoft.Storage/storageAccounts"
    let AppInsights = ResourceType "Microsoft.Insights/components"

namespace Farmer

type ArmTemplate =
    { Parameters : string list
      Variables : (string * string) list
      Outputs : (string * string) list
      Resources : obj list }