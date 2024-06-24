module LoadBalancer

open Expecto
open Microsoft.Azure.Management.Network
open Microsoft.Rest
open System
open Farmer
open Farmer.LoadBalancer
open Farmer.Builders
open Newtonsoft.Json.Linq

let client =
    new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList "Load Balancers" [
        test "Empty basic load balancer" {
            let lb = loadBalancer { name "lb" }

            let resource =
                arm { add_resource lb }
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.LoadBalancer>
                    client.SerializationSettings
                |> List.head

            Expect.equal resource.Name "lb" "Name did not match"
            Expect.equal resource.Sku.Name "Basic" "Incorrect sku"
        }

        test "Empty standard load balancer" {
            let lb = loadBalancer {
                name "lb"
                sku Sku.Standard
            }

            let resource =
                arm { add_resource lb }
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.LoadBalancer>
                    client.SerializationSettings
                |> List.head

            Expect.equal resource.Name "lb" "Name did not match"
            Expect.equal resource.Sku.Name "Standard" "Incorrect sku"
        }

        test "Empty standard load balancer with dependency" {
            let lb = loadBalancer {
                name "lb"
                add_dependencies [ Farmer.Arm.Network.virtualNetworks.resourceId "existing-vnet" ]
            }

            let deployment = arm { add_resource lb }
            let json = deployment.Template |> Writer.toJson
            let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

            let expectedLoadBalancerDeps =
                "[resourceId('Microsoft.Network/virtualNetworks', 'existing-vnet')]"

            let dependsOn = jobj.SelectToken("resources[?(@.name=='lb')].dependsOn")
            Expect.hasLength dependsOn 1 "load balancer has wrong number of dependencies"
            let actualLbDeps = (dependsOn :?> Newtonsoft.Json.Linq.JArray).First.ToString()
            Expect.equal actualLbDeps expectedLoadBalancerDeps "External dependency didn't match"
        }

        test "Load balancer with public ip generates public IP with dependency and matching sku" {
            let lb = loadBalancer {
                name "lb"
                sku Sku.Standard

                add_frontends [
                    frontend {
                        name "lb-frontend"
                        public_ip "lb-pip"
                    }
                ]
            }

            let deployment = arm { add_resource lb }
            let json = deployment.Template |> Writer.toJson
            let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

            let expectedLoadBalancerDeps =
                "[resourceId('Microsoft.Network/publicIPAddresses', 'lb-pip')]"

            let dependsOn = jobj.SelectToken("resources[?(@.name=='lb')].dependsOn")
            Expect.hasLength dependsOn 1 "load balancer has wrong number of dependencies"
            let actualLbDeps = (dependsOn :?> Newtonsoft.Json.Linq.JArray).First.ToString()
            Expect.equal actualLbDeps expectedLoadBalancerDeps "Public IP dependencies didn't match"

            let resource =
                deployment
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.PublicIPAddress>
                    client.SerializationSettings
                |> List.tryFind (fun r -> r.Name = "lb-pip")

            let pip =
                Expect.wantSome resource "Unable to find generated IP address in resources"

            Expect.equal pip.Name "lb-pip" "Incorrect name for generated public IP address"
            Expect.equal pip.Sku.Name "Standard" "Incorrect sku for generated public IP address"
        }

        let completeLoadBalancer () = loadBalancer {
            name "lb"
            sku Sku.Standard

            add_frontends [
                frontend {
                    name "lb-frontend"
                    public_ip "lb-pip"
                }
            ]

            add_backend_pools [
                backendAddressPool {
                    name "lb-backend"
                    link_to_subnet "my-subnet"
                    link_to_vnet "my-vnet"
                    add_ip_addresses [ "10.0.1.4"; "10.0.1.5" ]
                }
            ]

            add_probes [
                loadBalancerProbe {
                    name "httpGet"
                    protocol LoadBalancerProbeProtocol.HTTP
                    port 8080
                    request_path "/"
                }
            ]

            add_rules [
                loadBalancingRule {
                    name "rule1"
                    frontend_ip_config "lb-frontend"
                    backend_address_pool "lb-backend"
                    frontend_port 80
                    backend_port 8080
                    protocol TransmissionProtocol.TCP
                    probe "httpGet"
                }
            ]

            add_dependencies [ Farmer.Arm.Network.virtualNetworks.resourceId "my-vnet" ]
        }

        test "Complete load balancer" {
            let found =
                arm { add_resource (completeLoadBalancer ()) }
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.LoadBalancer>
                    client.SerializationSettings
                |> List.tryFind (fun r -> r.Name = "lb")

            let resource = Expect.wantSome found "No 'lb' resource found in template."
            Expect.hasLength resource.BackendAddressPools 1 "Incorrect number of backend address pools"

            Expect.equal resource.BackendAddressPools.[0].Name "lb-backend" "Incorrect name for backend address pool"

            Expect.hasLength resource.Probes 1 "Incorrect number of probes"
            let probe = resource.Probes |> Seq.head
            Expect.equal probe.Name "httpGet" "Incorrect name for httpGet probe"
            Expect.equal probe.Protocol "Http" "Incorrect protocol for httpGet probe"
            Expect.equal probe.Protocol "Http" "Incorrect protocol for httpGet probe"
            Expect.equal probe.RequestPath "/" "Incorrect request path for httpGet probe"
            Expect.equal probe.Port 8080 "Incorrect port for httpGet probe"
            Expect.equal probe.IntervalInSeconds (Nullable 15) "Incorrect interval for httpGet probe"
            Expect.equal probe.NumberOfProbes (Nullable 2) "Incorrect number of probes for httpGet probe"
            Expect.hasLength resource.LoadBalancingRules 1 "Incorrect number of load balancing rules"
            let rule = resource.LoadBalancingRules |> Seq.head
            Expect.equal rule.Name "rule1" "Incorrect name for rule"
            Expect.equal rule.FrontendPort 80 "Incorrect frontend port for rule"
            Expect.equal rule.BackendPort (Nullable 8080) "Incorrect backend port for rule"

            let backendResourceId =
                "[resourceId('Microsoft.Network/loadBalancers/backendAddressPools', 'lb', 'lb-backend')]"

            Expect.equal rule.BackendAddressPool.Id backendResourceId "Incorrect backend address pool for rule"
        }

        test "Complete load balancer backend pool" {
            let found =
                arm { add_resource (completeLoadBalancer ()) }
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.BackendAddressPool>
                    client.SerializationSettings
                |> List.tryFind (fun r -> r.Name = "lb/lb-backend")

            let resource =
                Expect.wantSome found "No 'lb/lb-backend' resource found in template."

            Expect.equal resource.Name "lb/lb-backend" "Incorrect name for backend address pool"
        }

        test "Backend pool for existing subnet" {

            let myVnet = vnet {
                name "my-vnet"
                add_address_spaces [ "10.0.1.0/24" ]

                add_subnets [
                    subnet {
                        name "my-subnet"
                        prefix "10.0.1.0/24"
                    }
                ]
            }

            let backendPool = backendAddressPool {
                name "backend-services"
                load_balancer "existing-lb"
                add_ip_addresses [ "10.0.1.4"; "10.0.1.5"; "10.0.1.6" ]
                link_to_vnet myVnet
                link_to_subnet myVnet.Subnets[0]
            }

            let template = arm { add_resource backendPool }

            let pool =
                template.Template.Resources |> Seq.head :?> Farmer.Arm.LoadBalancer.BackendAddressPool

            Expect.equal pool.LoadBalancer (ResourceName "existing-lb") "Pool had incorrect load balancer"

            let expectedSubnet =
                Unmanaged(Farmer.Arm.Network.subnets.resourceId (ResourceName "my-subnet"))

            Expect.hasLength pool.LoadBalancerBackendAddresses 3 "Pool should have 3 addresses"

            pool.LoadBalancerBackendAddresses
            |> List.iter (fun addr ->
                Expect.equal addr.Subnet (Some expectedSubnet) "Pool did not have expected subnet")
        }

        test "Setting backend pool on VM NIC" {
            let vm1 = vm {
                name "webserver1"
                vm_size Vm.Standard_B1ms
                operating_system Vm.UbuntuServer_2004LTS
                public_ip None
                username "webserver"
                diagnostics_support_managed
                link_to_vnet "my-vnet"
                subnet_name "my-webservers"

                link_to_backend_address_pool (
                    Farmer.Arm.LoadBalancer.loadBalancerBackendAddressPools.resourceId "lb/lb-backend"
                )
            }

            let template = arm { add_resource vm1 }

            match template.Template.Resources with
            | [ resource1; resource2 ] ->
                let _ = resource1 :?> Farmer.Arm.Compute.VirtualMachine
                let nic = resource2 :?> Farmer.Arm.Network.NetworkInterface

                Expect.equal
                    (Farmer.Arm.LoadBalancer.loadBalancerBackendAddressPools.resourceId "lb/lb-backend")
                    nic.IpConfigs.[0].LoadBalancerBackendAddressPools.[0].ResourceId
                    "Backend ID didn't match"
            | _ -> failwith "Only expecting two resources in the template."
        }

        test "Load balancer frontend with 'ip_v6' creates IPv6 Public IP Address" {
            let lb = loadBalancer {
                name "lb"
                sku Sku.Standard

                add_frontends [
                    frontend {
                        name "lb-frontend"
                        public_ip "lb-pip"
                        ip_v6
                    }
                ]
            }

            let deployment = arm { add_resource lb }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let frontendPublicIpAddressVersion =
                jobj.SelectToken("resources[?(@.name=='lb-pip')].properties.publicIPAddressVersion")

            Expect.equal (frontendPublicIpAddressVersion.ToString()) "IPv6" "Public IP not generated as IPv6"
        }
    ]