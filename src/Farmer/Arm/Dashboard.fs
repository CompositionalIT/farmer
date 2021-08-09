[<AutoOpen>]
module Farmer.Arm.Dashboard

open Farmer

let dashboard = ResourceType("Microsoft.Portal/dashboards", "2015-08-01-preview")

type DashboardMetadata =
| EmptyMetadata
| CustomMetadata of obj
| Cache24h 

type LensMetadata = {
    ``type`` : string
    inputs : obj list
    settings : obj
    filters : obj option
    asset : {| idInputName : string;  ``type`` : string |} option
    isAdapter : bool option
    defaultMenuItemId : string option
}

type LensPart = {
    position : {| x : int; y : int; rowSpan : int; colSpan : int; |}
    metadata : LensMetadata
}

type ChartDurationInterval =
| ISO8601DurationFormat of string
    static member OneHour = ISO8601DurationFormat "PT1H"
    static member FiveMinutes = ISO8601DurationFormat "PT5M"
    static member OneMinute = ISO8601DurationFormat "PT1M"

type ChartResources = 
| ChartResouce of string
    static member PercentageCPU = ChartResouce "Percentage CPU"
    static member DiskReadOperationsPerSec = ChartResouce "Disk Read Operations/Sec"
    static member DiskWriteOperationsPerSec = ChartResouce "Disk Write Operations/Sec"
    static member DiskReadBytes = ChartResouce "Disk Read Bytes"
    static member DiskWriteBytes = ChartResouce "Disk Write Bytes"
    static member NetworkIn = ChartResouce "Network In"
    static member NetworkOut = ChartResouce "Network Out"

/// Generates a MarkdownPart
let generateMarkdownPart (markdownProperties:{|title:string; subtitle:string; content:string|}) = {
    ``type`` = "Extension[azure]/HubsExtension/PartType/MarkdownPart"
    inputs = List.empty
    settings = {| content = markdownProperties.content; title = markdownProperties.title; subtitle = markdownProperties.subtitle |} :> obj
    filters = None
    asset = None 
    isAdapter = None
    defaultMenuItemId = None
}

/// Generates a VideoPart
let generateVideoPart (videoProperties:{|title:string; subtitle:string; url:string|}) = {
    ``type`` = "Extension[azure]/HubsExtension/PartType/VideoPart"
    inputs = List.empty
    settings = {| content = {| settings = {| title = videoProperties.title; subtitle = videoProperties.subtitle; src = videoProperties.url; autoplay= false |} |} |} :> obj
    filters = None
    asset = None 
    isAdapter = None
    defaultMenuItemId = None
}

/// Generates a virtualMachinePart
let generateVirtualMachinePart (vmId:ResourceId) = {
    ``type`` = "Extension/Microsoft_Azure_Compute/PartType/VirtualMachinePart"
    inputs = [ {| name = "id"; value = vmId.ArmExpression.Eval() |} :> obj ]
    settings = None
    filters = None
    asset = Some {| idInputName = "id"; ``type`` = "VirtualMachine" |}
    isAdapter = None
    defaultMenuItemId = Some "overview"
}

/// Generates a virtualMachinePart
let generateWebtestResultPart (applicationInsightsName:string) = {
    ``type`` = "Extension/AppInsightsExtension/PartType/AllWebTestsResponseTimeFullGalleryAdapterPart"
    inputs = [ {| name = "ComponentId"; value = {| Name = applicationInsightsName; SubscriptionId = "[ subscription().subscriptionId ]"; ResourceGroup = "[ resourceGroup().id ]" |} |} ]
    settings = None
    filters = None
    asset = Some {| idInputName = "ComponentId"; ``type`` = "ApplicationInsights" |}
    isAdapter = Some true
    defaultMenuItemId = None
}

/// Generates a MetricsChartPart for a resource given in parameters
let generateMetricsChartPart (chartProperties:{| resourceId:ResourceId; metrics: ChartResources list; interval : ChartDurationInterval |}) = {
    ``type`` = "Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart"
    inputs = [ {| name = "queryInputs"
                  value = {| id = chartProperties.resourceId.ArmExpression.Eval(); chartType = 0;
                             timespan = {| duration = match chartProperties.interval with | ISO8601DurationFormat dur -> dur
                                           start = null; ``end`` = null |}
                             metrics = chartProperties.metrics |> List.map(function 
                                            ChartResouce m -> {| name = m; resourceId = chartProperties.resourceId.ArmExpression.Eval() |})
                          |} |}  ]
    settings = None
    filters = None
    asset = None
    isAdapter = None
    defaultMenuItemId = None
}

/// Generates a MonitorChartPart
let generateMonitorChartPart (chartProperties : {| chartInputs:obj list; chartSettings: obj option; filters : obj option |}) = {
    ``type`` = "Extension/HubsExtension/PartType/MonitorChartPart"
    inputs = [ box <| {| name = "sharedTimeRange"; isOptional = true |};
               box <| {| name = "options"
                         value = {| v2charts = true
                                    charts = [ chartProperties.chartInputs ] |} |} ]
    settings = Some ({| content = {| options = {| chart = chartProperties.chartSettings |} |} |})
    filters = chartProperties.filters
    asset = None
    isAdapter = None
    defaultMenuItemId = None
}

type Dashboard =
    { Name : ResourceName
      Title : string option
      Location : Location
      Metadata : DashboardMetadata
      LensParts : LensPart list
      Dependencies : Set<ResourceId> }
    interface IArmResource with
        member this.ResourceId = dashboard.resourceId this.Name
        member this.JsonModel =

            let lensesPartsObjJson =
                // Parts-list is not array, instead it's "parts": { "0": { }, "1": { }, ... }
                let lensesJsons = this.LensParts |> List.mapi(fun idx item -> $"\"{idx}\": {item |> Farmer.Serialization.toJson }")
                ("{" + System.String.Join(",", lensesJsons) + "}") |> Farmer.Serialization.ofJson

            let dahsboardTitle = 
                match this.Title with
                | Some title -> title
                | None -> this.Name.Value

            {| dashboard.Create(this.Name, this.Location, dependsOn = this.Dependencies) with
                   tags = {| ``hidden-title`` = dahsboardTitle |}
                   id = ArmExpression.create(
                           $"concat('/subscriptions/', subscription().subscriptionId, '/resourceGroups/', resourceGroup().name, '/providers/Microsoft.Portal/dashboards/', '{this.Name.Value}')"
                       ).Eval()
                   properties =
                       {| metadata =
                            match this.Metadata with
                            | EmptyMetadata -> {||} :> obj
                            | CustomMetadata metad -> metad
                            | Cache24h -> 
                                {| model =
                                    {| timeRange =
                                         {| ``type`` = "MsPortalFx.Composition.Configuration.ValueTypes.TimeRange"; value = {| relative = {| duration = 24; timeUnit = 1 |} |} |}
                                       filterLocale = {| value = "en-us" |}
                                       filters =
                                          {| value = {| MsPortalFx_TimeRange = {| model = {| format = "utc"; granularity = "auto"; relative = "24h" |}
                                                                                  displayCache = {| name = "UTC Time"; value = "Past 24 hours" |}
                                           |} |} |}
                                    |}
                                |} :> _
                          lenses = {| ``0`` = {| order = "0"; parts = lensesPartsObjJson |} |}
                       |}
                |} :> _
