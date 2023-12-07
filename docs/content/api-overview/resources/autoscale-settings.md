---
title: "Autoscale Settings"
date: 2023-12-07T16:46:00-04:00
chapter: false
weight: 23
---

#### Overview
The Autoscale Setting builder is used to create Autoscale settings to control how the Azure Monitor Autoscale can increase and decrease the capacity of some resources, such as Virtual Machine Scale Sets.

* Autoscale Notification Email Builder (`autoscaleNotificationEmail`)
* Autoscale Webhook Builder (`autoscaleWebhook`)
* Autoscale Notification Builder (`autoscaleNotification`)
* Autoscale Capacity Builder (`autoscaleCapacity`)
* Autoscale Schedule Builder (`autoscaleSchedule`)
* Autoscale Dimension Builder (`autoscaleDimension`)
* Autoscale Scale Action Builder (`scaleAction`)
* Autoscale Recurrence Builder (`recurrence`)
* Autoscale Predictive Autoscale Policy Builder (`predictiveAutoscalePolicy`)
* Autoscale Fixed Date Builder (`fixedDate`)
* Autoscale Metric Trigger Builder (`autoscaleMetricTrigger`)
* Autoscale Rule Builder (`autoscaleRule`)
* Autoscale Profile Builder (`autoscaleProfile`)
* Autoscale Settings Properties Builder (`autoscaleSettingsProperties`)
* Autoscale Settings Builder (`autoscaleSettings`) - builds `Microsoft.Insights/autoscalesettings`


### NotificationEmailBuilder Keywords

The `NotificationEmailBuilder` constructs Email Notifications.

| Keyword | Purpose |
|---------|---------|
| custom_emails | Sets custom emails for notifications. |
| send_to_subscription_administrator | Sends notifications to the subscription administrator. |
| send_to_subscription_co_administrators | Sends notifications to the subscription co-administrators. |

---

### WebhookBuilder Keywords

The `WebhookBuilder` constructs Webhooks.

| Keyword | Purpose |
|---------|---------|
| properties | Sets properties for the webhook. |
| service_uri | Sets the service URI for the webhook. |

---

### NotificationBuilder Keywords

The `NotificationBuilder` constructs Notifications.

| Keyword | Purpose |
|---------|---------|
| email | Sets email notifications. |
| webhooks | Sets webhook notifications. |

---

### CapacityBuilder Keywords

The `CapacityBuilder` constructs Capacity configurations.

| Keyword | Purpose |
|---------|---------|
| default | Sets the default capacity value. |
| maximum | Sets the maximum capacity value. |
| minimum | Sets the minimum capacity value. |

---

### ScheduleBuilder Keywords

The `ScheduleBuilder` constructs Schedules.

| Keyword | Purpose |
|---------|---------|
| days | Sets the days for the schedule. |
| hours | Sets the hours for the schedule. |
| minutes | Sets the minutes for the schedule. |
| timeZone | Sets the time zone for the schedule. |

---

### DimensionBuilder Keywords

The `DimensionBuilder` constructs Dimensions.

| Keyword | Purpose |
|---------|---------|
| dimensionName | Sets the name of the dimension. |
| operator | Sets the operator for the dimension. |
| values | Sets values for the dimension. |

---

### ScaleActionBuilder Keywords

The `ScaleActionBuilder` constructs Scale Actions.

| Keyword | Purpose |
|---------|---------|
| cooldown | Sets the cooldown for the scale action. |
| direction | Sets the direction for the scale action. |
| action_type | Sets the type of action for the scale action. |
| value | Sets the value for the scale action. |

---

### RecurrenceBuilder Keywords

The `RecurrenceBuilder` constructs Recurrence configurations.

| Keyword | Purpose |
|---------|---------|
| frequency | Sets the frequency for the recurrence. |
| schedule | Sets the schedule for the recurrence. |

---

### PredictiveAutoscalePolicyBuilder Keywords

The `PredictiveAutoscalePolicyBuilder` constructs Predictive Autoscale Policies.

| Keyword | Purpose |
|---------|---------|
| scale_look_ahead_time | Sets the scale look-ahead time for the policy. |
| scale_mode | Sets the scale mode for the policy. |

---

### FixedDateBuilder Keywords

The `FixedDateBuilder` constructs Fixed Date configurations.

| Keyword | Purpose |
|---------|---------|
| end | Sets the end date for the fixed date. |
| start | Sets the start date for the fixed date. |
| time_zone | Sets the time zone for the fixed date. |

---

### MetricTriggerBuilder Keywords

The `MetricTriggerBuilder` constructs Metric Triggers.

| Keyword | Purpose |
|---------|---------|
| dimensions | Sets dimensions for the metric trigger. |
| divide_per_instance | Divides per instance for the metric trigger. |
| metric_name | Sets the metric name for the metric trigger. |
| metric_namespace | Sets the metric namespace for the metric trigger. |
| metric_resource_location | Sets the metric resource location for the metric trigger. |
| metric_resource_uri | Sets the metric resource URI for the metric trigger. |
| operator | Sets the operator for the metric trigger. |
| statistic | Sets the statistic for the metric trigger. |
| threshold | Sets the threshold for the metric trigger. |
| time_aggregation | Sets the time aggregation for the metric trigger. |
| time_grain | Sets the time grain for the metric trigger. |
| time_window | Sets the time window for the metric trigger. |

---

### RuleBuilder Keywords

The `RuleBuilder` constructs Rules.

| Keyword | Purpose |
|---------|---------|
| metric_trigger | Sets the metric trigger for the rule. |
| scale_action | Sets the scale action for the rule. |

---

### ProfileBuilder Keywords

The `ProfileBuilder` constructs Profiles.

| Keyword | Purpose |
|---------|---------|
| capacity | Sets the capacity for the profile. |
| fixed_date | Sets the fixed date for the profile. |
| name | Sets the name for the profile. |
| recurrence | Sets the recurrence for the profile. |
| rules | Sets the rules for the profile. |

---

### AutoscaleSettingsPropertiesBuilder Keywords

The `AutoscaleSettingsPropertiesBuilder` constructs Autoscale Settings Properties.

| Keyword | Purpose |
|---------|---------|
| enabled | Sets the enabled state for autoscale settings. |
| name | Sets the name for autoscale settings. |
| notifications | Sets notifications for autoscale settings. |
| predictive_autoscale_policy | Sets the predictive autoscale policy for autoscale settings. |
| profiles | Sets profiles for autoscale settings. |
| target_resource_location | Sets the target resource location for autoscale settings. |
| target_resource_uri | Sets the target resource URI for autoscale settings. |

---

### AutoscaleSettingsBuilder Keywords

The `AutoscaleSettingsBuilder` constructs Autoscale Settings.

| Keyword | Purpose |
|---------|---------|
| name | Sets the name for autoscale settings. |
| location | Sets the location for autoscale settings. |
| properties | Sets properties for autoscale settings. |

### Example

This will define autoscale settings for a VM scale set.

```fsharp
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
                    profiles [
                        autoscaleProfile {
                            capacity (
                                autoscaleCapacity {
                                    maximum 10
                                }
                            )
                            rules [ // Two rules, one for scaling out and one for scaling in.
                                autoscaleRule { // Scale up with CPU > 60 across the scale set
                                    metric_trigger (
                                        autoscaleMetricTrigger {
                                            metric_name "Percentage CPU"
                                            divide_per_instance true
                                            operator MetricTriggerOperator.GreaterThan
                                            statistic MetricTriggerStatistic.Average
                                            threshold 60
                                            time_aggregation MetricTriggerTimeAggregation.Average
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
```
