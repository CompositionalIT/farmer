---
title: "B2C Tenant"
date: 2024-02-01T00:00:00+01:00
chapter: false
weight: 1
---

#### Overview
Creates a new B2C tenant, please note that the current implementation only supports the creation of a new B2C tenant.

Usage of this computation expression when a B2C tenant already exists will result in an error, check the [example](#example) for more infos.

#### B2C Tenant Builder Keywords
| Applies To | Keyword             | Purpose                                                                                                                                                                                  |
|-|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| B2C Tenant | initial_domain_name | Initial domain name for the B2C tenant as in `initial_domain_name.onmicrosoft.com`                                                                                                       |
| B2C Tenant | display_name        | Display name for the B2C tenant.                                                                                                                                                         |
| B2C Tenant | sku                 | [SKU](https://learn.microsoft.com/en-us/rest/api/activedirectory/b2c-tenants/list-by-subscription?view=rest-activedirectory-2021-04-01&tabs=HTTP#b2cresourceskuname) for the B2C tenant. |
| B2C Tenant | country_code        | Country code defined by two capital letter, for examples check the official [docs](https://learn.microsoft.com/en-us/azure/active-directory-b2c/data-residency]                          |
| B2C Tenant | data_residency      | Data residency for the B2C tenant, for more infos check the official [docs](https://learn.microsoft.com/en-us/azure/active-directory-b2c/data-residency]                                 |
| B2C Tenant | tags                | Tags for the B2C tenant.                                                                                                                                                                 |

#### Example

Basic creation of a B2C tenant, while avoiding having an error when such tenant already exists.

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Deploy

let initialDomainName = "myb2c"

let myb2c =
    b2cTenant {
        initial_domain_name initialDomainName
        display_name "My B2C"
        sku B2cTenant.Sku.PremiumP1
        country_code "FR"
        data_residency B2cDataResidency.Europe
    }
    
let b2cDoesNotExist (initialDomainName: string) =
    let output =
        Az.AzHelpers.executeAz $"resource list --name '{initialDomainName}.onmicrosoft.com'"
        |> snd
    not (output.Contains initialDomainName)
    
let deployment =
    arm {
        location Location.FranceCentral
        add_resources
            [
                // This allows to avoid having an error when the B2C tenant already exists
                if b2cDoesNotExist initialDomainName then
                    myb2c
            ]
    }
````
