[<AutoOpen>]
module Farmer.Arm.Dashboard

open Farmer
open Farmer.Insights

let dashboard = ResourceType("Microsoft.Portal/dashboards", "2020-09-01-preview")

type DashboardMetadata =
    | EmptyMetadata
    | CustomMetadata of obj
    | Cache24h

type LensAsset = {
    idInputName: string
    ``type``: string
}

type LensMetadata = {
    ``type``: string
    inputs: obj list
    settings: obj
    filters: obj
    asset: LensAsset
    isAdapter: System.Nullable<bool>
    defaultMenuItemId: string
}

type LensPosition = {
    x: int
    y: int
    rowSpan: int
    colSpan: int
}

type LensPart = {
    position: LensPosition
    metadata: LensMetadata
}

type MarkdownPartParameters = {
    title: string
    subtitle: string
    content: string
}

/// Generates a MarkdownPart
let generateMarkdownPart (markdownProperties: MarkdownPartParameters) = {
    ``type`` = "Extension[azure]/HubsExtension/PartType/MarkdownPart"
    inputs = List.empty
    settings =
        {|
            content = markdownProperties.content
            title = markdownProperties.title
            subtitle = markdownProperties.subtitle
        |}
        :> obj
    filters = null
    asset = Unchecked.defaultof<LensAsset>
    isAdapter = System.Nullable()
    defaultMenuItemId = null
}

type VideoPartParameters = {
    title: string
    subtitle: string
    url: string
}

/// Generates a VideoPart
let generateVideoPart (videoProperties: VideoPartParameters) = {
    ``type`` = "Extension[azure]/HubsExtension/PartType/VideoPart"
    inputs = List.empty
    settings =
        {|
            content = {|
                settings = {|
                    title = videoProperties.title
                    subtitle = videoProperties.subtitle
                    src = videoProperties.url
                    autoplay = false
                |}
            |}
        |}
        :> obj
    filters = null
    asset = Unchecked.defaultof<LensAsset>
    isAdapter = System.Nullable()
    defaultMenuItemId = null
}

/// Generates a virtualMachinePart
let generateVirtualMachinePart (vmId: ResourceId) = {
    ``type`` = "Extension/Microsoft_Azure_Compute/PartType/VirtualMachinePart"
    inputs = [
        {|
            name = "id"
            value = vmId.ArmExpression.Eval()
        |}
        :> obj
    ]
    settings = null
    filters = null
    asset = {
        idInputName = "id"
        ``type`` = "VirtualMachine"
    }
    isAdapter = System.Nullable()
    defaultMenuItemId = "overview"
}

/// Generates a webtest part
let generateWebtestResultPart (applicationInsightsName: string) = {
    ``type`` = "Extension/AppInsightsExtension/PartType/AllWebTestsResponseTimeFullGalleryAdapterPart"
    inputs = [
        {|
            name = "ComponentId"
            value = {|
                Name = applicationInsightsName
                SubscriptionId = "[ subscription().subscriptionId ]"
                ResourceGroup = "[ resourceGroup().name ]"
            |}
        |}
    ]
    settings = null
    filters = null
    asset = {
        idInputName = "ComponentId"
        ``type`` = "ApplicationInsights"
    }
    isAdapter = System.Nullable(true)
    defaultMenuItemId = null
}

type MetrixChartParameters = {
    resourceId: ResourceId
    metrics: MetricsName list
    interval: IsoDateTime
}

/// Generates a MetricsChartPart for a resource given in parameters
let generateMetricsChartPart (chartProperties: MetrixChartParameters) = {
    ``type`` = "Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart"
    inputs = [
        {|
            name = "queryInputs"
            value = {|
                id = chartProperties.resourceId.ArmExpression.Eval()
                chartType = 0
                timespan = {|
                    duration =
                        match chartProperties.interval with
                        | IsoDateTime dur -> dur
                    start = null
                    ``end`` = null
                |}
                metrics =
                    chartProperties.metrics
                    |> List.map (function
                        | MetricsName m -> {|
                            name = m
                            resourceId = chartProperties.resourceId.ArmExpression.Eval()
                          |})
            |}
        |}
    ]
    settings = null
    filters = null
    asset = Unchecked.defaultof<LensAsset>
    isAdapter = System.Nullable()
    defaultMenuItemId = null
}

type MonitorChartParameters = {
    chartInputs: obj list
    chartSettings: obj
    filters: obj
}

/// Generates a MonitorChartPart
let generateMonitorChartPart (chartProperties: MonitorChartParameters) = {
    ``type`` = "Extension/HubsExtension/PartType/MonitorChartPart"
    inputs = [
        box
        <| {|
            name = "sharedTimeRange"
            isOptional = true
        |}
        box
        <| {|
            name = "options"
            value = {|
                v2charts = true
                charts = chartProperties.chartInputs
            |}
        |}
    ]
    settings = {|
        content = {|
            options = {|
                chart = chartProperties.chartSettings
            |}
        |}
    |}
    filters = chartProperties.filters
    asset = Unchecked.defaultof<LensAsset>
    isAdapter = System.Nullable()
    defaultMenuItemId = null
}

type Dashboard =
    {
        Name: ResourceName
        Title: string option
        Location: Location
        Metadata: DashboardMetadata
        LensParts: LensPart list
        Dependencies: Set<ResourceId>
    }

    interface IArmResource with
        member this.ResourceId = dashboard.resourceId this.Name

        member this.JsonModel =

            let dahsboardTitle =
                match this.Title with
                | Some title -> title
                | None -> this.Name.Value

            {| dashboard.Create(this.Name, this.Location, dependsOn = this.Dependencies) with
                tags = {| ``hidden-title`` = dahsboardTitle |}
                id =
                    ArmExpression
                        .create(
                            $"concat('/subscriptions/', subscription().subscriptionId, '/resourceGroups/', resourceGroup().name, '/providers/Microsoft.Portal/dashboards/', '{this.Name.Value}')"
                        )
                        .Eval()
                properties = {|
                    metadata =
                        match this.Metadata with
                        | EmptyMetadata -> {| |} :> obj
                        | CustomMetadata metad -> metad
                        | Cache24h -> {|
                            model = {|
                                timeRange = {|
                                    ``type`` = "MsPortalFx.Composition.Configuration.ValueTypes.TimeRange"
                                    value = {|
                                        relative = {| duration = 24; timeUnit = 1 |}
                                    |}
                                |}
                                filterLocale = {| value = "en-us" |}
                                filters = {|
                                    value = {|
                                        MsPortalFx_TimeRange = {|
                                            model = {|
                                                format = "utc"
                                                granularity = "auto"
                                                relative = "24h"
                                            |}
                                            displayCache = {|
                                                name = "UTC Time"
                                                value = "Past 24 hours"
                                            |}
                                        |}
                                    |}
                                |}
                            |}
                          |}
                    lenses = [
                        {|
                            order = "0"
                            parts = this.LensParts
                        |}
                    ]
                |}
            |}
