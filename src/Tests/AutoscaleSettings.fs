module AutoscaleSettings

open Expecto
open Farmer
open Farmer.Arm.AutoscaleSettings
open Farmer.Builders
open Newtonsoft.Json.Linq

let tests =
    testList "Autoscale Settings" [
        test "Basic Autoscale Settings ARM Resource (no builder)" {
            let settings =
                {
                    Name = ResourceName "test-autoscale-settings"
                    Location = Location.WestEurope
                    Tags = Map.empty
                    Properties = {
                        Enabled = true
                        Name = "test-autoscale-settings"
                        Notifications = []
                        PredictiveAutoscalePolicy = None
                        Profiles = [
                            {
                                Name = "DefaultAutoscaleProfile"
                                Capacity = {
                                    Minimum = "1"
                                    Maximum = "10"
                                    Default = "1" 
                                }
                                FixedDate = None
                                Recurrence = None
                                Rules = [
                                    {
                                        MetricTrigger = {
                                            MetricName = "Percentage CPU"
                                            Dimensions = []
                                            DividePerInstance = false
                                            MetricNamespace = null
                                            MetricResourceLocation = null
                                            MetricResourceUri = "[resourceId('Microsoft.Compute/virtualMachineScaleSets', 'my-vmss')]"
                                            Operator = "GreaterThan"
                                            Statistic = "Average"
                                            Threshold = 60
                                            TimeAggregation = "Average"
                                            TimeGrain = "PT5M"
                                            TimeWindow = "PT10M"
                                        }
                                        ScaleAction = {
                                            Cooldown = "PT10M"
                                            Direction = "Increase"
                                            Type = "ChangeCount"
                                            Value = "1" 
                                        } 
                                    }
                                    {
                                        MetricTrigger = {
                                            MetricName = "Percentage CPU"
                                            Dimensions = []
                                            DividePerInstance = false
                                            MetricNamespace = null
                                            MetricResourceLocation = null
                                            MetricResourceUri = "[resourceId('Microsoft.Compute/virtualMachineScaleSets', 'my-vmss')]"
                                            Operator = "LessThan"
                                            Statistic = "Average"
                                            Threshold = 30
                                            TimeAggregation = "Average"
                                            TimeGrain = "PT5M"
                                            TimeWindow = "PT10M"
                                        }
                                        ScaleAction = {
                                            Cooldown = "PT10M"
                                            Direction = "Decrease"
                                            Type = "ChangeCount"
                                            Value = "1" 
                                        } 
                                    }                                ]
                            }
                        ]
                        TargetResourceUri = "[resourceId('Microsoft.Compute/virtualMachineScaleSets', 'my-vmss')]"
                        TargetResourceLocation = null
                    } 
                }
            let deployment = arm {
                location Location.WestEurope
                add_resources [
                    vmss {
                        name "my-vmss"
                        add_availability_zones [ "1"; "2" ]
                        vm_profile (
                            vm {
                                vm_size Vm.Standard_B1ms
                                username "azureuser"
                                operating_system Vm.UbuntuServer_2204LTS
                                os_disk 30 Vm.Premium_LRS
                                no_data_disk
                            }
                        )
                    }
                ]
                add_resource settings
            }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let autoscaleJson = jobj.SelectToken "resources[?(@.name=='test-autoscale-settings')]"
            Expect.isNotNull autoscaleJson "Autoscale Settings are null"
            let autoscaleProps = autoscaleJson.SelectToken("properties")
            Expect.isNotNull
                autoscaleProps
                "Autoscale props is null."
            Expect.isNotNull
                autoscaleProps["name"]
                "Autoscale properties name is null"
        }
    ]
