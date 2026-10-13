---
title: "API Management"
---

Farmer provides builders for an Azure API Management service and its supported child resources. Each builder emits the correct ARM resource type, parent-qualified name, and parent dependency.

## API Management service

The service builder is `apiManagementService`.

| Keyword | Description |
| --- | --- |
| `name` | Sets the API Management service name. |
| `location` | Sets the Azure region. |
| `publisher_name` | Sets the publisher name shown by the service. |
| `publisher_email` | Sets the publisher email address. |
| `sku` | Sets the service tier: `Consumption`, `Developer`, `Basic`, `Standard`, or `Premium`. |
| `capacity` | Sets the number of units for the selected SKU. |
| `public_network_access` | Sets public network access, normally `Enabled` or `Disabled`. |
| `virtual_network_type` | Sets the virtual network mode: `None`, `External`, or `Internal`. |
| `disable_gateway` | Disables the managed gateway. |
| `custom_property` | Adds a gateway custom property by key and value. |
| `add_tag` / `add_tags` | Adds one or more resource tags. |

```fsharp
open Farmer
open Farmer.Builders.ApiManagement

let apim = apiManagementService {
    name "my-api-management"
    location Location.WestEurope
    publisher_name "Contoso"
    publisher_email "api-admin@contoso.com"
    sku ApiManagementSku.Developer
    capacity 1
    public_network_access "Enabled"
    virtual_network_type "None"
    custom_property "Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Tls10" "false"
    add_tag "environment" "production"
}
```

## API

The API builder is `apiManagementApi`. It creates `Microsoft.ApiManagement/service/apis` and automatically appends the revision to the ARM name (`name;rev=<revision>`).

| Keyword | Description |
| --- | --- |
| `service` | Sets the parent API Management service name. |
| `name` | Sets the logical API name. |
| `display_name` | Sets the API display name. |
| `path` | Sets the gateway URL path for the API. |
| `protocols` | Sets the supported protocol list, for example `[ "https" ]`. |
| `service_url` | Sets the backend service URL. |
| `description` | Sets the API description. |
| `api_version` | Sets the API version label. |
| `revision` | Sets the API revision, defaulting to `"1"`. |
| `subscription_required` | Controls whether callers need a subscription key. |

```fsharp
let catalogApi = apiManagementApi {
    service "my-api-management"
    name "catalog"
    display_name "Catalog API"
    path "catalog"
    protocols [ "https" ]
    service_url "https://catalog.contoso.com"
    description "The catalog API"
    revision "1"
    subscription_required true
}
```

## API operation

The operation builder is `apiManagementOperation`. It creates `Microsoft.ApiManagement/service/apis/operations`.

| Keyword | Description |
| --- | --- |
| `service` | Sets the parent API Management service name. |
| `api` | Sets the revision-qualified API name, for example `ResourceName "catalog;rev=1"`. |
| `name` | Sets the operation name. |
| `display_name` | Sets the operation display name. |
| `method` | Sets the HTTP method. |
| `url_template` | Sets the operation URL template. |
| `description` | Sets the operation description. |

```fsharp
let listCatalog = apiManagementOperation {
    service "my-api-management"
    api "catalog;rev=1"
    name "list"
    display_name "List catalog items"
    method "GET"
    url_template "/items"
    description "Lists catalog items"
}
```

## Product

The product builder is `apiManagementProduct`. It creates `Microsoft.ApiManagement/service/products`.

| Keyword | Description |
| --- | --- |
| `service` | Sets the parent API Management service name. |
| `name` | Sets the product name. |
| `display_name` | Sets the product display name. |
| `description` | Sets the product description. |
| `terms` | Sets the terms shown to subscribers. |
| `subscription_required` | Controls whether subscriptions are required. |
| `approval_required` | Controls whether subscriptions require approval. |
| `subscriptions_limit` | Sets the maximum number of subscriptions. |
| `state` | Sets the product state, such as `published` or `notPublished`. |

```fsharp
let publicProduct = apiManagementProduct {
    service "my-api-management"
    name "public"
    display_name "Public APIs"
    description "APIs available to external consumers"
    subscription_required false
    approval_required false
    state "published"
}
```

## Backend

The backend builder is `apiManagementBackend`. It creates `Microsoft.ApiManagement/service/backends`.

| Keyword | Description |
| --- | --- |
| `service` | Sets the parent API Management service name. |
| `name` | Sets the backend name. |
| `url` | Sets the backend endpoint URL. |
| `protocol` | Sets the backend protocol, normally `http` or `https`. |
| `title` | Sets a display title. |
| `description` | Sets the backend description. |
| `resource_id` | Associates the backend with an Azure resource ID. |

```fsharp
let catalogBackend = apiManagementBackend {
    service "my-api-management"
    name "catalog-backend"
    url "https://catalog.contoso.com"
    protocol "https"
    title "Catalog backend"
}
```

## Named value

The named-value builder is `apiManagementNamedValue`. It creates `Microsoft.ApiManagement/service/namedValues`.

| Keyword | Description |
| --- | --- |
| `service` | Sets the parent API Management service name. |
| `name` | Sets the named-value name. |
| `display_name` | Sets the display name. |
| `value` | Sets the value available to policies. |
| `secret` | Marks the value as secret. |
| `tags` | Sets the named-value tag list. |

```fsharp
let catalogKey = apiManagementNamedValue {
    service "my-api-management"
    name "catalog-key"
    display_name "Catalog API key"
    value "secret-value"
    secret
    tags [ "catalog"; "external" ]
}
```

## Policy

The policy builder is `apiManagementPolicy`. It creates a policy resource below the selected scope.

| Keyword | Description |
| --- | --- |
| `service` | Sets the parent API Management service name. |
| `scope` | Sets the policy scope, such as `service` or `apis`. |
| `api` | Sets the parent revision-qualified API name when `scope` is `apis`. |
| `name` | Sets the policy resource name. |
| `format` | Sets the policy format, normally `rawxml`. |
| `xml` | Sets the policy XML document. |

```fsharp
let catalogPolicy = apiManagementPolicy {
    service "my-api-management"
    scope "apis"
    api "catalog;rev=1"
    name "policy"
    format "rawxml"
    xml "<policies><inbound><base /></inbound><outbound><base /></outbound></policies>"
}
```

## Deploying the resources

All builders implement Farmer's normal builder interface and can be composed in one template:

```fsharp
let template =
    arm {
        add_resources [
            apim
            catalogApi
            listCatalog
            publicProduct
            catalogBackend
            catalogKey
            catalogPolicy
        ]
    }
```

`apiManagementChild` is retained as an escape hatch for an Azure child resource that does not yet have a typed Farmer builder. It is not required for the supported builders above and is not the preferred API for them.
