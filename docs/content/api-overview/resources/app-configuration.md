---
title: "App Configuration"
date: 2026-04-17T00:00:00+00:00
chapter: false
weight: 1
---

#### Overview
The App Configuration builder creates Azure App Configuration stores, which are a central repository for managing application settings and feature flags.

* Configuration Store (`Microsoft.AppConfiguration/configurationStores`)
* Key Values (`Microsoft.AppConfiguration/configurationStores/keyValues`)

#### Builder Keywords

##### configurationStore

| Keyword | Purpose |
|-|-|
| name | Sets the name of the App Configuration store. |
| sku | Sets the SKU of the store. Defaults to `Free`. Options: `Free`, `Developer`, `Standard`, `Premium`. |
| disable_local_auth | Disables local (access-key) authentication, requiring Azure AD / RBAC only. |
| enable_purge_protection | Enables purge protection (Standard SKU only). Once enabled, it cannot be disabled. |
| public_network_access | Sets public network access: `Enabled` or `Disabled`. |
| soft_delete_retention_in_days | Sets the soft-delete retention period in days (Standard SKU only, 1-7 days). |
| data_plane_authentication_mode | Sets the data plane authentication mode: `Local` or `Passthrough`. |
| add_feature_flag | Adds a single feature flag to the store. |
| add_feature_flags | Adds a list of feature flags to the store. |
| add_key_value | Adds a single key-value item to the store. |
| add_key_values | Adds a list of key-value items to the store. |
| add_tag | Adds an ARM tag to the store. |
| add_tags | Adds multiple ARM tags to the store. |
| depends_on | Adds a dependency to the store. |

#### Key Value Fields

| Field | Purpose |
|-|-|
| Key | The key name for the item. |
| Label | An optional label to distinguish between different environments or configurations. |
| Value | The value to store. |
| ContentType | Optional MIME content type (e.g. `application/json` for JSON values). |
| KeyValueTags | Optional App Configuration item tags (not ARM resource tags). |

#### Feature Flag Fields

| Field | Purpose |
|-|-|
| Name | The name of the feature flag. |
| Description | A human-readable description of the flag. |
| Label | An optional label (can be empty string for no label). |
| State | `true` if the flag is enabled, `false` if disabled. |

#### Configuration Members

| Member | Purpose |
|-|-|
| ResourceId | The ARM resource ID of the App Configuration store. |
| Endpoint | Returns an ARM expression for the data plane endpoint URL of the store. |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.ConfigurationStore

let myConfig = configurationStore {
    name "my-app-config"
    sku Standard
    disable_local_auth
    add_tags [ "env", "prod"; "team", "platform" ]

    add_feature_flags [
        {
            Name = "DarkMode"
            Description = "Enable dark mode UI"
            Label = ""
            State = true
        }
        {
            Name = "BetaFeature"
            Description = "Beta feature available to select users"
            Label = "beta"
            State = false
        }
    ]

    add_key_values [
        {
            Key = "Api:BaseUrl"
            Label = Some "prod"
            Value = "https://api.example.com"
            ContentType = None
            KeyValueTags = Map.empty
        }
        {
            Key = "FeatureSettings"
            Label = None
            Value = """{"timeout":30,"retries":3}"""
            ContentType = Some "application/json"
            KeyValueTags = Map [ "environment", "production" ]
        }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myConfig
}
```
