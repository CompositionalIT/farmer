---
title: "Network Manager"
date: 2026-04-15T00:00:00-00:00
chapter: false
weight: 5
---

#### Overview

The Network Manager builders are used to create an Azure Network Manager and its associated resources for centralized network policy management across subscriptions or management groups.

- Network Manager (`Microsoft.Network/networkManagers`)
- Network Manager Group (`Microsoft.Network/networkManagers/networkGroups`)
- Security Admin Configuration (`Microsoft.Network/networkManagers/securityAdminConfigurations`)
- Security Admin Rule Collection (`Microsoft.Network/networkManagers/securityAdminConfigurations/ruleCollections`)
- Security Admin Rule (`Microsoft.Network/networkManagers/securityAdminConfigurations/ruleCollections/rules`)

#### Builder Keywords

##### `networkManager`

| Keyword                               | Purpose                                                                                                                                 |
|---------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| name                                  | Sets the name of the Network Manager.                                                                                                   |
| description                           | Sets an optional description.                                                                                                           |
| add_scope_subscription                | Adds a subscription to the scope. A bare subscription ID (GUID) is automatically prefixed with `/subscriptions/`.                       |
| add_scope_subscriptions               | Adds multiple subscriptions to the scope. Bare subscription IDs are automatically prefixed with `/subscriptions/`.                      |
| add_scope_management_groups           | Adds management group resource IDs to the scope.                                                                                        |
| add_scope_access                      | Adds a scope access type — `SecurityAdmin` or `Connectivity`.                                                                           |
| add_scope_accesses                    | Adds multiple scope access types.                                                                                                       |
| add_network_groups                    | Adds one or more `networkManagerGroup` configurations to be created under this manager.                                                  |
| add_security_admin_configurations     | Adds one or more `networkManagerSecurityAdminConfiguration` configurations to be created under this manager.                             |
| depends_on                            | Specifies explicit resource dependencies.                                                                                               |
| add_tags                              | Adds resource tags.                                                                                                                     |
| add_tag                               | Adds a single resource tag.                                                                                                             |

##### `networkManagerGroup`

A network group is a logical container for virtual networks. It can be defined inline inside a `networkManager {}` block via `add_network_groups`, or created independently by linking it to an existing manager.

| Keyword                              | Purpose                                                                                                                     |
|--------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| name                                 | Sets the name of the network group.                                                                                         |
| description                          | Sets an optional description.                                                                                               |
| link_to_network_manager              | Links this group to a Farmer-managed Network Manager in the same deployment.                                                |
| link_to_unmanaged_network_manager    | Links this group to an existing Network Manager outside this deployment.                                                    |

##### `networkManagerSecurityAdminConfiguration`

A security admin configuration is the top-level container for rule collections. It can be defined inline inside `networkManager {}` via `add_security_admin_configurations`, or created independently by linking it to a manager.

| Keyword                              | Purpose                                                                                                                     |
|--------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| name                                 | Sets the name of the security admin configuration.                                                                          |
| description                          | Sets an optional description.                                                                                               |
| add_rule_collections                 | Adds one or more `networkManagerSecurityAdminRuleCollection` configurations.                                                 |
| link_to_network_manager              | Links this configuration to a Farmer-managed Network Manager in the same deployment.                                        |
| link_to_unmanaged_network_manager    | Links this configuration to an existing Network Manager outside this deployment.                                            |

##### `networkManagerSecurityAdminRuleCollection`

A rule collection groups security admin rules and specifies which network groups they apply to. At least one network group must be supplied via `add_applies_to_group`; a `FarmerException` is raised at build time if none are provided.

| Keyword                              | Purpose                                                                                                                     |
|--------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| name                                 | Sets the name of the rule collection.                                                                                       |
| description                          | Sets an optional description.                                                                                               |
| add_applies_to_group                 | Adds a network group resource ID that this rule collection targets. At least one is required.                                |
| add_rules                            | Adds one or more `networkManagerSecurityAdminRule` configurations.                                                          |

##### `networkManagerSecurityAdminRule`

Individual security admin rules control traffic flow. Sources and destinations may each be either IP prefixes or service tags, but cannot mix both types within the same direction — a `FarmerException` is raised at build time if mixing is attempted. When no port ranges are specified, all ports (`0-65535`) are used by default.

