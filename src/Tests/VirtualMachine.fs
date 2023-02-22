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
open Newtonsoft.Json.Linq

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

            test "Supports multiple private IP configurations" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"

                                    private_ip_allocation (
                                        PrivateIpAddress.StaticPrivateIp(Net.IPAddress.Parse("192.168.12.13"))
                                    )

                                    add_ip_configurations
                                        [
                                            ipConfig {
                                                private_ip_allocation (
                                                    PrivateIpAddress.StaticPrivateIp(
                                                        Net.IPAddress.Parse("192.168.12.14")
                                                    )
                                                )
                                            }
                                        ]
                                }
                            ]
                    }

                let jobj = Newtonsoft.Json.Linq.JObject.Parse(deployment.Template |> Writer.toJson)
                let nic = jobj.SelectToken("resources[?(@.name=='foo-nic')]")
                Expect.isNotNull nic "VM NIC not found"

                let ip0 =
                    nic.SelectToken "properties.ipConfigurations[0].properties.privateIPAddress"

                Expect.equal (ip0.ToString()) "192.168.12.13" "First static IP is wrong or missing"

                let ip1 =
                    nic.SelectToken "properties.ipConfigurations[1].properties.privateIPAddress"

                Expect.equal (ip1.ToString()) "192.168.12.14" "Second static IP is wrong or missing"

                let ip1SubnetId =
                    nic.SelectToken "properties.ipConfigurations[1].properties.subnet.id"

                Expect.equal
                    (ip1SubnetId.ToString())
                    "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'foo-vnet', 'foo-subnet')]"
                    "Second subnet is wrong or missing"
            }

            test "Supports adding multiple private IP addresses" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    public_ip None

                                    add_ip_configurations
                                        [
                                            ipConfig {
                                                private_ip_allocation (
                                                    PrivateIpAddress.StaticPrivateIp(
                                                        Net.IPAddress.Parse("192.168.12.13")
                                                    )
                                                )
                                            }
                                        ]
                                }
                            ]
                    }

                let jobj = Newtonsoft.Json.Linq.JObject.Parse(deployment.Template |> Writer.toJson)
                let nic = jobj.SelectToken("resources[?(@.name=='foo-nic')]").ToString()
                Expect.isNonEmpty nic "NIC not found"

                let nicProps = jobj.SelectToken("resources[?(@.name=='foo-nic')].properties")

                Expect.isNotNull nicProps "NIC properties not found"

                let ip0Token =
                    nicProps.SelectToken "ipConfigurations[1].properties.privateIPAddress"

                Expect.equal (ip0Token.ToString()) "192.168.12.13" "Static IP is wrong or missing"
            }

            test "Builds multiple NICs when attaching to multiple subnets" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    public_ip None

                                    add_ip_configurations
                                        [
                                            ipConfig {
                                                private_ip_allocation (
                                                    PrivateIpAddress.StaticPrivateIp(
                                                        Net.IPAddress.Parse("192.168.12.13")
                                                    )
                                                )

                                                subnet_name (ResourceName "another-subnet")
                                            }
                                        ]
                                }
                            ]
                    }

                let jobj = Newtonsoft.Json.Linq.JObject.Parse(deployment.Template |> Writer.toJson)

                let vm =
                    jobj.SelectToken("resources[?(@.type=='Microsoft.Compute/virtualMachines')]")

                let vmProps = vm.["properties"]
                let vmNics = vmProps.["networkProfile"].["networkInterfaces"]
                Expect.hasLength vmNics 2 "Emitted VM should have two network interfaces"
                let vmDepends = vm.["dependsOn"]
                Expect.hasLength vmDepends 2 "Emitted VM should have two dependencies"

                let nics =
                    jobj.SelectTokens("resources[?(@.type=='Microsoft.Network/networkInterfaces')]")

                Expect.hasLength nics 2 "Should have emitted two network interfaces"
                let secondNic = jobj.SelectToken("resources[?(@.name=='foo-nic-another-subnet')]")
                Expect.isNotNull secondNic "Second NIC not found"

                let secondNicProps = secondNic["properties"]
                Expect.isNotNull secondNicProps "Second NIC properties not found"

                let secondNicIp =
                    secondNicProps.SelectToken "ipConfigurations[0].properties.privateIPAddress"

                Expect.equal (secondNicIp.ToString()) "192.168.12.13" "Static IP is wrong or missing"
            }

            test "IP forwarding set for first NIC only" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    ip_forwarding Enabled

                                    add_ip_configurations [ ipConfig { subnet_name (ResourceName "another-subnet") } ]
                                }
                            ]
                    }

                let jobj = Newtonsoft.Json.Linq.JObject.Parse(deployment.Template |> Writer.toJson)
                let firstNicProps = jobj.SelectToken("resources[?(@.name=='foo-nic')].properties")

                Expect.equal
                    firstNicProps.["enableIPForwarding"]
                    (JValue true)
                    "First NIC should have IP forwarding enabled"

                let secondNicProps =
                    jobj.SelectToken("resources[?(@.name=='foo-nic-another-subnet')].properties")

                Expect.isNull secondNicProps.["enableIPForwarding"] "Second NIC should not have IP forwarding"
            }

            test "Accelerated networking set for all NICs" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vm {
                                    name "foo"
                                    username "foo"
                                    vm_size Standard_D2s_v5
                                    accelerated_networking Enabled

                                    add_ip_configurations [ ipConfig { subnet_name (ResourceName "another-subnet") } ]
                                }
                            ]
                    }

                let jobj = Newtonsoft.Json.Linq.JObject.Parse(deployment.Template |> Writer.toJson)
                let firstNicProps = jobj.SelectToken("resources[?(@.name=='foo-nic')].properties")

                Expect.equal
                    firstNicProps.["enableAcceleratedNetworking"]
                    (JValue true)
                    "First NIC should have accelerated networking enabled"

                let secondNicProps =
                    jobj.SelectToken("resources[?(@.name=='foo-nic-another-subnet')].properties")

                Expect.equal
                    secondNicProps.["enableAcceleratedNetworking"]
                    (JValue true)
                    "Second NIC should have accelerated networking enabled"
            }

            test "Accelerated networking not allowed on A-series VM" {
                Expect.throws
                    (fun _ ->
                        let _ =
                            arm {
                                add_resources
                                    [
                                        vm {
                                            name "foo"
                                            username "foo"
                                            vm_size Basic_A0
                                            accelerated_networking Enabled

                                            add_ip_configurations
                                                [ ipConfig { subnet_name (ResourceName "another-subnet") } ]
                                        }
                                    ]
                            }

                        ())
                    "Expected failure using accelerated networking with default VM size."
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

            test "Creates zonal VM and public IP" {
                let deployment =
                    arm {
                        location Location.WestUS3

                        add_resources
                            [
                                vm {
                                    name "zonal-vm"
                                    vm_size Standard_B1ms
                                    username "azureuser"
                                    add_availability_zone "2"
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let vmZones = jobj.SelectToken "resources[?(@.name=='zonal-vm')].zones" :?> JArray
                Expect.hasLength vmZones 1 "VM s have a zone assignment."
                Expect.equal (string vmZones.[0]) "2" "VM zone should be '2'"

                let publicIpZone =
                    jobj.SelectToken "resources[?(@.name=='zonal-vm-ip')].zones" :?> JArray

                Expect.hasLength publicIpZone 1 "Public IP should have a zone assignment."
                Expect.equal (string publicIpZone.[0]) "2" "Public IP zone should be '2'"
            }
            test "Creates VM with Ultra disk and zone" {
                let deployment =
                    arm {
                        location Location.WestUS3

                        add_resources
                            [
                                vm {
                                    name "ultra-disk-vm"
                                    vm_size Standard_D2s_v5
                                    username "azureuser"
                                    add_availability_zone "2"
                                    add_disk 4096 UltraSSD_LRS
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let vm = jobj.SelectToken "resources[?(@.name=='ultra-disk-vm')]"
                let vmProps = vm.["properties"]
                let ultraSsdEnabled = vmProps.SelectToken "additionalCapabilities.ultraSSDEnabled"
                Expect.equal ultraSsdEnabled (JValue true) "Ultra SSD capability not enabled on VM"

                let dataDiskType =
                    vmProps.SelectToken "storageProfile.dataDisks[0].managedDisk.storageAccountType"

                Expect.equal dataDiskType (JValue "UltraSSD_LRS") "Data disk not set to Ultra disk type"
            }

            test "Creates VM and attaches newly imported OS disk" {
                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                disk {
                                    name "imported-disk-image"
                                    sku Vm.DiskType.Premium_LRS
                                    os_type Linux

                                    import
                                        (Uri
                                            "https://rxw1n3qxt54dnvfen1gnza5n.blob.core.windows.net/vhds/Ubuntu2004WithJava_20230213141703.vhd")
                                        (ResourceId.create (
                                            Arm.Storage.storageAccounts,
                                            ResourceName "rxw1n3qxt54dnvfen1gnza5n",
                                            "IT_farmer-imgbldr_Ubuntu2004WithJava_aea5facc-e1b5-47de-aa5b-2c6aafe2161d"
                                        ))
                                }
                                vm {
                                    name "attached-os-disk-vm"
                                    vm_size Standard_B1ms
                                    username "azureuser"
                                    attach_os_disk Linux (Arm.Disk.disks.resourceId "imported-disk-image")
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let vm = jobj.SelectToken "resources[?(@.name=='attached-os-disk-vm')]"
                let dependencies = vm.["dependsOn"]

                Expect.contains
                    dependencies
                    (JValue "[resourceId('Microsoft.Compute/disks', 'imported-disk-image')]")
                    "Missing imported-disk-image dependency"

                let vmOsProfile = vm.SelectToken "properties.osProfile"
                Expect.isNull vmOsProfile "The osProfile should not be set when attaching an OS disk"

                let vmOsDisk = vm.SelectToken "properties.storageProfile.osDisk"
                Expect.isNotNull vmOsDisk "VM missing OS disk"
                Expect.equal vmOsDisk.["createOption"] (JValue "Attach") "OS disk createOption incorrect"

                Expect.equal
                    vmOsDisk.["name"]
                    (JValue "imported-disk-image")
                    "OS disk name should match attached disk name"

                Expect.equal vmOsDisk.["osType"] (JValue "Linux") "OS disk osType incorrect"

                Expect.equal
                    (vm.SelectToken "properties.storageProfile.osDisk.managedDisk.id")
                    (JValue "[resourceId('Microsoft.Compute/disks', 'imported-disk-image')]")
                    "Incorrect reference to managed disk"
            }

            test "Creates VM and attaches existing OS disk" {
                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                vm {
                                    name "attached-os-disk-vm"
                                    vm_size Standard_B1ms
                                    username "azureuser"
                                    attach_existing_os_disk Linux (Arm.Disk.disks.resourceId "existing-os-disk")
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let vm = jobj.SelectToken "resources[?(@.name=='attached-os-disk-vm')]"
                Expect.hasLength vm.["dependsOn"] 1 "Should only have dependency for NIC when attaching existing disk"
            }

            test "Creates VM and attaches newly created data disks" {
                let disk0 =
                    disk {
                        name "ultra-disk-0"
                        sku Vm.DiskType.UltraSSD_LRS
                        os_type Linux
                        create_empty 1024<Gb>
                        add_availability_zone "1"
                    }

                let disk1 =
                    disk {
                        name "standard-disk-1"
                        sku Vm.DiskType.Standard_LRS
                        os_type Linux
                        create_empty 1024<Gb>
                        add_availability_zone "1"
                    }

                let deployment =
                    arm {
                        location Location.EastUS

                        add_resources
                            [
                                disk0
                                disk1
                                vm {
                                    name "attached-data-disk-vm"
                                    vm_size Standard_B1ms
                                    operating_system UbuntuServer_2204LTS
                                    username "azureuser"
                                    add_availability_zone "1"
                                    attach_data_disk disk0
                                    attach_data_disk disk1
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let vm = jobj.SelectToken "resources[?(@.name=='attached-data-disk-vm')]"
                let dependencies = vm.["dependsOn"]

                Expect.contains
                    dependencies
                    (JValue "[resourceId('Microsoft.Compute/disks', 'ultra-disk-0')]")
                    "Missing disk-0"

                Expect.contains
                    dependencies
                    (JValue "[resourceId('Microsoft.Compute/disks', 'standard-disk-1')]")
                    "Missing disk-1"

                let dataDisks = vm.SelectToken "properties.storageProfile.dataDisks"
                Expect.hasLength dataDisks 2 "Incorrect number of data disks on VM"

                for disk in dataDisks do
                    Expect.equal disk.["createOption"] (JValue "Attach") "Incorrect createOption"

                let firstDisk = dataDisks.[0]

                Expect.equal
                    (firstDisk.SelectToken "managedDisk.id")
                    (JValue "[resourceId('Microsoft.Compute/disks', 'ultra-disk-0')]")
                    "Incorrect managedDisk.id"

                Expect.equal
                    (firstDisk.SelectToken "name")
                    (JValue "ultra-disk-0")
                    "Ultra disk name should match name from resourceId"

                let secondDisk = dataDisks.[1]

                Expect.equal
                    (secondDisk.SelectToken "managedDisk.id")
                    (JValue "[resourceId('Microsoft.Compute/disks', 'standard-disk-1')]")
                    "Incorrect managedDisk.id"

                Expect.equal
                    (secondDisk.SelectToken "name")
                    (JValue "standard-disk-1")
                    "Standard disk name should match name from resourceId"
            }

        ]
