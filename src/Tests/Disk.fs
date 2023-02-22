module Disk

open Expecto
open Farmer
open Farmer.Builders
open Newtonsoft.Json.Linq

let tests =
    testList
        "Disk Tests"
        [
            test "Import disk builder from VHD" {
                let deployment =
                    arm {
                        add_resources
                            [
                                disk {
                                    name "imported-disk-image"
                                    sku Vm.DiskType.Premium_LRS
                                    os_type Linux

                                    import
                                        (System.Uri
                                            "https://rxw1n3qxt54dnvfen1gnza5n.blob.core.windows.net/vhds/Ubuntu2004WithJava_20230213141703.vhd")
                                        (ResourceId.create (
                                            Arm.Storage.storageAccounts,
                                            ResourceName "rxw1n3qxt54dnvfen1gnza5n",
                                            "IT_farmer-imgbldr_Ubuntu2004WithJava_aea5facc-e1b5-47de-aa5b-2c6aafe2161d"
                                        ))
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let diskProps =
                    jobj.SelectToken("resources[?(@.name=='imported-disk-image')].properties")

                Expect.isNotNull diskProps "Unable to get disk properties"
                let os = diskProps.SelectToken "osType"
                Expect.equal os (JValue "Linux") "osType incorrect"
                let createOption = diskProps.SelectToken "creationData.createOption"
                Expect.equal createOption (JValue "Import") "createOption incorrect"
                let sourceUri = diskProps.SelectToken "creationData.sourceUri"

                Expect.equal
                    sourceUri
                    (JValue
                        "https://rxw1n3qxt54dnvfen1gnza5n.blob.core.windows.net/vhds/Ubuntu2004WithJava_20230213141703.vhd")
                    "sourceUri incorrect"

                let storageAccountId = diskProps.SelectToken "creationData.storageAccountId"

                Expect.equal
                    storageAccountId
                    (JValue
                        "[resourceId('IT_farmer-imgbldr_Ubuntu2004WithJava_aea5facc-e1b5-47de-aa5b-2c6aafe2161d', 'Microsoft.Storage/storageAccounts', 'rxw1n3qxt54dnvfen1gnza5n')]")
                    "storageAccountId incorrect"

                let diskSku =
                    jobj.SelectToken("resources[?(@.name=='imported-disk-image')].sku.name")

                Expect.equal diskSku (JValue "Premium_LRS") "disk sku incorrect"
            }

            test "Simple empty disk" {
                let deployment =
                    arm {
                        add_resources
                            [
                                disk {
                                    name "empty-disk"
                                    os_type Linux
                                    create_empty 128<Gb>
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let diskProps = jobj.SelectToken("resources[?(@.name=='empty-disk')].properties")
                Expect.isNotNull diskProps "Unable to get disk properties"
                let diskSizeGB = diskProps.SelectToken "diskSizeGB"
                Expect.equal diskSizeGB (JValue 128) "diskSizeGB incorrect"
                let os = diskProps.SelectToken "osType"
                Expect.equal os (JValue "Linux") "osType incorrect"
                let createOption = diskProps.SelectToken "creationData.createOption"
                Expect.equal createOption (JValue "Empty") "createOption incorrect"
            }
        ]
