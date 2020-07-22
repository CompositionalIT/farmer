[<AutoOpen>]
module Farmer.Builders.AppInsights

open Farmer
open Farmer.CoreTypes
open Farmer.Arm.Insights

let instrumentationKey (ResourceName accountName) =
    sprintf "reference('Microsoft.Insights/components/%s').InstrumentationKey" accountName
    |> ArmExpression.create

type AppInsightsConfig =
    { Name : ResourceName }
    /// Gets the ARM expression path to the instrumentation key of this App Insights instance.
    member this.InstrumentationKey = instrumentationKey this.Name
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              LinkedWebsite = None }
        ]

type AppInsightsBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty }
    [<CustomOperation "name">]
    /// Sets the name of the App Insights instance.
    member __.Name(state:AppInsightsConfig, name) = { state with Name = ResourceName name }

let appInsights = AppInsightsBuilder()