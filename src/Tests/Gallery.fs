module Gallery

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
    ]