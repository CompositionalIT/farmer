---
title: "Gallery"
date: 2023-02-20T10:20:00-04:00
weight: 10
chapter: false
---

#### Overview
The `gallery` builder is used to create Galleries for sharing VM images.

* Galleries (`Microsoft.Compute/galleries`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
|name|Name of the gallery resource, up to 80 characters, alphanumerics and periods.|
|description|Description for the gallery.|
|sharing_profile|Sharing profile that must be set to share with the community or groups.|
|soft_delete|Indicate soft deletion of images should be enabled or disabled (default disabled).|
|add_tags|Add tags to the gallery resource.|
|depends_on|Add explicit dependencies for the gallery resource.|

#### Example

```fsharp
open Farmer
open Farmer.Arm.Gallery
open Farmer.Builders

gallery {
    name "mygallery"
    description "Example Community Image Gallery"
    sharing_profile (
        Community {
            Eula = "End User License Agreement goes here"
            PublicNamePrefix = "farmages"
            PublisherContact = "farmer.gallery@example.com"
            PublisherUri = System.Uri "https://compositionalit.github.io/farmer"
        }
    )
}

```
