module Dashboards

open Expecto
open Farmer
open Farmer.Insights
open Farmer.Builders

let tests = testList "Dashboards" [
    test "Create a simple dashboard" {
        // https://docs.microsoft.com/en-us/azure/azure-portal/azure-portal-dashboards-structure

        let vm = vm { name "foo"; username "foo" }
        let vmId = (vm :> IBuilder).ResourceId
        let dash = dashboard { 
            name "myDashboard" 
            title "Monitoring"
            depends_on vm
            add_markdown_part (
                { x = 0; y = 0; rowSpan = 2; colSpan = 3 },
                { title = ""; subtitle = ""; content = "## Azure Virtual Machines Overview\r\nNew team members should watch this video to get familiar with Azure Virtual Machines." }
            )
            add_markdown_part (
                { x = 3; y = 0; rowSpan = 4; colSpan = 8 },
                { title = "Test VM Dashboard"; subtitle = "Contoso"; content = "This is the team dashboard for the test VM we use on our team. Here are some useful links:\r\n\r\n1. [Getting started](https://www.contoso.com/tsgs)\r\n1. [Troubleshooting guide](https://www.contoso.com/tsgs)\r\n1. [Architecture docs](https://www.contoso.com/tsgs)" }
            )
            add_video_part (
                { x = 3; y = 0; rowSpan = 4; colSpan = 8 },
                { title = ""; subtitle = ""; url = "https://www.youtube.com/watch?v=YcylDIiKaSU&list=PLLasX02E8BPCsnETz0XAMfpLR1LIBqpgs&index=4" }
            )
            add_metrics_chart (
                { x = 0; y = 4; rowSpan = 3; colSpan = 11 },
                { interval = System.TimeSpan(1,0,0) |> IsoDateTime.OfTimeSpan; 
                   metrics = [ MetricsName.PercentageCPU ]; 
                   resourceId = vmId }
            )
            add_virtual_machine_icon ({ x = 9; y = 7; rowSpan = 2; colSpan = 2 }, vmId)
        }

        let template = arm { add_resources [ vm; dash ] }
        let jsn = template.Template |> Writer.toJson 
        let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse
        let title = jobj.SelectToken("resources[?(@.name=='myDashboard')].tags.hidden-title").ToString()
        Expect.equal title "Monitoring" "Incorrect title"
        let lenses = jobj.SelectToken("resources[?(@.name=='myDashboard')].properties.lenses")
        Expect.isNotNull lenses "Lenses missing"
    }

    test "Create a dashboard with a complex monitor part" {

        let chartTitle = "my DB monitor"
        let valueToMonitor = "dtu_consumption_percent", "DTU percentage"
        let databaseResourceId = "test"
        let lineColor = "#47BDF5"
        let dashboard2 = dashboard { 
            name "myDashboard" 
            title "DB-Monitoring"
            add_monitor_chart({ x = 0; y = 0; rowSpan = 6; colSpan = 10 },
                ({  chartSettings = {| title = chartTitle
                                       openBladeOnClick = {| openBlade = true |}
                                       visualization = {| chartType = 2; legendVisualization = null; disablePinning = true;
                                                          axisVisualization = {| y = {| isVisible = true |} |} |}
                                       metrics = [ {| resourceMetadata = {| id = databaseResourceId |}
                                                      name = fst valueToMonitor
                                                      aggregationType = 3
                                                      metricVisualization = {| displayName = snd valueToMonitor;
                                                                               color = lineColor; resourceDisplayName = null |} |} ] |} :> obj |> Some
                    chartInputs = [ {| title = chartTitle
                                       ariaLabel = null; filterCollection = null; grouping = null;
                                       visualization = {| axisVisualization = null; legendVisualization = null; chartType = null|}
                                       resolvedBladeToOpenOnClick = {| openBlade = true |}
                                       timespan = {| relative = {| duration = 3600000 |} |}
                                       timeContext = {| options = {| useDashboardTimeRange = false; grain = 1 |}
                                                        relative = {| duration = 3600000 |} |}
                                       metrics = [ {| resourceMetadata = {| id = databaseResourceId; kind = "v12.0,user" |}
                                                      name = fst valueToMonitor; metricVisualization = null;
                                                      aggregationType = 3; thresholds = []
                                                      |} ]
                                       itemDataModel = {| id = $"defaultAiChartDiv{System.Guid.NewGuid()}"; grouping = null; chartHeight = 1;
                                                          priorPeriod = false; horizontalBars = true; showOther = false; palette = "multiColor";
                                                          jsonDefinitionId = ""; yAxisOptions = {| options = 1 |}; 
                                                          appliedISOGrain = System.TimeSpan(0,1,0) |> IsoDateTime.OfTimeSpan;
                                                          title = chartTitle; titleKind = "Auto"
                                                          visualization = {| chartType = 2; legend = null; axis = null |}
                                                          metrics = [
                                                            {| id = {| resourceDefinition = {| id = databaseResourceId; name = null |}
                                                                       name = {| id = fst valueToMonitor; displayName = snd valueToMonitor |} |}
                                                               metricAggregation = 3; color = lineColor; unit = 5; useSIConversions = false; displaySIUnit = true
                                                            |} ]
                                                      |} |} ]
                    filters = {| MsPortalFx_TimeRange = {| model = {| format = "local"; granularity = "auto"; relative = "60m" |} |} |} :> obj |> Some
                }))
        }

        let template = arm { add_resources [ dashboard2 ] }
        let jsn = template.Template |> Writer.toJson 
        let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse
        System.IO.File.WriteAllText(@"c:\temp\mydash.json", jsn)
        let title = jobj.SelectToken("resources[?(@.name=='myDashboard')].tags.hidden-title").ToString()
        Expect.equal title "DB-Monitoring" "Incorrect title"
        let lenses = jobj.SelectToken("resources[?(@.name=='myDashboard')].properties.lenses")
        Expect.isNotNull lenses "Lenses missing"
    }

]