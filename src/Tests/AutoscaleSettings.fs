module AutoscaleSettings

open System
open Expecto
open Farmer
open Farmer.Arm.AutoscaleSettings
open Farmer.Builders
open Farmer.Insights
open Newtonsoft.Json.Linq

let tests =
    testList
        "Autoscale Settings"
        [
            test "Basic Autoscale Settings ARM Resource (no builder)" {
                let myVmss =
                    vmss {
                        name "my-vmss"

                        vm_profile (
                            vm {
                                vm_size Vm.Standard_B1s
                                username "azureuser"
                                operating_system Vm.UbuntuServer_2204LTS
                                os_disk 30 Vm.Premium_LRS
                                no_data_disk
                                custom_script "apt update && apt install -y stress"
                            }
                        )
                    }

                let settings =
                    {
                        Name = ResourceName "test-autoscale-settings"
                        Location = Location.WestEurope
                        Tags = Map.empty
                        Dependencies = Set.empty
                        Properties =
                            {
                                Enabled = true
                                Name = "test-autoscale-settings"
                                Notifications = []
                                PredictiveAutoscalePolicy = None
                                Profiles =
                                    [
                                        {
                                            Name = "DefaultAutoscaleProfile"
                                            Capacity =
                                                {
                                                    Minimum = 1
                                                    Maximum = 10
                                                    Default = 1
                                                }
                                            FixedDate = None
                                            Recurrence = None
                                            Rules =
                                                [
                                                    {
                                                        MetricTrigger =
                                                            {
                                                                MetricName = "Percentage CPU"
                                                                Dimensions = []
                                                                DividePerInstance = None
                                                                MetricNamespace = None
                                                                MetricResourceLocation = None
                                                                MetricResourceUri = myVmss.ResourceId
                                                                Operator = MetricTriggerOperator.GreaterThan
                                                                Statistic = MetricTriggerStatistic.Average
                                                                Threshold = 60
                                                                TimeAggregation = MetricTriggerTimeAggregation.Average
                                                                TimeGrain = TimeSpan.FromMinutes 5
                                                                TimeWindow = TimeSpan.FromMinutes 10
                                                            }
                                                        ScaleAction =
                                                            {
                                                                Cooldown = TimeSpan.FromMinutes 10
                                                                Direction = ScaleActionDirection.Increase
                                                                Type = ScaleActionType.ChangeCount
                                                                Value = 1
                                                            }
                                                    }
                                                    {
                                                        MetricTrigger =
                                                            {
                                                                MetricName = "Percentage CPU"
                                                                Dimensions = []
                                                                DividePerInstance = None
                                                                MetricNamespace = None
                                                                MetricResourceLocation = None
                                                                MetricResourceUri = myVmss.ResourceId
                                                                Operator = MetricTriggerOperator.LessThan
                                                                Statistic = MetricTriggerStatistic.Average
                                                                Threshold = 30
                                                                TimeAggregation = MetricTriggerTimeAggregation.Average
                                                                TimeGrain = TimeSpan.FromMinutes 5
                                                                TimeWindow = TimeSpan.FromMinutes 10
                                                            }
                                                        ScaleAction =
                                                            {
                                                                Cooldown = TimeSpan.FromMinutes 10
                                                                Direction = ScaleActionDirection.Decrease
                                                                Type = ScaleActionType.ChangeCount
                                                                Value = 1
                                                            }
                                                    }
                                                ]
                                        }
                                    ]
                                TargetResourceUri = Managed myVmss.ResourceId
                                TargetResourceLocation = null
                            }
                    }

                let deployment =
                    arm {
                        location Location.WestEurope
                        add_resources [ myVmss; settings ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

                let autoscaleJson =
                    jobj.SelectToken "resources[?(@.name=='test-autoscale-settings')]"

                Expect.isNotNull autoscaleJson "Autoscale Settings are null"
                let dependencies = autoscaleJson["dependsOn"]
                Expect.hasLength dependencies 1 "Should have one dependency for the target resource being scaled."

                Expect.contains
                    (dependencies.ToObject<string array>())
                    "[resourceId('Microsoft.Compute/virtualMachineScaleSets', 'my-vmss')]"
                    "Incorrect dependency"

                let autoscaleProps = autoscaleJson.SelectToken("properties")
                Expect.isNotNull autoscaleProps "Autoscale props is null."
                Expect.isNotNull autoscaleProps["name"] "Autoscale properties name is null"

                Expect.equal
                    (string autoscaleProps["name"])
                    "test-autoscale-settings"
                    "'properties.name' should equal the 'name' of the autoscale setting"

                Expect.isTrue (autoscaleProps[ "enabled" ].ToObject<bool>()) "properties.enabled should be true"

                Expect.equal
                    (autoscaleProps[ "targetResourceUri" ].ToObject<string>())
                    "[resourceId('Microsoft.Compute/virtualMachineScaleSets', 'my-vmss')]"
                    "Incorrect properties.targetResourceUri"

                Expect.hasLength (autoscaleProps.SelectToken "profiles") 1 "Should have one autoscale profile."
                let profile = autoscaleProps.SelectToken "profiles[0]"

                Expect.equal
                    (profile.SelectToken "capacity.default" |> string)
                    "1"
                    "Incorrect profile[0].capacity.default"

                Expect.equal
                    (profile.SelectToken "capacity.maximum" |> string)
                    "10"
                    "Incorrect profile[0].capacity.maximum"

                Expect.equal
                    (profile.SelectToken "capacity.minimum" |> string)
                    "1"
                    "Incorrect profile[0].capacity.minimum"

                Expect.equal
                    (profile.SelectToken "name" |> string)
                    "DefaultAutoscaleProfile"
                    "Incorrect name for autoscale profile"

                Expect.hasLength (profile.SelectToken "rules") 2 "Should have two rules in autoscale profile."
                let rule1 = profile.SelectToken "rules[0]"

                Expect.isFalse
                    ((rule1.SelectToken "metricTrigger.dividePerInstance").ToObject<bool>())
                    "First rule metric trigger should not divide per instance"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.metricName").ToObject<string>())
                    "Percentage CPU"
                    "First rule metric trigger metricName incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.metricResourceUri").ToObject<string>())
                    "[resourceId('Microsoft.Compute/virtualMachineScaleSets', 'my-vmss')]"
                    "First rule metric trigger metricResourceUri incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.operator").ToObject<string>())
                    "GreaterThan"
                    "First rule metric trigger operator incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.statistic").ToObject<string>())
                    "Average"
                    "First rule metric trigger statistic incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.threshold").ToObject<int>())
                    60
                    "First rule metric trigger threshold incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.timeAggregation").ToObject<string>())
                    "Average"
                    "First rule metric trigger timeAggregation incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.timeGrain").ToObject<string>())
                    "PT5M"
                    "First rule metric trigger timeGrain incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.timeWindow").ToObject<string>())
                    "PT10M"
                    "First rule metric trigger timeWindow incorrect"

                Expect.equal
                    ((rule1.SelectToken "scaleAction.cooldown").ToObject<string>())
                    "PT10M"
                    "First rule metric scaleAction cooldown incorrect"

                Expect.equal
                    ((rule1.SelectToken "scaleAction.direction").ToObject<string>())
                    "Increase"
                    "First rule metric scaleAction direction incorrect"

                Expect.equal
                    ((rule1.SelectToken "scaleAction.type").ToObject<string>())
                    "ChangeCount"
                    "First rule metric scaleAction type incorrect"

                Expect.equal
                    ((rule1.SelectToken "scaleAction.value").ToObject<string>())
                    "1"
                    "First rule metric scaleAction value incorrect"
            }
            test "VMSS with autoscale builder" {
                let myVmss =
                    vmss {
                        name "my-vmss"

                        vm_profile (
                            vm {
                                vm_size Vm.Standard_B1s
                                username "azureuser"
                                operating_system Vm.UbuntuServer_2204LTS
                                os_disk 30 Vm.Premium_LRS
                                no_data_disk
                                custom_script "apt update && apt install -y stress"
                            }
                        )

                        autoscale (
                            autoscaleSettings {
                                name "my-vmss-autoscale"

                                properties (
                                    autoscaleSettingsProperties {
                                        profiles
                                            [
                                                autoscaleProfile {
                                                    capacity (autoscaleCapacity { maximum 10 })

                                                    rules
                                                        [
                                                            autoscaleRule { // Scale up with CPU > 60 across the scale set
                                                                metric_trigger (
                                                                    autoscaleMetricTrigger {
                                                                        metric_name "Percentage CPU"
                                                                        divide_per_instance true
                                                                        operator MetricTriggerOperator.GreaterThan
                                                                        statistic MetricTriggerStatistic.Average
                                                                        threshold 60

                                                                        time_aggregation
                                                                            MetricTriggerTimeAggregation.Average

                                                                        time_grain (TimeSpan.FromMinutes 5)
                                                                        time_window (TimeSpan.FromMinutes 10)
                                                                    }
                                                                )

                                                                scale_action (
                                                                    scaleAction {
                                                                        cooldown (TimeSpan.FromMinutes 10)
                                                                        direction ScaleActionDirection.Increase
                                                                        action_type ScaleActionType.ChangeCount
                                                                        value 2
                                                                    }
                                                                )
                                                            }
                                                            autoscaleRule { // Scale down with CPU < 20 across the scale set
                                                                metric_trigger (
                                                                    autoscaleMetricTrigger { // Leaving most defaults
                                                                        metric_name "Percentage CPU"
                                                                        divide_per_instance true
                                                                        operator MetricTriggerOperator.LessThan
                                                                        threshold 20
                                                                    }
                                                                )

                                                                scale_action (
                                                                    scaleAction { // Leaving most defaults
                                                                        direction ScaleActionDirection.Decrease
                                                                    }
                                                                )
                                                            }
                                                        ]
                                                }
                                            ]
                                    }
                                )
                            }
                        )
                    }

                let deployment =
                    arm {
                        location Location.WestEurope
                        add_resources [ myVmss ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
                let autoscaleJson = jobj.SelectToken "resources[?(@.name=='my-vmss-autoscale')]"
                Expect.isNotNull autoscaleJson "Autoscale Settings are null"
                let dependencies = autoscaleJson["dependsOn"]
                Expect.hasLength dependencies 1 "Should have one dependency for the target resource being scaled."

                Expect.contains
                    (dependencies.ToObject<string array>())
                    "[resourceId('Microsoft.Compute/virtualMachineScaleSets', 'my-vmss')]"
                    "Incorrect dependency"

                let autoscaleProps = autoscaleJson.SelectToken("properties")
                Expect.isNotNull autoscaleProps "Autoscale props is null."
                Expect.isNotNull autoscaleProps["name"] "Autoscale properties name is null"

                Expect.equal
                    (string autoscaleProps["name"])
                    "my-vmss-autoscale"
                    "'properties.name' should equal the 'name' of the autoscale setting"

                Expect.isTrue (autoscaleProps[ "enabled" ].ToObject<bool>()) "properties.enabled should be true"

                Expect.equal
                    (autoscaleProps[ "targetResourceUri" ].ToObject<string>())
                    "[resourceId('Microsoft.Compute/virtualMachineScaleSets', 'my-vmss')]"
                    "Incorrect properties.targetResourceUri"

                Expect.hasLength (autoscaleProps.SelectToken "profiles") 1 "Should have one autoscale profile."
                let profile = autoscaleProps.SelectToken "profiles[0]"

                Expect.equal
                    (profile.SelectToken "capacity.default" |> string)
                    "1"
                    "Incorrect profile[0].capacity.default"

                Expect.equal
                    (profile.SelectToken "capacity.maximum" |> string)
                    "10"
                    "Incorrect profile[0].capacity.maximum"

                Expect.equal
                    (profile.SelectToken "capacity.minimum" |> string)
                    "1"
                    "Incorrect profile[0].capacity.minimum"

                Expect.equal
                    (profile.SelectToken "name" |> string)
                    "DefaultAutoscaleProfile"
                    "Incorrect name for autoscale profile"

                Expect.hasLength (profile.SelectToken "rules") 2 "Should have two rules in autoscale profile."
                let rule1 = profile.SelectToken "rules[0]"

                Expect.isTrue
                    ((rule1.SelectToken "metricTrigger.dividePerInstance").ToObject<bool>())
                    "First rule metric trigger should divide per instance"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.metricName").ToObject<string>())
                    "Percentage CPU"
                    "First rule metric trigger metricName incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.metricResourceUri").ToObject<string>())
                    "[resourceId('Microsoft.Compute/virtualMachineScaleSets', 'my-vmss')]"
                    "First rule metric trigger metricResourceUri incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.operator").ToObject<string>())
                    "GreaterThan"
                    "First rule metric trigger operator incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.statistic").ToObject<string>())
                    "Average"
                    "First rule metric trigger statistic incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.threshold").ToObject<int>())
                    60
                    "First rule metric trigger threshold incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.timeAggregation").ToObject<string>())
                    "Average"
                    "First rule metric trigger timeAggregation incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.timeGrain").ToObject<string>())
                    "PT5M"
                    "First rule metric trigger timeGrain incorrect"

                Expect.equal
                    ((rule1.SelectToken "metricTrigger.timeWindow").ToObject<string>())
                    "PT10M"
                    "First rule metric trigger timeWindow incorrect"

                Expect.equal
                    ((rule1.SelectToken "scaleAction.cooldown").ToObject<string>())
                    "PT10M"
                    "First rule metric scaleAction cooldown incorrect"

                Expect.equal
                    ((rule1.SelectToken "scaleAction.direction").ToObject<string>())
                    "Increase"
                    "First rule metric scaleAction direction incorrect"

                Expect.equal
                    ((rule1.SelectToken "scaleAction.type").ToObject<string>())
                    "ChangeCount"
                    "First rule metric scaleAction type incorrect"

                Expect.equal
                    ((rule1.SelectToken "scaleAction.value").ToObject<string>())
                    "2"
                    "First rule metric scaleAction value incorrect"
            }
        ]
