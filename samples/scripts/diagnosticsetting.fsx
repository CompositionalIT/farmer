#r "./libs/Newtonsoft.Json.dll"
#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.DiagnosticSettings

let data = storageAccount { name "isaacsuperdata" }
let hub = eventHub { name "isaacsuperhub" }
let logs = logAnalytics { name "isaacsuperlogs" }

let web = webApp {
    name "isaacdiagsuperweb"
    app_insights_off
}

let mydiagnosticSetting = diagnosticSettings {
    name "myDiagnosticSetting"
    metrics_source web

    add_destination data
    add_destination logs
    add_destination hub
    loganalytics_output_type Dedicated
    capture_metrics [ "AllMetrics" ]

    capture_logs [
        Logging.Web.Sites.AppServicePlatformLogs
        Logging.Web.Sites.AppServiceAntivirusScanAuditLogs
        Logging.Web.Sites.AppServiceAppLogs
        Logging.Web.Sites.AppServiceHTTPLogs
    ]

    add_tag "sample" "isaac"
}

let deployment = arm { add_resources [ data; web; hub; logs; mydiagnosticSetting ] }

deployment |> Writer.quickWrite "diagnostics"

deployment |> Deploy.execute "isaacdiagtest" []