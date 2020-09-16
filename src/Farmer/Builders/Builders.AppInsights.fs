[<AutoOpen>]
module Farmer.Builders.AppInsights

open Farmer
open Farmer.CoreTypes
open Farmer.Arm.Insights

let instrumentationKey (ResourceName accountName) =
    sprintf "reference('Microsoft.Insights/components/%s').InstrumentationKey" accountName
    |> ArmExpression.create

type AppInsightsConfig =
    { Name : ResourceName<WebApp.AppInsightsName>
      DisableIpMasking : bool
      SamplingPercentage : int
      Tags : Map<string,string> }
    /// Gets the ARM expression path to the instrumentation key of this App Insights instance.
    member this.InstrumentationKey = instrumentationKey this.Name
    interface IBuilder with
        member this.DependencyName = this.Name.Untyped
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              LinkedWebsite = None
              DisableIpMasking = this.DisableIpMasking
              SamplingPercentage = this.SamplingPercentage
              Tags = this.Tags }
        ]

type AppInsightsBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          DisableIpMasking = false
          SamplingPercentage = 100
          Tags = Map.empty }
    [<CustomOperation "name">]
    /// Sets the name of the App Insights instance.
    member __.Name(state:AppInsightsConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "disable_ip_masking">]
    /// Sets the name of the App Insights instance.
    member __.DisableIpMasking(state:AppInsightsConfig) = { state with DisableIpMasking = true }

    [<CustomOperation "sampling_percentage">]
    /// Sets the name of the App Insights instance.
    member __.SamplingPercentage(state:AppInsightsConfig, samplingPercentage) = { state with SamplingPercentage = samplingPercentage }

    member _.Run (state:AppInsightsConfig) =
        if state.SamplingPercentage > 100  then failwith "Sampling Percentage cannot be higher than 100%"
        elif state.SamplingPercentage <= 0 then failwith "Sampling Percentage cannot be lower than or equal to 0%"
        state

let appInsights = AppInsightsBuilder()