[<AutoOpen>]
module Farmer.Builders.AppInsightsAvailabilityTest

open Farmer
open Farmer.Arm.InsightsAvailabilityTest

type AvailabilityTestProperties =
    { Name: ResourceName
      AppInsightsName: ResourceName
      Timeout : int<Seconds>
      VisitFrequency : int<Seconds>
      Locations : AvailabilityTest.TestSiteLocation list
      WebTest : AvailabilityTest.WebTestType option }
    interface IBuilder with
        member this.ResourceId = availabilitytest.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              AppInsightsName = this.AppInsightsName
              Location = location
              Timeout = this.Timeout
              VisitFrequency = this.VisitFrequency
              Locations = this.Locations
              WebTest = this.WebTest }
        ]

type AvailabilityTestBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          AppInsightsName = ResourceName.Empty
          Timeout = 120<Seconds>
          VisitFrequency = 900<Seconds>
          Locations = List.empty
          WebTest = None }
    [<CustomOperation "name">]
    /// Sets the name of the App Insights instance.
    member _.Name(state:AvailabilityTestProperties, name) = { state with Name = ResourceName name }
    [<CustomOperation "timeout">]
    /// Sets the name of the App Insights instance.
    member _.Timeout(state:AvailabilityTestProperties, seconds) = { state with Timeout = seconds }
    [<CustomOperation "frequency">]
    /// Sets the name of the App Insights instance.
    member _.Frequency(state:AvailabilityTestProperties, frequency) = { state with VisitFrequency = frequency }
    [<CustomOperation "locations">]
    /// Sets the name of the App Insights instance.
    member _.Locations(state:AvailabilityTestProperties, locations) = { state with Locations = locations }
    [<CustomOperation "web_test">]
    /// Sets the name of the App Insights instance.
    member _.WebTest(state:AvailabilityTestProperties, webtest) = { state with WebTest = Some webtest }
    [<CustomOperation "link_to_app_insights">]
    member this.LinkToAi (state:AvailabilityTestProperties, name) = { state with AppInsightsName = name }
    member this.LinkToAi (state:AvailabilityTestProperties, name) = this.LinkToAi (state, ResourceName name)
    member this.LinkToAi (state:AvailabilityTestProperties, config:AppInsightsConfig) = this.LinkToAi (state, config.Name)

let availabilityTest = AvailabilityTestBuilder()