| Keyword                              | Purpose                                                                                                                     |
|--------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| name                                 | Sets the name of the rule.                                                                                                  |
| description                          | Sets an optional description.                                                                                               |
| priority                             | Sets the rule priority (1–4096, default: 100).                                                                              |
| direction                            | Sets traffic direction: `SecurityAdmin.Inbound` or `SecurityAdmin.Outbound`.                                                |
| allow_traffic                        | Sets the rule action to `Allow` (default).                                                                                  |
| always_allow_traffic                 | Sets the rule action to `AlwaysAllow` — overrides lower-priority deny rules.                                                |
| deny_traffic                         | Sets the rule action to `Deny`.                                                                                             |
| protocol                             | Sets the protocol: `SecurityAdmin.TCP`, `SecurityAdmin.UDP`, `SecurityAdmin.ICMP`, `SecurityAdmin.ESP`, `SecurityAdmin.AH`, or `SecurityAdmin.AnyProtocol`. |
| add_source_ip_prefix                 | Adds an IP prefix as a source address (e.g. `"10.0.0.0/8"`).                                                               |
| add_source_service_tag               | Adds a service tag as a source (e.g. `"Internet"`, `"AzureCloud"`).                                                        |
| add_destination_ip_prefix            | Adds an IP prefix as a destination address (e.g. `"192.168.0.0/16"`).                                                      |
| add_destination_service_tag          | Adds a service tag as a destination (e.g. `"VirtualNetwork"`).                                                              |
| add_source_port_range                | Adds a source port or range (e.g. `"80"`, `"1024-65535"`).                                                                 |
| add_source_port_ranges               | Adds multiple source port ranges.                                                                                           |
| add_destination_port_range           | Adds a destination port or range.                                                                                            |
| add_destination_port_ranges          | Adds multiple destination port ranges.                                                                                      |

#### Example

This example creates a Network Manager spanning two subscriptions with a security admin configuration that blocks inbound Internet traffic on common management ports.

```fsharp
#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.Arm.NetworkManager

let prodGroup = networkManagerGroup {
    name "prod-vnets"
    description "Production virtual networks"
}

let blockManagementFromInternet = networkManagerSecurityAdminRule {
    name "deny-internet-management"
    priority 100
    direction SecurityAdmin.Inbound
    deny_traffic
    protocol SecurityAdmin.TCP
    add_source_service_tag "Internet"
    add_destination_ip_prefix "10.0.0.0/8"
    add_destination_port_range "22"
    add_destination_port_range "3389"
}

let baselineCollection = networkManagerSecurityAdminRuleCollection {
    name "baseline-rules"
    add_applies_to_group (networkManagerGroups.resourceId (ResourceName "my-network-manager/prod-vnets"))
    add_rules [ blockManagementFromInternet ]
}

let baselineConfig = networkManagerSecurityAdminConfiguration {
    name "baseline-config"
    add_rule_collections [ baselineCollection ]
}

let myManager = networkManager {
    name "my-network-manager"
    description "Centralised network policy manager"
    // Bare subscription GUIDs are automatically prefixed with /subscriptions/
    add_scope_subscription "00000000-0000-0000-0000-000000000001"
    add_scope_subscription "00000000-0000-0000-0000-000000000002"
    add_scope_access SecurityAdmin
    add_network_groups [ prodGroup ]
    add_security_admin_configurations [ baselineConfig ]
}

arm {
    location Location.EastUS
    add_resource myManager
}
```

#### Standalone Builders (linking to an existing manager)

Both `networkManagerGroup` and `networkManagerSecurityAdminConfiguration` implement `IBuilder` and can be added directly to an `arm {}` block without a wrapping `networkManager {}`, by linking to a pre-existing or Farmer-managed manager.

```fsharp
#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.Arm.NetworkManager

// Attach a group to a pre-existing network manager
let extraGroup = networkManagerGroup {
    name "dev-vnets"
    description "Developer virtual networks"
    link_to_unmanaged_network_manager (networkManagers.resourceId (ResourceName "my-network-manager"))
}

// Attach a security admin configuration to a pre-existing network manager
let extraConfig = networkManagerSecurityAdminConfiguration {
    name "extra-config"
    link_to_unmanaged_network_manager (networkManagers.resourceId (ResourceName "my-network-manager"))
    add_rule_collections [ baselineCollection ]
}

arm {
    location Location.EastUS
    add_resource extraGroup
    add_resource extraConfig
}
```
