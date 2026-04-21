---
title: "Azure Monitor - Data Collection"
date: 2025-08-07T19:30:59+02:00
chapter: false
weight: 1
---

#### Overview
The Data Collection Rules builder is used to create data collection rules used to manage data ingestion in Azure Monitor. The Data Collection Endpoints builder is used to create data collection endpoints in Azure Monitor. 

* Data Collection Rules (`Microsoft.Insights/dataCollectionRules`)

* Data Collection Endpoint (`Microsoft.Insights/dataCollectionEndpoints`)

* Data Collection Rule Association (`/providers/dataCollectionRuleAssociations`)


#### Data Collection Rules Builder Keywords
The Data Collection Rules builder (`dataCollectionRule`) constructs data collection rule.

| Keyword                             | Purpose                                                                                                                                                     |
|-------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| name                                | Sets the name of the Data Collection Rule.                                                                                                                           |
| os_type                                 | Specifies the kind for the data collection rule: `Windows` or `Linux`.                                                                                                   |
| endpoint                                | Specifies the endpoint associated for the data collection rule.                                                                                                |
| data_flows                          | Sets the data flows for the data collection rule.                                                                                                                   |
| destinations                     | Sets the destinations for the data collection rule.                                                                        |
| data_sources                | Sets the data sources for the data collection rule.
                                                                                     |
#### Data Collection Endpoint Builder
The Data Collection Endpoint builder (`dataCollectionEndpoint`) creates data collection endpoint.

| Keyword | Purpose |
|-|-|
| name | Specifies the data collection endpoint name. |
| os_type | Specifies the kind for the data collection endpoint: `Windows` or `Linux` |

#### Data Collection Rule Association Builder
The Data Collection Association builder (`dataCollectionRuleAssociationEndpoint`) creates data collection rule association for a separate resource.

| Keyword | Purpose |
|-|-|
| name | Specifies the data collection rule association name. |
| associated_resource | Specifies the resource id of associated resource linked to the data collection rule. This is required. |
| rule_id | Specifies the rule id to be associated with a resource specified. This is required. |
| description | Specifies description for the data collection rule association. |


#### Basic Examples

The simplest Data Collection Rule requires an endpoint. 

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Monitor

let myEndpoint = dataCollectionEndpoint {
    name "myEndpoint"
    os_type OS.Linux
}

let myRule = dataCollectionRule {
    endpoint (dataCollectionEndpoints.resourceId "myEndpoint")
}
```

The simplest Data Collection Endpoint. 

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Monitor

let myEndpoint = dataCollectionEndpoint {
    name "myEndpoint"
}
```

The simplest Data Collection Rule Association with AKS. 

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Arm.Monitor
open Farmer.Arm.ContainerService

let myAks = aks {
    name "myAks"
    service_principal_use_msi
    enable_azure_monitor
}

let myRule = dataCollectionRule {
    name "myRule"
    os_type OS.Linux
    endpoint (dataCollectionEndpoints.resourceId "myEndpoint")
}

let expectedRuleId = (myRule :> IBuilder).ResourceId

let ruleAssociation = dataCollectionRuleAssociation {
    name "myRuleAssociation"
    associated_resource ((myAks :> IBuilder).ResourceId)
    rule_id expectedRuleId
}
```
