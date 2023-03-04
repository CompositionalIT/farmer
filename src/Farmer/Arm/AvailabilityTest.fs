[<AutoOpen>]
module Farmer.Arm.InsightsAvailabilityTest

open Farmer

let availabilitytest = ResourceType("Microsoft.Insights/webtests", "2015-05-01")

type AvailabilityTest =
    {
        Name: ResourceName
        AppInsightsName: ResourceName
        Timeout: int<Seconds>
        VisitFrequency: int<Seconds>
        Location: Location
        Locations: AvailabilityTest.TestSiteLocation list
        WebTest: AvailabilityTest.WebTestType option
    }

    interface IArmResource with
        member this.ResourceId = availabilitytest.resourceId this.Name

        member this.JsonModel =
            if this.AppInsightsName = ResourceName.Empty then
                raiseFarmer $"AvailabilityTest {this.Name} needs to be attached to an Application Insights."
            else
                match this.WebTest with
                | None -> raiseFarmer $"AvailabilityTest {this.Name} Webtest value has to be defined."
                | Some webTest ->
                    let appInsightResource =
                        $"[concat('hidden-link:', resourceId('{components.Type}', '{this.AppInsightsName.Value}'))]"

                    let testString =
                        match webTest with
                        | AvailabilityTest.CustomWebtestXml xml -> xml
                        | AvailabilityTest.WebsiteUrl websiteUrl ->
                            $"""<WebTest xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
                                Name="{this.Name.Value}"
                                Id="{System.Guid.NewGuid()}"
                                Enabled="True"
                                CssProjectStructure=""
                                CssIteration=""
                                Timeout="{this.Timeout}"
                                WorkItemIds=""
                                Description=""
                                CredentialUserName=""
                                CredentialPassword=""
                                PreAuthenticate="True"
                                Proxy="default"
                                StopOnError="False"
                                RecordedResultFile=""
                                ResultsLocale="">
                                <Items>
                                    <Request
                                        Method="GET"
                                        Guid="{System.Guid.NewGuid()}"
                                        Version="1.1"
                                        Url="{websiteUrl}"
                                        ThinkTime="0"
                                        Timeout="{this.Timeout}"
                                        ParseDependentRequests="True"
                                        FollowRedirects="True"
                                        RecordResult="True"
                                        Cache="False"
                                        ResponseTimeGoal="0"
                                        Encoding="utf-8"
                                        ExpectedHttpStatusCode="200"
                                        ExpectedResponseUrl=""
                                        ReportingName=""
                                        IgnoreHttpStatusCode="False" />
                                </Items>
                            </WebTest>"""

                    {| availabilitytest.Create(this.Name) with
                        location = this.Location.ArmValue
                        dependsOn = [ Farmer.ResourceId.create(components, this.AppInsightsName).Eval() ]
                        tags = Farmer.Serialization.ofJson ("{\"" + appInsightResource + "\": \"Resource\"}")
                        properties = {|
                            SyntheticMonitorId = $"{this.Name.Value.ToLower()}-{this.AppInsightsName.Value.ToLower()}"
                            Name = this.Name.Value
                            Enabled = true
                            Frequency = this.VisitFrequency
                            Timeout = this.Timeout
                            Kind = "ping"
                            RetryEnabled = true
                            Locations =
                                this.Locations
                                |> List.map (fun (AvailabilityTest.AvailabilityTestSite lo) -> {| Id = lo.ArmValue |})
                            Configuration = {|
                                WebTest =
                                    System.Text.RegularExpressions.Regex.Replace(
                                        testString.Replace("\r\n", "").Replace("\n", ""),
                                        @"\s+",
                                        " "
                                    )
                            |}
                        |}
                    |}
