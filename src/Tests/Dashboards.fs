module Dashboards

open Expecto
open Farmer
open Farmer.Insights
open Farmer.Builders

let tests =
    testList
        "Dashboards"
        [
            test "Create a simple dashboard" {
                // https://docs.microsoft.com/en-us/azure/azure-portal/azure-portal-dashboards-structure

                let vm =
                    vm {
                        name "foo"
                        username "foo"
                    }

                let vmId = (vm :> IBuilder).ResourceId

                let dash =
                    dashboard {
                        name "myDashboard"
                        title "Monitoring"
                        depends_on vm

                        add_markdown_part (
                            {
                                x = 0
                                y = 0
                                rowSpan = 2
                                colSpan = 3
                            },
                            {
                                title = ""
                                subtitle = ""
                                content =
                                    "## Azure Virtual Machines Overview\r\nNew team members should watch this video to get familiar with Azure Virtual Machines."
                            }
                        )

                        add_markdown_part (
                            {
                                x = 3
                                y = 0
                                rowSpan = 4
                                colSpan = 8
                            },
                            {
                                title = "Test VM Dashboard"
                                subtitle = "Contoso"
                                content =
                                    "This is the team dashboard for the test VM we use on our team. Here are some useful links:\r\n\r\n1. [Getting started](https://www.contoso.com/tsgs)\r\n1. [Troubleshooting guide](https://www.contoso.com/tsgs)\r\n1. [Architecture docs](https://www.contoso.com/tsgs)"
                            }
                        )

                        add_video_part (
                            {
                                x = 3
                                y = 0
                                rowSpan = 4
                                colSpan = 8
                            },
                            {
                                title = ""
                                subtitle = ""
                                url =
                                    "https://www.youtube.com/watch?v=YcylDIiKaSU&list=PLLasX02E8BPCsnETz0XAMfpLR1LIBqpgs&index=4"
                            }
                        )

                        add_metrics_chart (
                            {
                                x = 0
                                y = 4
                                rowSpan = 3
                                colSpan = 11
                            },
                            {
                                interval = System.TimeSpan(1, 0, 0) |> IsoDateTime.OfTimeSpan
                                metrics = [ MetricsName.PercentageCPU ]
                                resourceId = vmId
                            }
                        )

                        add_virtual_machine_icon (
                            {
                                x = 9
                                y = 7
                                rowSpan = 2
                                colSpan = 2
                            },
                            vmId
                        )
                    }

                let template = arm { add_resources [ vm; dash ] }
                let jsn = template.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                let title =
                    jobj
                        .SelectToken("resources[?(@.name=='myDashboard')].tags.hidden-title")
                        .ToString()

                Expect.equal title "Monitoring" "Incorrect title"

                let lenses =
                    jobj.SelectToken("resources[?(@.name=='myDashboard')].properties.lenses")

                Expect.isNotNull lenses "Lenses missing"
            }

            test "Create a complex dashboard with monitor parts" {

                /// Azure ARM-template dasboard that creates a clock of selected timezone
                /// This is example of add_custom_lens.
                let clockPart (tz: System.TimeZoneInfo) : Farmer.Arm.Dashboard.LensMetadata =
                    {
                        ``type`` = "Extension/HubsExtension/PartType/ClockPart"
                        settings =
                            {|
                                content =
                                    {|
                                        settings =
                                            {|
                                                timezoneId = tz.Id
                                                timeFormat = "HH:mm"
                                                version = 1
                                            |}
                                    |}
                            |}
                            |> box
                        inputs = []
                        filters = None
                        asset = Unchecked.defaultof<Farmer.Arm.Dashboard.LensAsset>
                        isAdapter = System.Nullable()
                        defaultMenuItemId = null
                    }

                /// Azure ARM-template dasboard graph of Virtual Machine CPU usage
                let virtualMachineCPU (vmResourceId: Farmer.ResourceId) : Farmer.Arm.Dashboard.MonitorChartParameters =
                    let vmName = vmResourceId.Name
                    let insightsName = "Percentage CPU" // See: Farmer.Insights.MetricsName.PercentageCPU
                    let title = $"{insightsName} - {vmName.Value}"
                    let timeSpan = "PT1M" // See: Farmer.IsoDateTime.OfTimeSpan System.TimeSpan(0,1,0)

                    {
                        chartSettings =
                            {|
                                title = title
                                titleKind = 2
                                visualization =
                                    {|
                                        chartType = 3
                                        disablePinning = true
                                        legendVisualization =
                                            {|
                                                isVisible = true
                                                position = 2
                                                hideSubtitle = false
                                            |}
                                        axisVisualization =
                                            {|
                                                y = {| isVisible = true; axisType = 1 |}
                                                x = {| isVisible = true; axisType = 2 |}
                                            |}
                                    |}
                                metrics =
                                    [
                                        {|
                                            resourceMetadata = {| id = vmResourceId.Eval() |}
                                            name = insightsName
                                            aggregationType = 4
                                            ``namespace`` = Farmer.Arm.Compute.virtualMachines.Type
                                            metricVisualization =
                                                {|
                                                    displayName = insightsName
                                                    color = "#47BDF5"
                                                    resourceDisplayName = vmName.Value
                                                |}
                                        |}
                                    ]
                            |}
                            :> obj
                        chartInputs =
                            [
                                {|
                                    title = title
                                    visualization = {| chartType = 3 |}
                                    timeContext =
                                        {|
                                            relative = {| duration = 3600000 |}
                                            options =
                                                {|
                                                    useDashboardTimeRange = false
                                                    grain = 1
                                                    appliedISOGrain = timeSpan
                                                |}
                                        |}
                                    metrics =
                                        [
                                            {|
                                                name = title
                                                resourceMetadata = {| resourceId = vmResourceId.Eval() |}
                                                ``type`` = "host"
                                                aggregationType = 3 (* or 1, do you want Max or Avg ?*)
                                            |}
                                        ]
                                    itemDataModel =
                                        {|
                                            id = System.Guid.NewGuid()
                                            appliedISOGrain = timeSpan
                                            chartHeight = 1
                                            priorPeriod = false
                                            horizontalBars = true
                                            showOther = false
                                            palette = "multiColor"
                                            jsonDefinitionId = ""
                                            version = {| major = 1; minor = 0; build = 0 |}
                                            filters =
                                                {|
                                                    filterType = 0
                                                    id = System.Guid.NewGuid()
                                                    OperandFilters = []
                                                    LogicalOperator = 0
                                                |}
                                            yAxisOptions = {| options = 1 |}
                                            title = insightsName
                                            titleKind = "Auto"
                                            visualization = {| chartType = 3 |}
                                            metrics =
                                                [
                                                    {|
                                                        metricAggregation = 4
                                                        color = "#47BDF5" // "#7E58FF"
                                                        unit = 5
                                                        useSIConversions = true
                                                        displaySIUnit = true
                                                        id =
                                                            {|
                                                                name = {| id = title; displayName = title |}
                                                                kind = {| id = "host" |}
                                                                ``namespace`` =
                                                                    {|
                                                                        name = Farmer.Arm.Compute.virtualMachines.Type
                                                                    |}
                                                                resourceDefinition =
                                                                    {|
                                                                        resourceId = vmResourceId.Eval()
                                                                        id = vmResourceId.Eval()
                                                                        name = vmName.Value
                                                                    |}
                                                            |}
                                                    |}
                                                ]
                                        |}
                                |}
                            ]
                        filters = None
                    }

                /// Azure ARM-template dasboard graph of SQL Server DTU usage
                let databaseUtilization
                    (databaseResourceId: Farmer.ResourceId)
                    : Farmer.Arm.Dashboard.MonitorChartParameters =
                    let title = $"Resource utilization database - {databaseResourceId.Name.Value}"
                    let insightsName = "dtu_consumption_percent" // See: Farmer.Insights.MetricsName.SQL_DB_DTU
                    let insightsNameClear = "DTU percentage"
                    let timeSpan = "PT1M"

                    {
                        filters =
                            {|
                                MsPortalFx_TimeRange =
                                    {|
                                        model =
                                            {|
                                                format = "local"
                                                granularity = "auto"
                                                relative = "60m"
                                            |}
                                    |}
                            |}
                            :> obj
                        chartSettings =
                            {|
                                title = title
                                openBladeOnClick = {| openBlade = true |}
                                visualization =
                                    {|
                                        chartType = 2
                                        legendVisualization = null
                                        disablePinning = true
                                        axisVisualization = {| y = {| isVisible = true |} |}
                                    |}
                                metrics =
                                    [
                                        {|
                                            resourceMetadata = {| id = databaseResourceId.Eval() |}
                                            name = insightsName
                                            aggregationType = 3
                                            metricVisualization =
                                                {|
                                                    displayName = insightsNameClear
                                                    color = "#47BDF5"
                                                    resourceDisplayName = null
                                                |}
                                        |}
                                    ]
                            |}
                            :> obj
                        chartInputs =
                            [
                                {|
                                    title = title
                                    ariaLabel = null
                                    filterCollection = null
                                    grouping = null
                                    visualization =
                                        {|
                                            axisVisualization = null
                                            legendVisualization = null
                                            chartType = null
                                        |}
                                    resolvedBladeToOpenOnClick = {| openBlade = true |}
                                    timespan =
                                        {|
                                            relative = {| duration = 3600000 |}
                                        |}
                                    timeContext =
                                        {|
                                            options =
                                                {|
                                                    useDashboardTimeRange = false
                                                    grain = 1
                                                |}
                                            relative = {| duration = 3600000 |}
                                        |}
                                    metrics =
                                        [
                                            {|
                                                resourceMetadata =
                                                    {|
                                                        id = databaseResourceId.Eval()
                                                        kind = "v12.0,user"
                                                    |}
                                                name = insightsName
                                                metricVisualization = null
                                                aggregationType = 3
                                                thresholds = []
                                            |}
                                        ]
                                    itemDataModel =
                                        {|
                                            id = $"defaultAiChartDiv{System.Guid.NewGuid()}"
                                            grouping = null
                                            chartHeight = 1
                                            priorPeriod = false
                                            horizontalBars = true
                                            showOther = false
                                            palette = "multiColor"
                                            jsonDefinitionId = ""
                                            yAxisOptions = {| options = 1 |}
                                            appliedISOGrain = timeSpan
                                            title = title
                                            titleKind = "Auto"
                                            visualization =
                                                {|
                                                    chartType = 2
                                                    legend = null
                                                    axis = null
                                                |}
                                            metrics =
                                                [
                                                    {|
                                                        id =
                                                            {|
                                                                resourceDefinition =
                                                                    {|
                                                                        id = databaseResourceId.Eval()
                                                                        name = null
                                                                    |}
                                                                name =
                                                                    {|
                                                                        id = insightsName
                                                                        displayName = insightsNameClear
                                                                    |}
                                                            |}
                                                        metricAggregation = 3
                                                        color = "#47BDF5"
                                                        unit = 5
                                                        useSIConversions = false
                                                        displaySIUnit = true
                                                    |}
                                                ]
                                        |}
                                |}
                            ]
                    }


                /// Azure ARM-template dasboard graph of Application Insights, user page render times. Needs the Javascript to the page.
                let appInsights_PageResponseTimes
                    (appInsightsId: Farmer.ResourceId)
                    : Farmer.Arm.Dashboard.MonitorChartParameters =
                    let timeSpan = "PT5M"

                    {
                        filters =
                            {|
                                MsPortalFx_TimeRange =
                                    {|
                                        model =
                                            {|
                                                format = "local"
                                                granularity = "auto"
                                                relative = "1440m"
                                            |}
                                    |}
                            |}
                            :> obj
                        chartSettings =
                            {|
                                title = "Avg Page load network connect time, Avg Client processing time"
                                visualization =
                                    {|
                                        chartType = 3
                                        legendVisualization =
                                            {|
                                                isVisible = true
                                                position = 2
                                                hideSubtitle = false
                                            |}
                                        axisVisualization = {| y = {| isVisible = true |} |}
                                        disablePinning = true
                                    |}
                                metrics =
                                    [
                                        "networkDuration", "Page load network connect time", "#47BDF5"
                                        "processingDuration", "Client processing time", "#7E58FF"
                                        "sendDuration", "Send request time", "#44F1C8"
                                        "receiveDuration", "Receiving response time", "#EB9371"
                                    ]
                                    |> List.map (fun (typ, nam, col) ->
                                        {|
                                            resourceMetadata =
                                                {|
                                                    id = appInsightsId.Eval()
                                                    kind = "Historical"
                                                |}
                                            name = $"browserTimings/{typ}"
                                            aggregationType = 4
                                            ``namespac`` = Farmer.Arm.Insights.components.Type
                                            metricVisualization = {| displayName = nam; color = col |}
                                        |})
                            |}
                            :> obj
                        chartInputs =
                            [
                                {|
                                    title = "Avg Page load network connect time, Avg Client processing time"
                                    visualization =
                                        {|
                                            chartType = 3
                                            legend =
                                                {|
                                                    isVisible = true
                                                    position = 2
                                                    hideSubtitle = false
                                                |}
                                        |}
                                    timeContext =
                                        {|
                                            options =
                                                {|
                                                    useDashboardTimeRange = false
                                                    grain = 1
                                                    appliedISOGrain = timeSpan
                                                |}
                                            relative = {| duration = 86400000 |}
                                        |}
                                    metrics =
                                        [ "networkDuration"; "processingDuration"; "sendDuration"; "receiveDuration" ]
                                        |> List.map (fun ctype ->
                                            {|
                                                name = $"browserTimings/{ctype}"
                                                ``type`` = "Historical"
                                                resourceMetadata = {| resourceId = appInsightsId.Eval() |}
                                                aggregationType = 1
                                            |})
                                    itemDataModel =
                                        {|
                                            id = System.Guid.NewGuid()
                                            chartHeight = 1
                                            priorPeriod = false
                                            horizontalBars = true
                                            showOther = false
                                            aggregation = 1
                                            palette = "multiColor"
                                            jsonDefinitionId = System.Guid.NewGuid()
                                            titleKind = "Auto"
                                            version = {| major = 1; minor = 0; build = 0 |}
                                            yAxisOptions = {| options = 1 |}
                                            appliedISOGrain = timeSpan
                                            filters =
                                                {|
                                                    filterType = 0
                                                    id = System.Guid.NewGuid()
                                                    OperandFilters = []
                                                    LogicalOperator = 0
                                                |}
                                            title = "Avg Page load network connect time, Avg Client processing time"
                                            visualization =
                                                {|
                                                    chartType = 3
                                                    legend =
                                                        {|
                                                            isVisible = true
                                                            position = 2
                                                            hideSubtitle = false
                                                        |}
                                                |}
                                            metrics =
                                                [
                                                    "networkDuration", "Page load network connect time", "#47BDF5"
                                                    "processingDuration", "Client processing time", "#7E58FF"
                                                    "sendDuration", "Send request time", "#44F1C8"
                                                    "receiveDuration", "Receiving response time", "#EB9371"
                                                ]
                                                |> List.map (fun (typ, nam, col) ->
                                                    {|
                                                        metricAggregation = 4
                                                        color = col
                                                        id =
                                                            {|
                                                                dataSource = 1
                                                                resourceDefinition = {| id = appInsightsId.Eval() |}
                                                                name =
                                                                    {|
                                                                        id = $"browserTimings/{typ}"
                                                                        displayName = nam
                                                                    |}
                                                                ``namespace`` =
                                                                    {|
                                                                        name = Farmer.Arm.Insights.components.Type
                                                                    |}
                                                                kind =
                                                                    {|
                                                                        id = "Historical"
                                                                        displayName = "Historical"
                                                                    |}
                                                            |}
                                                    |})
                                        |}
                                |}
                            ]
                    }

                /// Azure ARM-template dasboard graph of Application Insights, unique users count. Needs the Javascript to the page.
                let appInsights_UniqueUsers
                    (appInsightsId: Farmer.ResourceId)
                    : Farmer.Arm.Dashboard.MonitorChartParameters =
                    let timeSpan = "PT5M"

                    {
                        filters =
                            {|
                                MsPortalFx_TimeRange =
                                    {|
                                        model =
                                            {|
                                                format = "local"
                                                granularity = "auto"
                                                relative = "60m"
                                            |}
                                    |}
                                ``type`` =
                                    {|
                                        model =
                                            {|
                                                operator = "equals"
                                                values = [ "pageView"; "customEvent"; "request" ]
                                            |}
                                    |}
                                ``operation/synthetic`` =
                                    {|
                                        model =
                                            {|
                                                operator = "equals"
                                                values = [ "False" ]
                                            |}
                                    |}
                            |}
                            :> obj
                        chartSettings =
                            {|
                                title = "Unique Users"
                                visualization =
                                    {|
                                        chartType = 1
                                        legendVisualization = null
                                        disablePinning = true
                                        axisVisualization = {| y = {| isVisible = true |} |}
                                    |}
                                filterCollection =
                                    {|
                                        filters =
                                            [
                                                {|
                                                    key = "type"
                                                    operator = 0
                                                    values = [ "pageView"; "customEvent"; "request" ]
                                                |}
                                                {|
                                                    key = "operation/synthetic"
                                                    operator = 0
                                                    values = [ "False" ]
                                                |}
                                            ]
                                    |}
                                openBladeOnClick =
                                    {|
                                        openBlade = true
                                        destinationBlade =
                                            {|
                                                parameters =
                                                    {|
                                                        id = appInsightsId.Eval()
                                                        menuid = "segmentationUsers"
                                                    |}
                                                bladeName = "ResourceMenuBlade"
                                                extensionName = "HubsExtension"
                                                options =
                                                    {|
                                                        parameters =
                                                            {|
                                                                id = appInsightsId.Eval()
                                                                menuid = "segmentationUsers"
                                                            |}
                                                    |}
                                            |}
                                    |}
                                metrics =
                                    [
                                        {|
                                            resourceMetadata = {| id = appInsightsId.Eval() |}
                                            name = "users/count"
                                            aggregationType = 5
                                            ``namespace`` = "microsoft.insights/components/kusto"
                                            metricVisualization =
                                                {|
                                                    displayName = "Users"
                                                    color = "#4683de"
                                                |}
                                        |}
                                    ]
                            |}
                            :> obj
                        chartInputs =
                            [
                                {|
                                    title = "Unique Users"
                                    ariaLabel = null
                                    grouping = null
                                    visualization =
                                        {|
                                            axisVisualization = null
                                            legendVisualization = null
                                            chartType = 1
                                        |}
                                    filterCollection =
                                        {|
                                            filters =
                                                [
                                                    {|
                                                        key = "type"
                                                        values = [ "pageView"; "customEvent"; "request" ]
                                                    |}
                                                    {|
                                                        key = "operation/synthetic"
                                                        values = [ "False" ]
                                                    |}
                                                ]
                                        |}
                                    timeContext =
                                        {|
                                            options =
                                                {|
                                                    useDashboardTimeRange = false
                                                    grain = 1
                                                |}
                                            relative = {| duration = 3600000 |}
                                        |}
                                    resolvedBladeToOpenOnClick =
                                        {|
                                            openBlade = true
                                            resolvedBlade =
                                                {|
                                                    extension = "HubsExtension"
                                                    detailBlade = "ResourceMenuBlade"
                                                    detailBladeInputs =
                                                        {|
                                                            id = appInsightsId.Eval()
                                                            menuid = "segmentationUsers"
                                                        |}
                                                |}
                                        |}
                                    timespan =
                                        {|
                                            relative = {| duration = 3600000 |}
                                        |}
                                    metrics =
                                        [
                                            {|
                                                resourceMetadata = {| id = appInsightsId.Eval() |}
                                                name = "users/count"
                                                metricVisualization = {| color = "#4683de" |}
                                                aggregationType = 5
                                                thresholds = []
                                            |}
                                        ]
                                    itemDataModel =
                                        {|
                                            id = $"defaultAiChartDiv{System.Guid.NewGuid()}"
                                            grouping = null
                                            chartHeight = 1
                                            appliedISOGrain = timeSpan
                                            priorPeriod = false
                                            horizontalBars = true
                                            showOther = false
                                            palette = "multiColor"
                                            jsonDefinitionId = ""
                                            yAxisOptions = {| options = 1 |}
                                            title = "Unique Users"
                                            titleKind = "Auto"
                                            visualization =
                                                {|
                                                    chartType = 1
                                                    legend = null
                                                    axis = null
                                                |}
                                            metrics =
                                                [
                                                    {|
                                                        metricAggregation = 5
                                                        color = "#4683de"
                                                        unit = 1
                                                        id =
                                                            {|
                                                                resourceDefinition = {| id = appInsightsId.Eval() |}
                                                                name =
                                                                    {|
                                                                        id = "users/count"
                                                                        displayName = "Users"
                                                                    |}
                                                                ``namespace`` =
                                                                    {|
                                                                        name = "microsoft.insights/components/kusto"
                                                                    |}
                                                            |}
                                                    |}
                                                ]
                                            filters =
                                                {|
                                                    filterType = 0
                                                    id = System.Guid.NewGuid()
                                                    LogicalOperator = 0
                                                    OperandFilters =
                                                        [
                                                            {|
                                                                filterType = 1
                                                                id = System.Guid.NewGuid()
                                                                ComparisonOperator = 0
                                                                OperandSelectedKey =
                                                                    {| dimensionName = {| id = "type" |} |}
                                                                OperandSelectedValues =
                                                                    [ "pageView"; "customEvent"; "request" ]
                                                            |}
                                                            {|
                                                                filterType = 1
                                                                id = System.Guid.NewGuid()
                                                                ComparisonOperator = 0
                                                                OperandSelectedKey =
                                                                    {|
                                                                        dimensionName = {| id = "operation/synthetic" |}
                                                                    |}
                                                                OperandSelectedValues = [ "False" ]
                                                            |}
                                                        ]
                                                |}
                                        |}
                                |}
                            ]
                    }

                let dashboardId = "Monitor-MyEnvironment"

                let positions: Farmer.Arm.Dashboard.LensPosition list =
                    [ // Little matrix of sizes and positins in the screen
                        {
                            x = 0
                            y = 0
                            colSpan = 10
                            rowSpan = 6
                        } // vm
                        {
                            x = 10
                            y = 0
                            colSpan = 11
                            rowSpan = 6
                        } // db
                        {
                            x = 0
                            y = 6
                            colSpan = 9
                            rowSpan = 6
                        } // response times
                        {
                            x = 9
                            y = 6
                            colSpan = 6
                            rowSpan = 6
                        } // user count
                        {
                            x = 15
                            y = 10
                            colSpan = 2
                            rowSpan = 2
                        } // clock
                    ]

                // Some resources
                let ai = appInsights { name "myInsights" }

                let database =
                    sqlServer {
                        name "server543c8"
                        admin_username "isaac"

                        add_databases
                            [
                                sqlDb {
                                    name "db"
                                    sku Farmer.Sql.DtuSku.S0
                                }
                            ]
                    }

                let vm =
                    vm {
                        name "myVm"
                        username "foo"
                        custom_data "foo"
                    }

                let aiResId = ResourceId.create (Farmer.Arm.Insights.components, ai.Name)
                let vmResId = vm.ResourceId

                let dbResId =
                    Farmer.Arm.Sql.databases.resourceId (database.Name.ResourceName, database.Databases.Head.Name)

                let lenspart = clockPart (System.TimeZoneInfo.Local)

                let dashboard2 =
                    dashboard {
                        name dashboardId
                        title dashboardId
                        depends_on [ vm :> IBuilder; database :> IBuilder; ai :> IBuilder ]
                        add_monitor_chart (positions.[0], virtualMachineCPU vmResId)
                        add_monitor_chart (positions.[1], databaseUtilization dbResId)
                        add_monitor_chart (positions.[2], appInsights_PageResponseTimes aiResId)
                        add_monitor_chart (positions.[3], appInsights_UniqueUsers aiResId)

                        add_custom_lens (
                            {
                                position = positions.[4]
                                metadata = lenspart
                            }
                        )
                    }

                let template = arm { add_resources [ ai; database; vm; dashboard2 ] }
                let jsn = template.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                let title =
                    jobj
                        .SelectToken("resources[?(@.name=='Monitor-MyEnvironment')].tags.hidden-title")
                        .ToString()

                Expect.equal title "Monitor-MyEnvironment" "Incorrect title"

                let lenses =
                    jobj.SelectToken("resources[?(@.name=='Monitor-MyEnvironment')].properties.lenses")

                Expect.isNotNull lenses "Lenses missing"
            }

        ]
