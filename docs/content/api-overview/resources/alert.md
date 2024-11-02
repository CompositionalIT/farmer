---
title: "App Insights - Alerts"
date: 2021-08-20T07:00:00+01:00
weight: 1
chapter: false
---

#### Overview

Azure Application Insights allows you to monitor your application and send you alerts when it is either unavailable, experiencing failures, or suffering from performance issues.

* Application Insights Metric Alerts (`Microsoft.Insights/metricAlerts`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Sets the name of Alert. |
| description | Sets the description of the alert. |
| frequency | How often the metric alert is evaluated |
| window | The period of time that is used to monitor alert activity based on the threshold. |
| severity | Alert severity |
| add_linked_resource | Add the target resource on which the alert is created/updated.  |
| add_linked_resources | Add the target resources on which the alert is created/updated. |
| single_resource_multiple_metric_criteria | The rule criteria that defines the conditions of the alert rule. |
| single_resource_multiple_custom_metric_criteria | The rule criteria that defines the conditions of the alert rule based on Application Insights custom metric or metric with a custom namespace. Metric validation is disabled for such criteria, so it is possible to create an alert that watches metrics not yet emitted. More details are available [here](https://docs.microsoft.com/azure/azure-monitor/platform/alerts-troubleshoot-metric#define-an-alert-rule-on-a-custom-metric-that-isnt-emitted-yet). |
| multiple_resource_multiple_metric_criteria | The rule criterias that defines the conditions of the alert rule. |
| webtest_location_availability_criteria | The rule criteria that defines the conditions of the alert rule. AppInsightsId * WebTestId * FailedLocationCount |
| add_action | Add an action that is performed when the alert rule becomes active. |

More detailed documentation: https://docs.microsoft.com/en-us/azure/templates/microsoft.insights/metricalerts?tabs=json#metricalertproperties

#### Example

Virtual machine alert:

```fsharp
let vm = vm { name "foo"; username "foo" }
let vmAlert = alert { 
    name "myVmAlert2"
    description "Alert if VM CPU goes over 80% for 15 minutes"
    frequency (System.TimeSpan.FromMinutes(5.0) |> IsoDateTime.OfTimeSpan)
    window (System.TimeSpan.FromMinutes(15.0) |> IsoDateTime.OfTimeSpan)
    add_linked_resource vm
    severity AlertSeverity.Warning
    single_resource_multiple_metric_criteria [
            {   MetricNamespace = vm.ResourceId.Type
                MetricName = MetricsName.PercentageCPU
                Threshold = 80
                Comparison = GreaterThan
                Aggregation = Average
            }]
}
```

For the metric names and their aggregations, there is a huge list of options:
https://docs.microsoft.com/en-us/azure/azure-monitor/essentials/metrics-supported

Database alert:

```fsharp
let myAlert = alert { 
        name "myDbAlert"
        description "Alert if DB DTU goes over 85% for 5 minutes"
        frequency (System.TimeSpan.FromMinutes(1.0) |> IsoDateTime.OfTimeSpan)
        window (System.TimeSpan.FromMinutes(5.0) |> IsoDateTime.OfTimeSpan)
        add_linked_resource resId
        severity AlertSeverity.Error
        single_resource_multiple_metric_criteria [
                {   MetricNamespace = resId.ResourceId.Type
                    MetricName = MetricsName.SQL_DB_DTU
                    Threshold = 85
                    Comparison = GreaterThan
                    Aggregation = Average
                }]
    } 
```

Website down alert:

```fsharp
let ai = appInsights { name "ai" }
let webtest = availabilityTest { ... } 
let aiId, webId = (ai :> IBuilder).ResourceId |> Managed, 
                    (webtest :> IBuilder).ResourceId |> Managed
let webAlert = alert { 
    name "myWebAlert"
    description "Alert if website is failing 5 mins on 3 locations"
    frequency (System.TimeSpan.FromMinutes(1.0) |> IsoDateTime.OfTimeSpan)
    window (System.TimeSpan.FromMinutes(5.0) |> IsoDateTime.OfTimeSpan)
    add_linked_resources [aiId; webId]
    severity AlertSeverity.Warning
    webtest_location_availability_criteria (aiId.ResourceId, webId.ResourceId, 3)
}
```

Custom metric alert based on Azure Application Insights:

```fsharp
let ai = appInsights { name "ai" }

let customMetricAlert = alert {
    name "customMetricAlert"
    description "My Custom Metric Alert Description"
    frequency (System.TimeSpan.FromMinutes(1.0) |> IsoDateTime.OfTimeSpan)
    window (System.TimeSpan.FromMinutes(5.0) |> IsoDateTime.OfTimeSpan)
    add_linked_resource ai
    severity AlertSeverity.Error
    single_resource_multiple_custom_metric_criteria [
        {
            MetricNamespace = None
            MetricName = Insights.MetricsName "MyCustomMetric"
            Threshold = 0
            Comparison = GreaterThan
            Aggregation = Maximum
        }
    ]
}
```

Custom metric alert with custom metric namespace:

```fsharp
let ai = appInsights { name "ai" }

let customMetricAlert = alert {
    name "customMetricAlert"
    description "My Custom Metric Alert Description"
    frequency (System.TimeSpan.FromMinutes(1.0) |> IsoDateTime.OfTimeSpan)
    window (System.TimeSpan.FromMinutes(5.0) |> IsoDateTime.OfTimeSpan)
    add_linked_resource ai
    severity AlertSeverity.Error
    single_resource_multiple_custom_metric_criteria [
        {
            MetricNamespace = Some (ResourceType("MyCustomNamespace", ""))
            MetricName = Insights.MetricsName "MyCustomMetric"
            Threshold = 0
            Comparison = GreaterThan
            Aggregation = Maximum
        }
    ]
}
```
