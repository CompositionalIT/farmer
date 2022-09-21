module VirtualMachine

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Vm
open Microsoft.Azure.Management.Compute
open Microsoft.Azure.Management.Compute.Models
open Microsoft.Rest
open System
open Microsoft.Azure.Management.WebSites.Models

/// Client instance needed to get the serializer settings.
let client =
    new ComputeManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList
        "Virtual Machine"
        [
            test "Can create a basic virtual machine" {
                let resource =
                    let myVm =
                        vm {
                            name "isaacsVM"
                            username "isaac"
                            vm_size Standard_A2
                            operating_system WindowsServer_2012Datacenter
                            os_disk 128 StandardSSD_LRS
                            add_ssd_disk 128
                            add_slow_disk 512
                            diagnostics_support
                        }

                    arm { add_resource myVm }
                    |> findAzureResources<VirtualMachine> client.SerializationSettings
                    |> List.find (fun r -> r.StorageProfile |> isNull |> not)

                resource.Validate()

                Expect.equal resource.StorageProfile.OsDisk.DiskSizeGB (Nullable 128) "Incorrect OS disk size"

                Expect.equal
                    resource.StorageProfile.ImageReference.Offer
                    WindowsServer_2012Datacenter.Offer.ArmValue
                    "Incorrect Offer"

                Expect.equal resource.StorageProfile.DataDisks.Count 2 "Incorrect number of data disks"
                Expect.equal resource.OsProfile.AdminUsername "isaac" "Incorrect username"

                Expect.equal
                    resource.NetworkProfile.NetworkInterfaces.[0].Id
                    "[resourceId('Microsoft.Network/networkInterfaces', 'isaacsVM-nic')]"
                    "Incorrect NIC reference"

                Expect.isTrue
                    (resource.DiagnosticsProfile.BootDiagnostics.Enabled.GetValueOrDefault false)
                    "Boot Diagnostics should be enabled"
            }

            test "By default, VM does not include Priority" {
                let template =
                    let myVm =
                        vm {
                            name "myvm"
                            username "me"
                        }

                    arm { add_resource myVm }

                let jobj = Newtonsoft.Json.Linq.JObject.Parse(template.Template |> Writer.toJson)

                let vmProperties =
                    jobj.SelectToken("resources[?(@.name=='myvm')].properties") :?> Newtonsoft.Json.Linq.JObject

                Expect.isNull (vmProperties.Property "priority") "Priority should not be set by default"
            }
            test "Can create a basic virtual machine with managed boot diagnostics" {
                let resource =
                    let myVm =
                        vm {
                            name "bootdiagvm"
                            username "farmeruser"
                            vm_size Standard_A2
                            operating_system UbuntuServer_1804LTS
                            diagnostics_support_managed
                        }

                    arm { add_resource myVm }
                    |> findAzureResources<VirtualMachine> client.SerializationSettings
                    |> List.head

                resource.Validate()

                Expect.isTrue
                    (resource.DiagnosticsProfile.BootDiagnostics.Enabled.GetValueOrDefault false)
                    "Boot Diagnostics should be enabled"

                Expect.isTrue
                    (isNull resource.DiagnosticsProfile.BootDiagnostics.StorageUri)
                    "Storage should be null for managed boot diagnotics"
            }
            test "Can create a basic virtual machine with no data disk" {
                let resource =
                    let myVm =
                        vm {
                            name "nodatadiskvm"
                            username "farmeruser"
                            vm_size Standard_A2
                            no_data_disk
                            operating_system UbuntuServer_1804LTS
                            diagnostics_support_managed
                        }

                    arm { add_resource myVm }
                    |> findAzureResources<VirtualMachine> client.SerializationSettings
                    |> List.head

                resource.Validate()
                Expect.hasLength resource.StorageProfile.DataDisks 0 "Should have no data disks"
            }
            test "Creates a parameter for the password" {
                let deployment =
                    arm {
                        add_resource (
                            vm {
                                name "isaac"
                                username "foo"
                            }
                        )
                    }

                let template = deployment.Template |> Writer.TemplateGeneration.processTemplate
                Expect.isTrue (template.parameters.ContainsKey "password-for-isaac") "Missing parameter"
                Expect.equal template.parameters.Count 1 "Should only be one parameter"
            }
            test "Throws an error if you upload script files but no script" {
                let createVm () =
                    arm {
                        add_resource (
                            vm {
                                name "foo"
                                username "foo"
                                custom_script_files [ "http://test.fsx" ]
                            }
                        )
                    }
                    |> ignore

                Expect.throws createVm "No script was supplied"
            }
            test "Does not throws an error if you provide a script" {
                arm {
                    add_resource (
                        vm {
                            name "foo"
                            username "foo"
                            custom_script "foo"
                            custom_script_files [ "http://test.fsx" ]
                        }
                    )
                }
                |> ignore

                arm {
                    add_resource (
                        vm {
                            name "foo"
                            username "foo"
                            custom_script "foo"
                        }
                    )
                }
                |> ignore
            }

            test "CustomData is correctly encoded" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    custom_data "foo"
                                }
                            ]
                    }

                let json = deployment.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let customData =
                    jobj.SelectToken("resources[?(@.name=='foo')].properties.osProfile.customData")

                let actualCustomData = (customData.ToString())
                let expectedCustomData = "Zm9v"
                Expect.equal actualCustomData expectedCustomData "customData was not correctly encoded"
            }

            test "Can remove public Ip" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    public_ip None
                                }
                            ]
                    }

                let json = deployment.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let publicIps =
                    jobj.SelectTokens("resources[?(@.type=='Microsoft.Network/publicIPAddresses')]")

                Expect.isEmpty publicIps "No public IP should be created"

                let nicDependsOn =
                    jobj.SelectTokens("resources[?(@.type=='Microsoft.Network/networkInterfaces')].dependsOn.[*]")
                    |> Seq.map (fun x -> x.ToString())

                Expect.isEmpty
                    (nicDependsOn
                     |> Seq.filter (fun x -> x.Contains("Microsoft.Network/publicIPAddresses")))
                    "Network Interface should not depende on any public IP"

                let nicPublicIp =
                    jobj.SelectTokens(
                        "resources[?(@.type=='Microsoft.Network/networkInterfaces')].properties.publicIpAddress"
                    )

                Expect.isEmpty (nicPublicIp) "Network Interface should not link to any public IP"
            }

            test "Can create static Ip" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    ip_allocation PublicIpAddress.Static
                                }
                            ]
                    }

                let json = deployment.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let publicIpProps =
                    jobj.SelectTokens("resources[?(@.type=='Microsoft.Network/publicIPAddresses')].properties")

                Expect.isNonEmpty publicIpProps "IP settings not found"

                let ipToken = publicIpProps |> Seq.head

                let expectedToken =
                    Newtonsoft.Json.Linq.JToken.Parse("{\"publicIPAllocationMethod\": \"Static\"}")

                Expect.equal (ipToken.ToString()) (expectedToken.ToString()) "Static IP was not found"

            }

            test "Disabled password auth" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    disable_password_authentication true
                                    add_authorized_key "fooPath" "fooKey"
                                }
                            ]
                    }

                let json = deployment.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let linuxConfig =
                    jobj.SelectToken("resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration")

                let passwordAuthentication =
                    jobj
                        .SelectToken(
                            "resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration.disablePasswordAuthentication"
                        )
                        .ToString()

                Expect.equal passwordAuthentication "True" "password authentication was not correctly added"
            }

            test "Public key and path added" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    add_authorized_key "fooPath" "fooKey"
                                }
                            ]
                    }

                let json = deployment.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let linuxConfig =
                    jobj.SelectToken("resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration")

                let keyData =
                    jobj
                        .SelectToken(
                            "resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration.ssh.publicKeys[0].keyData"
                        )
                        .ToString()

                let path =
                    jobj
                        .SelectToken(
                            "resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration.ssh.publicKeys[0].path"
                        )
                        .ToString()

                Expect.equal keyData "fooKey" "public keys were not correctly added"
                Expect.equal path "fooPath" "path was not correctly added"
            }

            test "Public keys and paths added" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    add_authorized_keys [ ("fooPath", "fooKey"); ("fooPath1", "fooKey1") ]
                                }
                            ]
                    }

                let json = deployment.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let linuxConfig =
                    jobj.SelectToken("resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration")

                let keyData =
                    jobj
                        .SelectToken(
                            "resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration.ssh.publicKeys[0].keyData"
                        )
                        .ToString()

                let path =
                    jobj
                        .SelectToken(
                            "resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration.ssh.publicKeys[0].path"
                        )
                        .ToString()

                Expect.equal keyData "fooKey" "public keys were not correctly added"
                Expect.equal path "fooPath" "path was not correctly added"

                let keyData =
                    jobj
                        .SelectToken(
                            "resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration.ssh.publicKeys[1].keyData"
                        )
                        .ToString()

                let path =
                    jobj
                        .SelectToken(
                            "resources[?(@.name=='foo')].properties.osProfile.linuxConfiguration.ssh.publicKeys[1].path"
                        )
                        .ToString()

                Expect.equal keyData "fooKey1" "public keys were not correctly added"
                Expect.equal path "fooPath1" "path was not correctly added"
            }

            test "Handles identity correctly" {
                let machine =
                    arm {
                        add_resource (
                            vm {
                                name ""
                                username "isaac"
                            }
                        )
                    }
                    |> findAzureResources<VirtualMachine> client.SerializationSettings
                    |> Seq.head

                Expect.isNull machine.Identity "Default managed identity should be null"

                let machine =
                    arm {
                        add_resource (
                            vm {
                                system_identity
                                username "isaac"
                            }
                        )
                    }
                    |> findAzureResources<VirtualMachine> client.SerializationSettings
                    |> Seq.head

                Expect.equal
                    machine.Identity.Type
                    (Nullable ResourceIdentityType.SystemAssigned)
                    "Should have system identity"

                Expect.isNull machine.Identity.UserAssignedIdentities "Should have no user assigned identities"

                let machine =
                    arm {
                        add_resource (
                            vm {
                                system_identity
                                add_identity (createUserAssignedIdentity "test")
                                add_identity (createUserAssignedIdentity "test2")
                                username "isaac"
                            }
                        )
                    }
                    |> findAzureResources<VirtualMachine> client.SerializationSettings
                    |> Seq.head

                Expect.equal
                    machine.Identity.Type
                    (Nullable ResourceIdentityType.SystemAssignedUserAssigned)
                    "Should have system identity"

                Expect.sequenceEqual
                    (machine.Identity.UserAssignedIdentities |> Seq.map (fun r -> r.Key))
                    [
                        "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test2')]"
                        "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test')]"
                    ]
                    "Should have two user assigned identities"
            }

            test "PrivateIpAllocation set correctly" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"

                                    private_ip_allocation (
                                        PrivateIpAddress.StaticPrivateIp(Net.IPAddress((int64 0x2414188f)))
                                    )
                                }
                            ]
                    }

                let json = deployment.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let privateIpProps = jobj.SelectToken("resources[?(@.name=='foo-nic')]").ToString()
                Expect.isNonEmpty privateIpProps "IP settings not found"

                let methodToken =
                    jobj.SelectToken(
                        "resources[?(@.name=='foo-nic')].properties.ipConfigurations[0].properties.privateIPAllocationMethod"
                    )

                let expectedMethodToken = "Static"
                Expect.equal (methodToken.ToString()) (expectedMethodToken) "Allocation Method is wrong or missing"

                let ipToken =
                    jobj
                        .SelectToken(
                            "resources[?(@.name=='foo-nic')].properties.ipConfigurations[0].properties.privateIPAddress"
                        )
                        .ToString()

                let expectedIpToken = "143.24.20.36"
                Expect.equal (ipToken.ToString()) (expectedIpToken) "Static IP is wrong or missing"
            }

            test "Can attach to NSG" {
                let vmName = "fooVm"
                let myNsg = nsg { name "testNsg" }

                let myVm =
                    vm {
                        name vmName
                        username "foo"
                        network_security_group myNsg
                    }

                let deployment = arm { add_resources [ myNsg; myVm ] }
                let json = deployment.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse json

                let vmNsgId =
                    jobj
                        .SelectToken($"resources[?(@.name=='{vmName}-nic')].properties.networkSecurityGroup.id")
                        .ToString()

                Expect.isFalse (String.IsNullOrEmpty vmNsgId) "NSG not attached"
            }

            test "Link new VM to existing vnet" {
                let template =
                    let myVm =
                        vm {
                            name "myvm"
                            username "azureuser"
                            link_to_unmanaged_vnet "myvnet"
                            subnet_name "default"
                        }

                    arm { add_resource myVm }

                let jobj = Newtonsoft.Json.Linq.JObject.Parse(template.Template |> Writer.toJson)
                let vmResource = jobj.SelectToken("resources[?(@.name=='myvm')]")
                let vmDependsOn = (vmResource.["dependsOn"] :?> Newtonsoft.Json.Linq.JArray)
                Expect.hasLength vmDependsOn 1 "Incorrect number of VM dependencies"

                Expect.sequenceEqual
                    vmDependsOn
                    (Newtonsoft.Json.Linq.JArray [ "[resourceId('Microsoft.Network/networkInterfaces', 'myvm-nic')]" ])
                    $"VM should only depend on its NIC, not also the vnet: {vmDependsOn}"

                let nicResource = jobj.SelectToken("resources[?(@.name=='myvm-nic')]")
                let nicDependsOn = (nicResource.["dependsOn"] :?> Newtonsoft.Json.Linq.JArray)
                Expect.hasLength nicDependsOn 1 "NIC should only have 1 dependency - the public IP"

                Expect.sequenceEqual
                    nicDependsOn
                    (Newtonsoft.Json.Linq.JArray [ "[resourceId('Microsoft.Network/publicIPAddresses', 'myvm-ip')]" ])
                    $"NIC should only depend on its public IP, not also the vnet: {nicDependsOn}"
            }

            test "Link new VM to existing vnet in different resource group" {
                let myVnet =
                    Arm.Network.virtualNetworks.resourceId ("myvnet", groupName = "other-group")

                let template =
                    let myVm =
                        vm {
                            name "myvm"
                            username "azureuser"
                            link_to_unmanaged_vnet myVnet
                            subnet_name "default"
                        }

                    arm { add_resource myVm }

                let jobj = Newtonsoft.Json.Linq.JObject.Parse(template.Template |> Writer.toJson)
                let vmResource = jobj.SelectToken("resources[?(@.name=='myvm')]")
                let vmDependsOn = (vmResource.["dependsOn"] :?> Newtonsoft.Json.Linq.JArray)
                Expect.hasLength vmDependsOn 1 "Incorrect number of VM dependencies"

                Expect.sequenceEqual
                    vmDependsOn
                    (Newtonsoft.Json.Linq.JArray [ "[resourceId('Microsoft.Network/networkInterfaces', 'myvm-nic')]" ])
                    $"VM should only depend on its NIC, not also the vnet: {vmDependsOn}"

                let nicResource = jobj.SelectToken("resources[?(@.name=='myvm-nic')]")
                let nicDependsOn = (nicResource.["dependsOn"] :?> Newtonsoft.Json.Linq.JArray)
                Expect.hasLength nicDependsOn 1 "NIC should only have 1 dependency - the public IP"

                let nicSubnetId =
                    nicResource
                        .SelectToken("properties.ipConfigurations[0].properties.subnet.id")
                        .ToString()

                Expect.equal
                    nicSubnetId
                    "[resourceId('other-group', 'Microsoft.Network/virtualNetworks/subnets', 'myvnet', 'default')]"
                    "NIC subnet should repect resource group specified in VM VNet"

                Expect.sequenceEqual
                    nicDependsOn
                    (Newtonsoft.Json.Linq.JArray [ "[resourceId('Microsoft.Network/publicIPAddresses', 'myvm-ip')]" ])
                    $"NIC should only depend on its public IP, not also the vnet: {nicDependsOn}"
            }

            test "Enables Azure AD SSH access on Linux virtual machine" {
                let template =
                    let myVm =
                        vm {
                            name "myvm"
                            username "ubuntu"
                            vm_size Standard_B1s
                            operating_system UbuntuServer_1804LTS
                            system_identity
                            aad_ssh_login Enabled
                        }

                    arm { add_resource myVm }

                let jobj = Newtonsoft.Json.Linq.JObject.Parse(template.Template |> Writer.toJson)

                let extensionResource =
                    jobj.SelectToken("resources[?(@.name=='myvm/AADSSHLoginForLinux')]")

                Expect.sequenceEqual
                    (extensionResource.["dependsOn"] :?> Newtonsoft.Json.Linq.JArray)
                    (Newtonsoft.Json.Linq.JArray [ "[resourceId('Microsoft.Compute/virtualMachines', 'myvm')]" ])
                    $"Missing or incorrect extension dependency."

                Expect.equal
                    (string extensionResource.["properties"].["type"])
                    "AADSSHLoginForLinux"
                    $"Missing or incorrect extension type."

                Expect.equal
                    (string extensionResource.["properties"].["typeHandlerVersion"])
                    "1.0"
                    $"Missing or incorrect extension typeHandlerVersion."
            }

            test "throws an error if you set priority more than once" {
                let createVm () =
                    arm {
                        add_resource (
                            vm {
                                name "foo"
                                username "foo"
                                priority Regular
                                priority Regular
                            }
                        )
                    }
                    |> ignore

                Expect.throws createVm "priority set more than once"
            }

            test "throws an error if you set spot_instance more than once" {
                let createVm () =
                    arm {
                        add_resource (
                            vm {
                                name "foo"
                                username "foo"
                                spot_instance Deallocate
                                spot_instance Deallocate
                            }
                        )
                    }
                    |> ignore

                Expect.throws createVm "spot_instance set more than once"
            }

            test "throws an error if you set priority and spot_instance" {
                let createVm () =
                    arm {
                        add_resource (
                            vm {
                                name "foo"
                                username "foo"
                                priority Regular
                                spot_instance Deallocate
                            }
                        )
                    }
                    |> ignore

                Expect.throws createVm "priority and spot_instance both set"
            }

        ]
