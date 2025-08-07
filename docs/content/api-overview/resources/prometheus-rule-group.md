---
title: "Alert Management - Prometheus Rule Groups"
date: 2025-08-07T19:30:59+02:00
chapter: false
weight: 1
---

#### Overview
The Prometheus Rule Group builder is used to create prometheus rule groups which can then be applied to Prometheus metrics in an Azure Monitor workspace.

* Prometheus Rule Groups (`Microsoft.AlertsManagement/prometheusRuleGroups`)

#### Prometheus Rule Group Builder Keywords
The Prometheus Rule Group builder (`prometheusRuleGroup`) constructs prometheus rule groups.

| Keyword                             | Purpose                                                                                                                                                     |
|-------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| name                                | Sets the name of the Prometheus Rule Group.                                                                                                                           |
| description                                 | Specifies the description of the Prometheus Rule Group.                                                                                                   |
| cluster_name                                | Specifies the name of the AKS cluster associated with this rule group.                                                                                                  |
| interval                          | Specifies the interval to run the Prometheus rule group in ISO 8601 duration format.                                                                                                                     |
| add_rules                     | Adds rules to the Prometheus rule group. At least one rule is required.                                                                                                |
| enable_rule_group                | Enables the rule group.                                                                                     |
| disable_rule_group              | Disables the rule group.                                             |
| azure_monitor_workspace_id              | Specifies the Azure monitor workspace id. This is required.                                          |
| scopes              | Specifies the scopes for the Prometheus rule groups. Will at least contain the `azure_monitor_workspace_id` by default.                                          |

#### Prometheus Rule Builder
The Prometheus Rule builder (`prometheusRule`) creates Prometheus Rule for the Prometheus Rule Group.

| Keyword | Purpose |
|-|-|
| record | Specifies recorded metrics name for Prometheus Rule. |
| expression | Specifies a PromQL expression to evaluate. This is required |
| labels | Specifies labels to add or overwrite before storing the result. |
| enable_rule | Enables the Prometheus rule. |
| disable_rule | Disables the Prometheus rule. |
| alert | Sets the alert rule name for Prometheus Rule. |
| severity | Specifies the severity of alerts fired by Prometheus Rule.  |
| actions | Specifies actions that are performed when the alert rule becomes active.  |
| resolve_configuration | Defines configuration for resolving alerts  |

#### Basic Example

The simplest Prometheus Rule group requires at least one rule and specify `azure_monitor_workspace_id`

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.AlertsManagement

let myRule = prometheusRule {
        expression "up == 1"
    }

let monitoringAccountType =
    ResourceType("Microsoft.Monitor/accounts", "2025-05-03-preview")

let monitorAccountId =
    ResourceId.create (monitoringAccountType, ResourceName "monitorAccount")

let myGroup = prometheusRuleGroup {
    name "myGroup"
    add_rules [ myRule ]
    azure_monitor_workspace_id monitorAccountId
}
```
