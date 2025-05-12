---
title: "Gallery"
date: 2023-02-20T10:20:00-04:00
weight: 10
chapter: false
---

#### Overview
The `gallery` builder is used to create Galleries for sharing VM images. These can be used to create virtual machines or virtual machine scale sets that can expand or contract to scale capacity as needed.

* Galleries (`Microsoft.Compute/galleries`)
* Gallery Images (`Microsoft.Compute/galleries/images`)
* Gallery Applications (`Microsoft.Compute/galleries/applications`)
* Gallery Application Versions (`Microsoft.Compute/galleries/applications/versions`)

#### Builder Keywords

| Applies To        | Keyword                  | Purpose                                                                                                                                                                                                         |
|-------------------|--------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| gallery           | name                     | Name of the gallery resource, up to 80 characters, alphanumerics and periods.                                                                                                                                   |
| gallery           | description              | Description for the gallery.                                                                                                                                                                                    |
| gallery           | sharing_profile          | Sharing profile that must be set to share with the community or groups.                                                                                                                                         |
| gallery           | soft_delete              | Indicate soft deletion of images should be enabled or disabled (default disabled).                                                                                                                              |
| gallery           | add_tags                 | Add tags to the gallery resource.                                                                                                                                                                               |
| gallery           | depends_on               | Add explicit dependencies for the gallery resource.                                                                                                                                                             |
| galleryImage      | name                     | Name of the image in the gallery.                                                                                                                                                                               |
| galleryImage      | gallery_name             | Name of the gallery where the image is created.                                                                                                                                                                 |
| galleryImage      | gallery                  | Specify the gallery in the same deployment. The image will depend on the gallery.                                                                                                                               |
| galleryImage      | architecture             | Indicates x64 or ARM 64 images - defaults to x64 if not set.                                                                                                                                                    |
| galleryImage      | description              | Optional description for the image in the gallery.                                                                                                                                                              |
| galleryImage      | eula                     | Optional End User License Agreement for using the image.                                                                                                                                                        |
| galleryImage      | hyperv_generation        | The Hyper-V generation for the image. This should be set to match the Hyper-V generation of the source image that was used to create this image.                                                                |
| galleryImage      | gallery_image_identifier | The publisher, offer, and sku for the image in the gallery.                                                                                                                                                     |
| galleryImage      | os_state                 | Indicate if the VM is Generalized or Specialized. A generalized image allows OS configuration options, such as setting the username and password, whereas this is typically set already in a specialized image. |
| galleryImage      | os_type                  | OS type for the image - Windows or Linux                                                                                                                                                                        |
| galleryImage      | privacy_statement_uri    | URI where the privacy statement for the use of the image can be found.                                                                                                                                          |
| galleryImage      | purchase_plan            | A purchase plan name, publisher, and product for the image, for use in a community gallery or marketplace images.                                                                                               |
| galleryImage      | recommended_configuration | Recommended range of vCPUs and memory for VMs created from this image.                                                                                                                                          |
| galleryImage      | recommended_memory       | A recommended range of memory for VMs created from this image. Default is 1 Gb to 32 Gb.                                                                                                                        |
| igalleryImagemage | recommended_vcpu         | A recommended range of vCPUs for VMs created from this image. The default is 1 to 16.                                                                                                                           |
| galleryImage      | release_notes_uri        | URI where release notes can be found for the image.                                                                                                                                                             |
| galleryImage      | add_tags                 | Add tags to the image resource.                                                                                                                                                                                 |
| galleryImage      | depends_on               | Add explicit dependencies for the image resource.                                                                                                                                                               |
| galleryApp        | name                     | Name of the gallery application.                                                                                                                                                                                |
| galleryApp        | gallery                  | Gallery where the application is distributed.                                                                                                                                                                   |
| galleryApp        | description              | Description of the gallery application.                                                                                                                                                                         |
| galleryApp        | os_type                  | Operating system required by the gallery application (Windows or Linux).                                                                                                                                        |
| galleryApp        | add_tags                 | Add tags to the gallery application resource.                                                                                                                                                                   |
| galleryApp        | depends_on               | Add explicit dependencies for the gallery application resource.                                                                                                                                                 |
| galleryAppVersion | name                     | Name of the gallery application.                                                                                                                                                                                |
| galleryAppVersion | gallery_app              | Gallery Application for the Version.                                                                                                                                                                            |
| galleryAppVersion | gallery                  | Gallery where the application version is distributed.                                                                                                                                                           |
| galleryAppVersion | end_of_life              | Date when the Gallery Application Version is not longer supported.                                                                                                                                              |
| galleryAppVersion | install_action           | Shell script to execute to install the the application version.                                                                                                                                                 |
| galleryAppVersion | remove_action            | Shell script to execute to remove the the application version.                                                                                                                                                  |
| galleryAppVersion | source_media_link        | A storage account link (public or SAS URL) for media to include.                                                                                                                                                |
| galleryAppVersion | add_target_regions       | Adds regions where the gallery application version should be distributed for VM installation.                                                                                                                   |
| galleryAppVersion | add_tags                 | Add tags to the gallery application version resource.                                                                                                                                                           |
| galleryAppVersion | depends_on               | Add explicit dependencies for the gallery application version resource.                                                                                                                                         |

#### Example of a Gallery Image

```fsharp
open Farmer
open Farmer.Arm.Gallery
open Farmer.Builders

let myGallery =
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

let myImage =
    galleryImage {
        name "my-server-image"
        gallery myGallery

        gallery_image_identifier (
            {
                Offer = "my-server"
                Publisher = "farmages"
                Sku = "my-server-2023"
            }
        )

        hyperv_generation Image.HyperVGeneration.V2
        os_state Image.OsState.Generalized
        os_type OS.Linux
    }

arm {
    add_resources [
        myGallery
        myImage
    ]
}
```

#### Example of a Gallery Application Version

```fsharp
open Farmer
open Farmer.Arm.Gallery
open Farmer.Builders

let myGallery =
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

let myGalleryApp =
    galleryApp {
        name "java-app.tar.gz"
        gallery myGallery
        os_type OS.Linux
    }

let myGalleryAppVersion = galleryAppVersion {
    name "1.0.1"
    gallery_app myGalleryApp
    gallery myGallery
    end_of_life (DateTimeOffset(DateTime(2026, 9, 30)))
    install_action "sudo apt-get update && sudo apt-get -y install openjdk-17-jre-headless"
    remove_action "sudo apt-get remove openjdk-17-jre-headless && sudo apt-get autoremove"
    source_media_link "https://mystorageaccount/sas-url"

    add_target_regions [
        targetRegion { name Location.EastUS }
        targetRegion { name Location.EastUS2 }
        targetRegion { name Location.WestUS2 }
        targetRegion { name Location.WestUS3 }
        targetRegion { name Location.NorthEurope }
        targetRegion { name Location.WestEurope }
    ]
}

arm {
    add_resources [
        myGallery
        myGalleryApp
        myGalleryAppVerion
    ]
}
```
