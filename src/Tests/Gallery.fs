module Gallery

open System
open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.Gallery
open Newtonsoft.Json.Linq

let tests =
    testList "Image Gallery" [
        test "Builds basic image gallery" {
            let deployment = arm {
                location Location.EastUS

                add_resources [
                    gallery {
                        name "mygallery"
                        description "Example Image Gallery"
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
            let gallery = jobj.SelectToken "resources[?(@.name=='mygallery')]"
            Expect.isNotNull gallery "Gallery is not included in deployment template"
            let galleryDesc = gallery.SelectToken "properties.description"
            Expect.equal galleryDesc (JValue "Example Image Gallery") "incorrect description"
        }
        test "Build community image gallery" {
            let deployment = arm {
                location Location.EastUS

                add_resources [
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
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
            let gallery = jobj.SelectToken "resources[?(@.name=='mygallery')]"
            let galleryPermissions = gallery.SelectToken "properties.sharingProfile.permissions"
            Expect.equal galleryPermissions (JValue "Community") "incorrect permissions on community gallery"

            let galleryDesc =
                gallery.SelectToken "properties.sharingProfile.communityGalleryInfo.publicNamePrefix"

            Expect.equal galleryDesc (JValue "farmages") "incorrect communityGalleryInfo.publicNamePrefix"
        }

        test "Create basic image" {
            let deployment = arm {
                add_resources [
                    galleryImage {
                        name "javaserver"
                        gallery_name "mygallery"

                        gallery_image_identifier (
                            {
                                GalleryImageIdentifier.Offer = "ubuntu-java"
                                Publisher = "farmages"
                                Sku = "ubuntu-20-java-17"
                            }
                        )

                        hyperv_generation Image.HyperVGeneration.V2
                        os_state Image.OsState.Generalized
                        os_type OS.Linux
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
            let image = jobj.SelectToken "resources[?(@.name=='mygallery/javaserver')]"
            Expect.isNotNull image "Image not found by gallery/image name"
            Expect.isEmpty image.["dependsOn"] "Image should have no dependencies"
            let imageProps = image.["properties"]
            Expect.equal imageProps.["hyperVGeneration"] (JValue "V2") "Incorrect Hyper-V generation"
            Expect.equal imageProps.["osState"] (JValue "Generalized") "Incorrect OS state"
            Expect.equal imageProps.["osType"] (JValue "Linux") "Incorrect OS type"
            let identifier = imageProps.["identifier"]
            Expect.isNotNull identifier "Image properties missing 'identifier'"
            Expect.equal identifier.["offer"] (JValue "ubuntu-java") "Incorrect identifier.offer"
            Expect.equal identifier.["publisher"] (JValue "farmages") "Incorrect identifier.publisher"
            Expect.equal identifier.["sku"] (JValue "ubuntu-20-java-17") "Incorrect identifier.sku"
            let recommended = imageProps.["recommended"]
            Expect.isNotNull recommended "properties.recommended is missing"

            Expect.equal
                (recommended.SelectToken "memory.max")
                (JValue 32)
                "properties.recommended.memory.max incorrect"

            Expect.equal (recommended.SelectToken "vCPUs.max") (JValue 16) "properties.recommended.vCPUs.max incorrect"
        }

        test "Create gallery and image" {
            let myGallery = gallery {
                name "mygallery"
                description "Example Private Gallery"
            }

            let myGalleryImage = galleryImage {
                name "ubuntu-java-17-server"
                gallery myGallery

                gallery_image_identifier (
                    {
                        GalleryImageIdentifier.Offer = "ubuntu-java"
                        Publisher = "farmages"
                        Sku = "ubuntu-20-java-17"
                    }
                )

                hyperv_generation Image.HyperVGeneration.V2
                os_state Image.OsState.Generalized
                os_type OS.Linux
            }

            let deployment = arm {
                location Location.EastUS
                add_resources [ myGallery; myGalleryImage ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

            let image =
                jobj.SelectToken "resources[?(@.name=='mygallery/ubuntu-java-17-server')]"

            Expect.isNotNull image "Image not found by gallery/image name"
            Expect.hasLength image.["dependsOn"] 1 "Image should have 1 dependency"

            Expect.equal
                image.["dependsOn"].[0]
                (JValue "[resourceId('Microsoft.Compute/galleries', 'mygallery')]")
                "Image should depend on gallery"
        }
        test "Create gallery, application, and version" {
            let myGallery = gallery {
                name "mygallery"
                description "Example Private Gallery"
            }

            let myGalleryApp = galleryApp {
                name "java-17"
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

            let deployment = arm {
                location Location.EastUS
                add_resources [ myGallery; myGalleryApp; myGalleryAppVersion ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

            let app = jobj.SelectToken "resources[?(@.name=='mygallery/java-17')]"

            Expect.isNotNull app "Gallery App not found by gallery/galleryApplication name"
            Expect.hasLength app.["dependsOn"] 1 "Gallery App should have 1 dependency"

            Expect.equal
                app.["dependsOn"].[0]
                (JValue "[resourceId('Microsoft.Compute/galleries', 'mygallery')]")
                "Gallery App should depend on gallery"

            let appVersion = jobj.SelectToken "resources[?(@.name=='mygallery/java-17/1.0.1')]"

            Expect.isNotNull appVersion "Gallery App Version not found by gallery/galleryApplication/version name"
            Expect.hasLength appVersion.["dependsOn"] 2 "Gallery App Version should have 2 dependencies"

            Expect.contains
                appVersion.["dependsOn"]
                (JValue "[resourceId('Microsoft.Compute/galleries/applications', 'mygallery', 'java-17')]")
                "Gallery App Version should depend on gallery app"

            Expect.contains
                appVersion.["dependsOn"]
                (JValue "[resourceId('Microsoft.Compute/galleries', 'mygallery')]")
                "Gallery App Version should depend on gallery"

        }
        test "Gallery app version with ARM Expression for source media link" {
            let myGallery = gallery {
                name "mygallery"
                description "Example Private Gallery"
            }

            let myGalleryApp = galleryApp {
                name "myapp"
                gallery myGallery
                os_type OS.Linux
            }

            let sasUrl =
                "concat('https://', 'mystorageaccount', '/sas-url')" |> ArmExpression.create

            let myGalleryAppVersion = galleryAppVersion {
                name "1.0.1"
                gallery_app myGalleryApp
                gallery myGallery
                install_action "install.sh"
                remove_action "remove.sh"
                source_media_link sasUrl
            }

            let deployment = arm {
                location Location.EastUS
                add_resources [ myGallery; myGalleryApp; myGalleryAppVersion ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

            let appVersion = jobj.SelectToken "resources[?(@.name=='mygallery/myapp/1.0.1')]"

            Expect.equal
                (appVersion.SelectToken("properties.publishingProfile.source.mediaLink")
                 |> string)
                "[concat('https://', 'mystorageaccount', '/sas-url')]"
                "ARM expression incorrect for Gallery App Version source media link"
        }
    ]