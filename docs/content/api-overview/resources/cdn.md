---
title: "CDN"
chapter: false
weight: 3
---

#### Overview
The CDN builder is used to create Azure Content Delivery Network instances.

* CDN Profile (`Microsoft.Cdn/profiles`)
* CDN Endpoint (`Microsoft.Cdn/profiles/endpoints`)
* CDN Custom Domain (`Microsoft.Cdn/profiles/endpoints/customDomains`)

There are two builders available:
* The CDN builder, which maps to a CDN profile.
* The Endpoint builder, which creates endpoints and custom domains. Endpoints are created within a CDN.

#### CDN Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the CDN instance. |
| sku | Sets the SKU of the CDN instance. Defaults to Standard Akamai. |
| add_endpoints | Adds several endpoints to the CDN. |

#### Endpoint Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the endpoint instance. If you do not set this, a name is generated based on the origin. |
| origin | Sets the address of the origin and is used to auto-generate the endpoint name if none if supplied. |
| depends_on | Sets dependencies on this endpoint. |
| add_compressed_content | Adds a set of content types to compress. |
| query_string_caching_behaviour | Specifies the Query String Caching Behaviour. |
| enable_http | Enables HTTP delivery on the endpoint. |
| disable_http | Disables HTTP delivery on the endpoint. |
| enable_https | Enables HTTPS delivery on the endpoint. |
| disable_https | Disables HTTPS delivery on the endpoint. |
| custom_domain | Sets the custom domain name to use on the endpoint. |
| optimise_for | Optimises delivery for a specific type of content. |

> Storage Accounts and Web Apps have special support for CDN endpoints. You can supply a storage
> account or web app builders directly as the origin.

#### Example
```fsharp
let isaacWebApp = webApp {
    name "isaacsuperweb"
    app_insights_off
}

let isaacStorage = storageAccount {
    name "isaacsuperstore"
}

let isaacCdn = cdn {
    name "isaacsupercdn"
    add_endpoints [
        endpoint {
            origin isaacStorage
            optimise_for Cdn.OptimizationType.LargeFileDownload
        }
        endpoint {
            origin isaacWebApp
            disable_http
        }
        endpoint {
            name "custom-endpoint-name"
            origin "mysite.com"
            add_compressed_content [ "text/plain"; "text/html"; "text/css" ]
            query_string_caching_behaviour Cdn.BypassCaching
        }
    ]
}
```