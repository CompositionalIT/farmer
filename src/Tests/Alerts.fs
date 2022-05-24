module Alerts

open Expecto
open Farmer
open Farmer.Insights
open Farmer.Builders
open Farmer.Arm.InsightsAlerts

let tests =
    testList
        "Alerts"
        [ test "Create a VM alert" {
              let vm =
                  vm {
                      name "foo"
                      username "foo"
                  }

              let vmAlert =
                  alert {
                      name "myVmAlert2"
                      description "Alert if VM CPU goes over 80% for 15 minutes"

                      frequency (
                          System.TimeSpan.FromMinutes(5.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      window (
                          System.TimeSpan.FromMinutes(15.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      add_linked_resource vm
                      severity AlertSeverity.Warning

                      single_resource_multiple_metric_criteria
                          [ { MetricNamespace = vm.ResourceId.Type
                              MetricName = MetricsName.PercentageCPU
                              Threshold = 80
                              Comparison = GreaterThan
                              Aggregation = Average } ]
                  }

              let template = arm { add_resources [ vm; vmAlert ] }
              let jsn = template.Template |> Writer.toJson
              let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

              let isenabled =
                  jobj
                      .SelectToken("resources[?(@.name=='myVmAlert2')].properties.enabled")
                      .ToString()
                      .ToLower()

              Expect.equal isenabled "true" "Webtest not enabled"
          }

          test "Create a SQL Database heavy usage alert" {
              let sql =
                  sqlServer {
                      name "my37server"
                      admin_username "isaac"

                      add_databases
                          [ sqlDb {
                                name "mydb23"
                                sku Farmer.Sql.DtuSku.S0
                            } ]
                  }

              let db = sql.Databases.Head

              let resId =
                  Farmer.Arm.Sql.databases.resourceId (sql.Name.ResourceName, db.Name)
                  |> Managed

              let myAlert =
                  alert {
                      name "myDbAlert"
                      description "Alert if DB DTU goes over 80% for 5 minutes"

                      frequency (
                          System.TimeSpan.FromMinutes(5.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      window (
                          System.TimeSpan.FromMinutes(5.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      add_linked_resource resId
                      severity AlertSeverity.Error

                      single_resource_multiple_metric_criteria
                          [ { MetricNamespace = resId.ResourceId.Type
                              MetricName = MetricsName.SQL_DB_DTU
                              Threshold = 80
                              Comparison = GreaterThan
                              Aggregation = Average } ]
                  }

              let template =
                  arm {
                      add_resource sql
                      add_resource myAlert
                  }

              let jsn = template.Template |> Writer.toJson
              let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

              let allOf =
                  jobj.SelectToken("resources[?(@.name=='myDbAlert')].properties.criteria.allOf[0]")

              Expect.isNotNull allOf "allOf not found"

              let targ =
                  jobj
                      .SelectToken("resources[?(@.name=='myDbAlert')].properties.targetResourceType")
                      .ToString()

              Expect.equal targ Farmer.Arm.Sql.databases.Type "Wrong target resource type"
          }

          test "Create a webtest alert when website down" {
              let ai = appInsights { name "ai" }

              let webtest =
                  availabilityTest {
                      name "webTest"
                      link_to_app_insights ai

                      locations
                          [ AvailabilityTest.TestSiteLocation.CentralUS
                            AvailabilityTest.TestSiteLocation.NorthEurope ]

                      web_test (
                          "https://google.com"
                          |> System.Uri
                          |> AvailabilityTest.WebsiteUrl
                      )
                  }

              let aiId, webId =
                  (ai :> IBuilder).ResourceId |> Managed, (webtest :> IBuilder).ResourceId |> Managed

              let webAlert =
                  alert {
                      name "myWebAlert"
                      description "Alert if Google is failing 5 mins on both 2 locations"

                      frequency (
                          System.TimeSpan.FromMinutes(1.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      window (
                          System.TimeSpan.FromMinutes(5.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      add_linked_resources [ aiId; webId ]
                      severity AlertSeverity.Warning
                      webtest_location_availability_criteria (aiId.ResourceId, webId.ResourceId, 2)
                  }

              let template = arm { add_resources [ ai; webtest; webAlert ] }
              let jsn = template.Template |> Writer.toJson
              let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

              let freq =
                  jobj
                      .SelectToken("resources[?(@.name=='myWebAlert')].properties.evaluationFrequency")
                      .ToString()

              Expect.equal freq "PT1M" "Wrong frequency"
          }

          test "Create a custom metric alert based on Azure ApplicationInsights" {
              let alertName = "myCustomAlert"
              let ai = appInsights { name "ai" }

              let customAlert =
                  alert {
                      name alertName
                      description "Alert based on MyCustomMetric"

                      frequency (
                          System.TimeSpan.FromMinutes(1.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      window (
                          System.TimeSpan.FromMinutes(5.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      add_linked_resource ai
                      severity AlertSeverity.Warning

                      single_resource_multiple_custom_metric_criteria
                          [ { MetricNamespace = None
                              MetricName = MetricsName "MyCustomMetric"
                              Threshold = 20
                              Comparison = GreaterThan
                              Aggregation = Average } ]
                  }

              let template = arm { add_resources [ ai; customAlert ] }
              let jsn = template.Template |> Writer.toJson
              let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

              let allOf =
                  jobj.SelectToken($"resources[?(@.name=='{alertName}')].properties.criteria.allOf[0]")

              Expect.isNotNull allOf "allOf not found"
              Expect.equal (allOf.Item("metricName").ToString()) "MyCustomMetric" "Wrong target metric namespace"

              Expect.equal
                  (allOf.Item("metricNamespace").ToString())
                  "Azure.ApplicationInsights"
                  "Wrong target metric namespace"

              Expect.equal
                  (allOf
                      .Item("skipMetricValidation")
                      .ToObject<bool>())
                  true
                  "Wrong value of skipMetricValidation"

              let targ =
                  jobj
                      .SelectToken($"resources[?(@.name=='{alertName}')].properties.targetResourceType")
                      .ToString()

              Expect.equal targ Farmer.Arm.Insights.components.Type "Wrong target resource type"
          }

          test "Create a custom metric alert based on custom namespace" {
              let alertName = "myCustomAlert"

              let customAlert =
                  alert {
                      name alertName
                      description "Alert based on MyCustomMetric"

                      frequency (
                          System.TimeSpan.FromMinutes(1.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      window (
                          System.TimeSpan.FromMinutes(5.0)
                          |> IsoDateTime.OfTimeSpan
                      )

                      severity AlertSeverity.Warning

                      single_resource_multiple_custom_metric_criteria
                          [ { MetricNamespace = Some(ResourceType("MyCustomNamespace", ""))
                              MetricName = MetricsName "MyCustomMetric"
                              Threshold = 20
                              Comparison = GreaterThan
                              Aggregation = Average } ]
                  }

              let template = arm { add_resources [ customAlert ] }
              let jsn = template.Template |> Writer.toJson
              let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

              let allOf =
                  jobj.SelectToken($"resources[?(@.name=='{alertName}')].properties.criteria.allOf[0]")

              Expect.isNotNull allOf "allOf not found"
              Expect.equal (allOf.Item("metricName").ToString()) "MyCustomMetric" "Wrong target metric namespace"

              Expect.equal
                  (allOf.Item("metricNamespace").ToString())
                  "MyCustomNamespace"
                  "Wrong target custom metric namespace"
          } ]
