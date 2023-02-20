module Gallery

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.Gallery
open Newtonsoft.Json.Linq

let tests =
    testList
        "Image Gallery"
        [
            test "Builds basic image gallery" {
                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
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
                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                gallery {
                                    name "mygallery"
                                    description "Example Community Image Gallery"

                                    sharing_profile (
                                        Community
                                            {
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
        ]
