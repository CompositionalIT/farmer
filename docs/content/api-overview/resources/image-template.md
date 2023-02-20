---
title: "Image Template"
date: 2023-02-19T10:20:00-04:00
weight: 10
chapter: false
---

#### Overview
The Image Template builder is used to create Image Templates for generating VM images. An image template starts with a source image, runs several customizations, and then can finally distribute the image to one or more destinations, including an Azure Image Gallery, a managed image, or a VHD file.

* Image Templates (`Microsoft.VirtualMachineImages/imageTemplates`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| imageTemplate | name | Sets the name of the App Insights instance. |
| imageTemplate | add_identity | Adds a managed identity to the the imageTemplate that it uses to access resources when building the image and to publish images to a gallery. |
| imageTemplate | build_timeout | Timeout for the image builder, after which it will fail. Default is 4 hours. |
| imageTemplate | source_platform_image | Specify the source image to be customized from the Azure Gallery. | 
| imageTemplate | source_managed_image | Specify the source as an existing managed image. |
| imageTemplate | source_shared_image_version | Specify the source image to be customized from a shared image gallery. |
| imageTemplate | add_customizers | Add customizers to make changes to the image being built. |
| imageTemplate | add_distributors | Specify one or more output distributions for the newly built image. |
| imageTemplate | add_tags | Add tags to the image template resource. |
| imageTemplate | depends_on | Add explicit dependencies for the imageTemplate resource. |
| fileCustomizer | name | Name for the file customizer as shown in logs. |
| fileCustomizer | source_uri | Location to download the file. |
| fileCustomizer | destination | Location to save the downloaded file - needs to be under /tmp on Linux or an existing location on Windows. |
| fileCustomizer | checksum | The SHA 256 checksum to validate the file. |
| shellCustomizer | name | The name of the customizer as shown in logs. |
| shellCustomizer | inline_statements | A list of shell commands to run. |
| shellScriptCustomizer | name | The name of the customizer as shown in logs. |
| shellScriptCustomizer | script_uri | A script to download and run. |
| shellScriptCustomizer | checksum | The SHA 256 checksum to validate the file. |
| powerShellCustomizer | name | The name of the customizer as shown in logs. |
| powerShellCustomizer | inline_statements | A list of PowerShell commands to run. |
| powerShellCustomizer | run_as_elevated | Run the commands in an elevated PowerShell session. |
| powerShellCustomizer | run_as_system | Run the commands that need to execute as the System. |
| powerShellCustomizer | valid_exit_codes | A list of exit codes to treat as successful. |
| powerShellScriptCustomizer | name | The name of the customizer as shown in logs. |
| powerShellScriptCustomizer | script_uri | A PowerShell script to download and run. |
| powerShellScriptCustomizer | checksum | The SHA 256 checksum to validate the file. |
| powerShellScriptCustomizer | run_as_elevated | Run the commands in an elevated PowerShell session. |
| powerShellScriptCustomizer | run_as_system | Run the commands that need to execute as the System. |
| powerShellScriptCustomizer | valid_exit_codes | A list of exit codes to treat as successful. |
| windowsRestartCustomizer | restart_command |  Command to execute the restart (optional). The default is 'shutdown /r /f /t 0 /c \"packer restart\"' |
| windowsRestartCustomizer | restart_check_command | Command to check if restart succeeded (optional). |
| windowsRestartCustomizer | restart_timeout | Restart timeout specified as a string of magnitude and unit. For example, 5m (5 minutes) or 2h (2 hours). The default is: 5m. |
| windowsUpdateCustomizer | search_criteria | Optional, defines which type of updates are installed (like Recommended or Important), BrowseOnly=0 and IsInstalled=0 (Recommended) is the default. |
| windowsUpdateCustomizer | filters | Optional, allows you to specify a filter to include or exclude updates. |
| windowsUpdateCustomizer | update_limit | Optional, defines how many updates can be installed, default 1000. |
| managedImageDistributor | image_id | Target image ID of the image to build. The resource group should exist and be accessible. Either 'image_id' or 'image_name' must be set. |
| managedImageDistributor | image_name | Target image name of the image to build in the same resource group. Either 'image_id' or 'image_name' must be set. |
| managedImageDistributor | location | Azure region where the managed image should be created (required). |
| managedImageDistributor | run_output_name | A label for the run, shown in logs and in the portal. |
| managedImageDistributor | add_tags | An optional list of tags that will be added on the image. |
| sharedImageDistributor | gallery_image_id | Target ID for the gallery image to create. It can reference the image itself or a specific version to create. |
| sharedImageDistributor | add_replication_regions | Azure regions where the managed image should be replicated. First in the list should be the location of the image gallery. |
| sharedImageDistributor | exclude_from_latest | Option to ensure the image will not be the "latest" version so it will always be pulled by version number. |
| sharedImageDistributor | run_output_name | A label for the run, shown in logs and in the portal. |
| sharedImageDistributor | add_tags | An optional list of tags that will be added on the gallery image. |
| vhdDistributor | run_output_name | A label for the run, shown in logs and in the portal. |
| vhdDistributor | add_tags | An optional list of tags that will be added on the virtual hard disk. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

imageTemplate {
    name "Ubuntu2004WithJava"
    add_identity msi
    source_platform_image Vm.UbuntuServer_2004LTS

    add_customizers
        [
            shellCustomizer {
                name "install-jdk"

                inline_statements
                    [
                        "set -eux"
                        "sudo apt-get update"
                        "sudo apt-get -y upgrade"
                        "sudo apt-get -y install openjdk-17-jre-headless"
                    ]
            }
        ]

    add_distributors
        [
            sharedImageDistributor {
                gallery_image_id
                    (ResourceType("Microsoft.Compute/galleries/images", "2020-09-30")
                        .resourceId (
                            ResourceName "my-image-gallery",
                            ResourceName "java-server-os"
                        )
                    )
                add_replication_regions [ Location.EastUS ]
                add_tags [
                    "image-type", "java"
                ]
            }
        ]
}
```