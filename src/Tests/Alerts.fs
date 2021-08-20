module Alerts

open Expecto
open Farmer
open Farmer.Insights
open Farmer.Builders
open Farmer.Arm.InsightsAlerts

let tests = testList "Alerts" [
    test "Create a VM alert" {
        let vm = vm { name "foo"; username "foo" }
        let vmAlert = alert { 
            name "myVmAlert2"
            description "Alert if VM CPU goes over 80% for 15 minutes"
            frequency (DurationInterval.FiveMinutes)
            window (DurationInterval.FifteenMinutes)
            add_linked_resource vm
            severity AlertSeverity.Alert_Warning
            criteria 
                (SingleResourceMultipleMetricCriteria [
                    {   MetricNamespace = vm.ResourceId.Type
                        MetricName = MetricsName.PercentageCPU
                        Threshold = 80
                        Comparison = GreaterThan
                        Aggregation = Average
                    }])
        }

        let template = arm { add_resources [ vm; vmAlert ] }
        let jsn = template.Template |> Writer.toJson 
        let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse
        let isenabled = jobj.SelectToken("resources[?(@.name=='myVmAlert2')].properties.enabled").ToString().ToLower()
        Expect.equal isenabled "true" "Webtest not enabled"
    }

    test "Create a SQL Database heavy usage alert" {
        let sql = 
            sqlServer {
                name "my37server"; admin_username "isaac"
                add_databases [ sqlDb { name "mydb23"; sku Farmer.Sql.DtuSku.S0 } ]
            }
        let db = sql.Databases.Head
        let resId = Farmer.Arm.Sql.databases.resourceId (sql.Name.ResourceName, db.Name) |> Managed
        let myAlert = alert { 
                name "myDbAlert"
                description "Alert if DB DTU goes over 80% for 5 minutes"
                frequency (DurationInterval.FiveMinutes)
                window (DurationInterval.FiveMinutes)
                add_linked_resource resId
                severity AlertSeverity.Alert_Error
                criteria 
                    (SingleResourceMultipleMetricCriteria [
                        {   MetricNamespace = resId.ResourceId.Type
                            MetricName = MetricsName.SQL_DB_DTU
                            Threshold = 80
                            Comparison = GreaterThan
                            Aggregation = Average
                        }])
            } 

        let template = arm { add_resource sql; add_resource myAlert }
        let jsn = template.Template |> Writer.toJson 
        let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse
        let allOf = jobj.SelectToken("resources[?(@.name=='myDbAlert')].properties.criteria.allOf[0]")
        Expect.isNotNull allOf "allOf not found"
        let targ = jobj.SelectToken("resources[?(@.name=='myDbAlert')].properties.targetResourceType").ToString()
        Expect.equal targ Farmer.Arm.Sql.databases.Type "Wrong target resource type"
    }

    test "Create a webtest alert when website down" {
        let ai = appInsights { name "ai" }
        let webtest =
            availabilityTest {
                name "webTest"
                link_to_app_insights ai
                locations [ AvailabilityTest.TestSiteLocation.CentralUS;
                            AvailabilityTest.TestSiteLocation.NorthEurope ]
                web_test ("https://google.com" |> System.Uri |> AvailabilityTest.WebsiteUrl)
            } 
        let aiId, webId = (ai :> IBuilder).ResourceId |> Managed, 
                          (webtest :> IBuilder).ResourceId |> Managed
        let webAlert = alert { 
            name "myWebAlert"
            description "Alert if Google is failing 5 mins on both 2 locations"
            frequency (DurationInterval.OneMinute)
            window (DurationInterval.FiveMinutes)
            add_linked_resources [aiId; webId]
            severity AlertSeverity.Alert_Warning
            criteria (WebtestLocationAvailabilityCriteria(aiId.ResourceId, webId.ResourceId, 2))
        }

        let template = arm { add_resources [ ai; webtest; webAlert ] }
        let jsn = template.Template |> Writer.toJson 
        let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse
        let freq = jobj.SelectToken("resources[?(@.name=='myWebAlert')].properties.evaluationFrequency").ToString()
        Expect.equal freq "PT1M" "Wrong frequency"
    }

]