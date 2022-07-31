module LoadBalancer

open Expecto
open Microsoft.Azure.Management.Network
open Microsoft.Rest
open System
open Farmer
open Farmer.LoadBalancer
open Farmer.Builders

let client = new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "Load Balancers" [
    test "Empty basic load balancer" {
        let lb =
            loadBalancer {
                name "lb"
            }
        let resource =
            arm { add_resource lb }
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.LoadBalancer> client.SerializationSettings
                |> List.head
        Expect.equal resource.Name "lb" "Name did not match"
        Expect.equal resource.Sku.Name "Basic" "Incorrect sku"
    }

    test "Empty standard load balancer" {
        let lb =
            loadBalancer {
                name "lb"
                sku Sku.Standard
            }
        let resource =
            arm { add_resource lb }
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.LoadBalancer> client.SerializationSettings
                |> List.head
        Expect.equal resource.Name "lb" "Name did not match"
        Expect.equal resource.Sku.Name "Standard" "Incorrect sku"
    }

    test "Empty standard load balancer with dependency" {
        let lb =
            loadBalancer {
                name "lb"
                add_dependencies [
                    Farmer.Arm.Network.virtualNetworks.resourceId "existing-vnet"
                ]
            }
        let deployment = arm { add_resource lb }
        let json = deployment.Template |> Writer.toJson
        let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)
        let expectedLoadBalancerDeps = "[resourceId('Microsoft.Network/virtualNetworks', 'existing-vnet')]"
        let dependsOn = jobj.SelectToken("resources[?(@.name=='lb')].dependsOn")
        Expect.hasLength dependsOn 1 "load balancer has wrong number of dependencies"
        let actualLbDeps =
            (dependsOn :?> Newtonsoft.Json.Linq.JArray).First.ToString()
        Expect.equal actualLbDeps expectedLoadBalancerDeps "External dependency didn't match"
    }

    test "Load balancer with public ip generates public IP with dependency and matching sku" {
        let lb =
            loadBalancer {
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
        let expectedLoadBalancerDeps = "[resourceId('Microsoft.Network/publicIPAddresses', 'lb-pip')]"
        let dependsOn = jobj.SelectToken("resources[?(@.name=='lb')].dependsOn")
        Expect.hasLength dependsOn 1 "load balancer has wrong number of dependencies"
        let actualLbDeps =
            (dependsOn :?> Newtonsoft.Json.Linq.JArray).First.ToString()
        Expect.equal actualLbDeps expectedLoadBalancerDeps "Public IP dependencies didn't match"
        let resource =
            deployment
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.PublicIPAddress> client.SerializationSettings
                |> List.tryFind (fun r -> r.Name = "lb-pip")
        let pip = Expect.wantSome resource "Unable to find generated IP address in resources"
        Expect.equal pip.Name "lb-pip" "Incorrect name for generated public IP address"
        Expect.equal pip.Sku.Name "Standard" "Incorrect sku for generated public IP address"
    }

    let completeLoadBalancer () =
        loadBalancer {
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
                    link_to_vnet "my-vnet"
                    add_ip_addresses [
                        "10.0.1.4"
                        "10.0.1.5"
                    ]
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
            add_dependencies [
                Farmer.Arm.Network.virtualNetworks.resourceId "my-vnet"
            ]
        }

    test "Complete load balancer" {
        let found =
            arm { add_resource (completeLoadBalancer ()) }
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.LoadBalancer> client.SerializationSettings
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
        let backendResourceId = "[resourceId('Microsoft.Network/loadBalancers/backendAddressPools', 'lb', 'lb-backend')]"
        Expect.equal rule.BackendAddressPool.Id backendResourceId "Incorrect backend address pool for rule"
    }

    test "Complete load balancer backend pool" {
        let found =
            arm { add_resource (completeLoadBalancer ()) }
                |> findAzureResources<Microsoft.Azure.Management.Network.Models.BackendAddressPool> client.SerializationSettings
                |> List.tryFind (fun r -> r.Name = "lb/lb-backend")
        let resource = Expect.wantSome found "No 'lb/lb-backend' resource found in template."
        Expect.equal resource.Name "lb/lb-backend" "Incorrect name for backend address pool"
    }

    test "Backend pool for existing vnet" {
        let myVnet = vnet {
            name "my-vnet"
        }
        let backendPool = backendAddressPool {
            name "backend-services"
            load_balancer "existing-lb"
            link_to_vnet myVnet
            add_ip_addresses [
                "10.0.1.4"
                "10.0.1.5"
                "10.0.1.6"
            ]
        }
        let template = arm {
            add_resource backendPool
        }
        let pool = template.Template.Resources |> Seq.head :?> Farmer.Arm.LoadBalancer.BackendAddressPool
        Expect.equal pool.LoadBalancer (ResourceName "existing-lb") "Pool had incorrect load balancer"
        let expectedVnet = Unmanaged (Farmer.Arm.Network.virtualNetworks.resourceId (ResourceName "my-vnet"))
        Expect.hasLength pool.LoadBalancerBackendAddresses 3 "Pool should have 3 addresses"
        pool.LoadBalancerBackendAddresses |> List.iter (fun addr ->
            Expect.equal addr.VirtualNetwork (Some expectedVnet) "Pool did not have expected vnet"
        )
    }

    test "Setting backend pool on VM NIC" {
        let vm1 =
            vm {
                name "webserver1"
                vm_size Vm.Standard_B1ms
                operating_system Vm.UbuntuServer_2004LTS
                public_ip None
                username "webserver"
                diagnostics_support_managed
                link_to_vnet "my-vnet"
                subnet_name "my-webservers"
                link_to_backend_address_pool (Farmer.Arm.LoadBalancer.loadBalancerBackendAddressPools.resourceId "lb/lb-backend")
            }
        let template = arm {
            add_resource vm1
        }
        match template.Template.Resources with
        | [resource1; resource2] ->
            let _ = resource1 :?> Farmer.Arm.Compute.VirtualMachine
            let nic = resource2 :?> Farmer.Arm.Network.NetworkInterface
            Expect.equal (Farmer.Arm.LoadBalancer.loadBalancerBackendAddressPools.resourceId "lb/lb-backend") nic.IpConfigs.[0].LoadBalancerBackendAddressPools.[0].ResourceId "Backend ID didn't match"
        | _ -> failwith "Only expecting two resources in the template."
    }
]
