module ImageTemplate

open Expecto
open Newtonsoft.Json.Linq
open Farmer
open Farmer.Builders
open Farmer.Arm.ImageTemplate

let tests =
    testList
        "Image Template Tests"
        [
            test "Builds basic customized image" {
                let msi = createUserAssignedIdentity "imgbldr"

                let imageBuilder =
                    {
                        Name = ResourceName "Ubuntu2004WithJava"
                        Location = Location.EastUS
                        Identity =
                            {
                                SystemAssigned = Disabled
                                UserAssigned = [ msi.UserAssignedIdentity ]
                            }
                        Tags = Map.empty
                        Dependencies = Set.empty
                        BuildTimeoutInMinutes = None
                        Source =
                            {
                                PlanInfo = None
                                ImageIdentifier =
                                    {
                                        Publisher = "canonical"
                                        Offer = "0001-com-ubuntu-server-focal"
                                        Sku = "20_04-lts-gen2"
                                    }
                                Version = null
                            }
                            |> ImageBuilderSource.Platform
                        Customize =
                            [
                                {
                                    Name = "install-jdk"
                                    Inline =
                                        [
                                            "set -eux"
                                            "sudo apt-get update"
                                            "sudo apt-get -y upgrade"
                                            "sudo apt-get -y install openjdk-17-jre-headless"
                                        ]
                                }
                                |> Customizer.Shell
                            ]
                        Distribute =
                            [
                                {
                                    RunOutputName = "testVhdRun"
                                    ArtifactTags = Map.empty
                                }
                                |> Distibutor.VHD
                            ]
                    }

                let deployment =
                    arm {
                        location Location.EastUS
                        add_resource msi
                        add_resource imageBuilder
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let imageTemplate = jobj.SelectToken "resources[?(@.name == 'Ubuntu2004WithJava')]"
                Expect.isNotNull imageTemplate "imageTemplate missing from deployment"
                Expect.isNotNull (imageTemplate.SelectToken "identity") "imageTemplate.identity not set"

                let source = imageTemplate.SelectToken "properties.source"
                Expect.equal source.["offer"] (JValue "0001-com-ubuntu-server-focal") "Incorrect source.offer"
                Expect.equal source.["publisher"] (JValue "canonical") "Incorrect source.publisher"
                Expect.equal source.["sku"] (JValue "20_04-lts-gen2") "Incorrect source.sku"
                Expect.equal source.["type"] (JValue "PlatformImage") "Incorrect source.type"
                Expect.equal source.["version"] (JValue "latest") "Incorrect source.version"

                let customize = imageTemplate.SelectToken "properties.customize"
                Expect.hasLength customize 1 "customize length incorrect"
                Expect.equal customize.[0].["type"] (JValue "Shell") "customize[0].type incorrect"
                Expect.isNotNull customize.[0].["inline"] "Shell customization missing inline"
                Expect.hasLength customize.[0].["inline"] 4 "Incorrect shell inline values"

                let distribute = imageTemplate.SelectToken "properties.distribute"
                Expect.hasLength distribute 1 "distribute length incorrect"
                Expect.equal distribute.[0].["runOutputName"] (JValue "testVhdRun") "Incorrect distribute.runOutputName"
                Expect.equal distribute.[0].["type"] (JValue "VHD") "Incorrect distribute.type"

            }

            test "Customized image template builder" {
                let msi = createUserAssignedIdentity "imgbldr"

                let imageBuilder =
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
                                shellScriptCustomizer { script_uri "https://whatever.example.com/install.sh" }
                            ]

                        add_distributors
                            [
                                vhdDistributor { run_output_name "testVhdRun" }
                                sharedImageDistributor {
                                    gallery_image_id (
                                        Farmer.Arm.Gallery.galleryImages.resourceId (
                                            ResourceName "my-image-gallery",
                                            ResourceName "java-server-os"
                                        )
                                    )

                                    add_replication_regions [ Location.EastUS ]
                                    add_tags [ "image-type", "java" ]
                                }
                            ]
                    }

                let deployment =
                    arm {
                        location Location.EastUS
                        add_resource msi
                        add_resource imageBuilder
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let imageTemplate = jobj.SelectToken "resources[?(@.name == 'Ubuntu2004WithJava')]"
                Expect.isNotNull imageTemplate "imageTemplate missing from deployment"
                Expect.isNotNull (imageTemplate.SelectToken "identity") "imageTemplate.identity not set"

                let source = imageTemplate.SelectToken "properties.source"
                Expect.equal source.["offer"] (JValue "0001-com-ubuntu-server-focal") "Incorrect source.offer"
                Expect.equal source.["publisher"] (JValue "canonical") "Incorrect source.publisher"
                Expect.equal source.["sku"] (JValue "20_04-lts-gen2") "Incorrect source.sku"
                Expect.equal source.["type"] (JValue "PlatformImage") "Incorrect source.type"
                Expect.equal source.["version"] (JValue "latest") "Incorrect source.version"

                let customize = imageTemplate.SelectToken "properties.customize"
                Expect.hasLength customize 2 "customize length incorrect"
                Expect.equal customize.[0].["type"] (JValue "Shell") "customize[0].type incorrect"
                Expect.isNotNull customize.[0].["inline"] "Shell customization missing inline"
                Expect.hasLength customize.[0].["inline"] 4 "Incorrect shell inline values"
                Expect.equal customize.[1].["type"] (JValue "Shell") "customize[1].type incorrect"

                Expect.equal
                    customize.[1].["scriptUri"]
                    (JValue "https://whatever.example.com/install.sh")
                    "Incorrect shell scriptUri"

                let distribute = imageTemplate.SelectToken "properties.distribute"
                Expect.hasLength distribute 2 "distribute length incorrect"
                Expect.equal distribute.[0].["runOutputName"] (JValue "testVhdRun") "Incorrect distribute.runOutputName"
                Expect.equal distribute.[0].["type"] (JValue "VHD") "Incorrect distribute.[0].type"
                Expect.isNull (distribute.[0].SelectToken "artifactTags") "distrbute.[0] should not have 'artifactTags'"
                Expect.equal distribute.[1].["type"] (JValue "SharedImage") "Incorrect distribute.[1].type"

                Expect.equal
                    (string distribute.[1].["galleryImageId"])
                    ("[resourceId('Microsoft.Compute/galleries/images', 'my-image-gallery', 'java-server-os')]")
                    "Incorrect distribute.[1].galleryImageId"

                Expect.equal
                    distribute.[1].["runOutputName"]
                    (JValue "shared-image-run")
                    "Incorrect 'runOutputName' for shared iamge distributor"

                Expect.equal
                    (distribute.[1].SelectToken "artifactTags.image-type")
                    (JValue "java")
                    "distrbute.[1].artifactTags in correct"
            }
        ]
