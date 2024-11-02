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

There are three builders available:
* The CDN builder, which maps to a CDN profile.
* The Endpoint builder, which creates endpoints and custom domains. Endpoints are created within a CDN.
* The CDN Rule builder, which creates CDN rules with conditions and actions that can be added to an endpoint

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
| origin | Sets the address of the origin and is used to auto-generate the endpoint name if none is supplied. |
| depends_on | [Sets dependencies on this endpoint](../../dependencies/). |
| add_compressed_content | Adds a set of content types to compress. |
| query_string_caching_behaviour | Specifies the Query String Caching Behaviour. |
| enable_http | Enables HTTP delivery on the endpoint. |
| disable_http | Disables HTTP delivery on the endpoint. |
| enable_https | Enables HTTPS delivery on the endpoint. |
| disable_https | Disables HTTPS delivery on the endpoint. |
| custom_domain | Sets the custom domain name to use on the endpoint. |
| optimise_for | Optimises delivery for a specific type of content. |
| add_rule | Adds a single rule to the endpoint delivery policy.
| add_rules | Adds multiple rule to the endpoint delivery policy.

#### CDN Rule Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the rule. |
| order | Sets the order of rule. |
| when_device_type | Adds device type condition. |
| when_http_version | Adds http version condition.|
| when_request_cookies | Adds request cookies condition. |
| when_post_argument | Adds post argument condition. |
| when_query_string | Adds query string condition. |
| when_remote_address | Adds remote address condition. |
| when_request_body | Adds request body condition. |
| when_request_header | Adds request header condition. |
| when_request_method | Adds request method condition. |
| when_request_protocol | Adds request protocol condition. |
| when_request_url |Adds request URL condition. |
| when_url_file_extension | Adds URL file extension condition. |
| when_url_file_name |Adds URL file name condition. |
| when_url_path | Adds URL path condition. |
| cache_expiration |Adds cache expiration action. |
| cache_key_query_string | Adds cache key query string action. |
| modify_request_header |Adds modify request header action. |
| modify_response_header | Adds modify response header action. |
| url_rewrite | Adds URL rewrite action. |
| url_redirect |Adds URL redirect action. |

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

let isaacRule = cdnRule {
    name "isaacsuperrule"
    order 1
    when_request_header "issac" Contains ["great"] ToLowercase
    modify_response_header Append "issac" "super"
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
            add_rule isaacRule
        }
    ]
}
```
