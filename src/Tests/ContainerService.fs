module ContainerService

open Expecto
open Farmer.Arm.ContainerService.AddonProfiles
open Farmer.Arm.RoleAssignment
open Farmer.Builders
open Farmer
open Microsoft.Azure.Management.Compute.Models
open Microsoft.Azure.Management.ContainerService
open Microsoft.Rest
open System
open Microsoft.Azure.Management.ContainerService.Models

let dummyClient =
    new ContainerServiceClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList
        "AKS"
        [
            /// The simplest AKS cluster would be one that uses a system assigned managed identity (MSI),
            /// uses that MSI for accessing other resources, and then takes the defaults for node pool
            /// size (3 nodes) and DNS prefix (generated based on cluster name).
            test "Basic AKS cluster with MSI" {
                let myAks =
                    aks {
                        name "aks-cluster"
                        service_principal_use_msi
                    }

                let template = arm { add_resource myAks }

                let aks =
                    template
                    |> findAzureResources<ManagedCluster> dummyClient.SerializationSettings
                    |> Seq.head

                Expect.equal aks.Name "aks-cluster" ""
                Expect.hasLength aks.AgentPoolProfiles 1 ""
                Expect.equal aks.AgentPoolProfiles.[0].Name "nodepool1" ""
                Expect.equal aks.AgentPoolProfiles.[0].Count (Nullable 3) ""
                let json = template.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let identity =
                    jobj.SelectToken("resources[?(@.name=='aks-cluster')].identity.type") |> string

                Expect.equal identity "SystemAssigned" "Basic cluster using MSI should have a SystemAssigned identity."
            }
            test "Basic AKS cluster with client ID" {
                let myAks =
                    aks {
                        name "aks-cluster"
                        service_principal_client_id "some-spn-client-id"
                    }

                let template = arm { add_resource myAks }

                let aks =
                    template
                    |> findAzureResources<ManagedCluster> dummyClient.SerializationSettings
                    |> Seq.head

                Expect.equal aks.Name "aks-cluster" ""
                Expect.hasLength aks.AgentPoolProfiles 1 ""
                Expect.equal aks.AgentPoolProfiles.[0].Name "nodepool1" ""
                Expect.equal aks.AgentPoolProfiles.[0].Count (Nullable 3) ""
                let json = template.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let identity =
                    jobj.SelectToken("resources[?(@.name=='aks-cluster')].identity.type") |> string

                Expect.equal identity "None" "Basic cluster with client ID should have no identity assigned."
            }
            test "Basic AKS cluster needs SP" {
                Expect.throws
                    (fun _ ->
                        let myAks = aks { name "aks-cluster" }
                        let template = arm { add_resource myAks }
                        template |> Writer.quickWrite "aks-cluster-should-fail")
                    "Error should be raised if there are no service principal settings."
            }
            test "Simple AKS cluster" {
                let myAks =
                    aks {
                        name "k8s-cluster"
                        dns_prefix "testaks"

                        add_agent_pools
                            [
                                agentPool {
                                    name "linuxPool"
                                    count 3
                                }
                            ]

                        linux_profile "aksuser" "public-key-here"
                        service_principal_client_id "some-spn-client-id"
                    }

                let aks =
                    arm { add_resource myAks }
                    |> findAzureResources<ManagedCluster> dummyClient.SerializationSettings
                    |> Seq.head

                Expect.equal aks.Name "k8s-cluster" ""
                Expect.hasLength aks.AgentPoolProfiles 1 ""
                Expect.equal aks.AgentPoolProfiles.[0].Name "linuxpool" ""
                Expect.equal aks.AgentPoolProfiles.[0].Count (Nullable 3) ""
                Expect.equal aks.AgentPoolProfiles.[0].VmSize "Standard_DS2_v2" ""
                Expect.equal aks.LinuxProfile.AdminUsername "aksuser" ""
                Expect.equal aks.LinuxProfile.Ssh.PublicKeys.Count 1 ""
                Expect.equal aks.LinuxProfile.Ssh.PublicKeys.[0].KeyData "public-key-here" ""
                Expect.equal aks.ServicePrincipalProfile.ClientId "some-spn-client-id" ""
                Expect.equal aks.ServicePrincipalProfile.Secret "[parameters('client-secret-for-k8s-cluster')]" ""
            }
            test "AKS cluster using MSI" {
                let myAks =
                    aks {
                        name "k8s-cluster"
                        dns_prefix "testaks"

                        add_agent_pools
                            [
                                agentPool {
                                    name "linuxPool"
                                    count 3
                                }
                            ]

                        service_principal_use_msi
                    }

                let aks =
                    arm { add_resource myAks }
                    |> findAzureResources<ManagedCluster> dummyClient.SerializationSettings
                    |> Seq.head

                Expect.equal
                    aks.ServicePrincipalProfile.ClientId
                    "msi"
                    "ClientId should be 'msi' for service principal."
            }
            test "Calculates network profile DNS server" {
                let netProfile = azureCniNetworkProfile { service_cidr "10.250.0.0/16" }
                let serviceCidr = Expect.wantSome netProfile.ServiceCidr "Service CIDR not set"
                Expect.equal (serviceCidr |> IPAddressCidr.format) "10.250.0.0/16" "Service CIDR set incorrectly."
                Expect.isSome netProfile.DnsServiceIP "DNS service IP should have a value"

                Expect.equal
                    (netProfile.DnsServiceIP.Value.ToString())
                    "10.250.0.2"
                    "DNS service IP should be .2 in service_cidr"
            }
            test "AKS cluster on Private VNet" {
                let myAks =
                    aks {
                        name "private-k8s-cluster"
                        dns_prefix "testprivateaks"

                        add_agent_pools
                            [
                                agentPool {
                                    name "linuxPool"
                                    count 3
                                    vnet "my-vnet"
                                    subnet "containernet"
                                }
                            ]

                        network_profile (azureCniNetworkProfile { service_cidr "10.250.0.0/16" })
                        linux_profile "aksuser" "public-key-here"
                        service_principal_client_id "some-spn-client-id"
                    }

                let aks =
                    arm { add_resource myAks }
                    |> findAzureResources<ManagedCluster> dummyClient.SerializationSettings
                    |> Seq.head

                Expect.hasLength aks.AgentPoolProfiles 1 ""
                Expect.equal aks.AgentPoolProfiles.[0].Name "linuxpool" ""
            }
            test "AKS with private API must use a standard load balancer." {
                Expect.throws
                    (fun () ->
                        let _ =
                            aks {
                                name "k8s-cluster"
                                service_principal_client_id "some-spn-client-id"
                                dns_prefix "testaks"

                                add_agent_pools
                                    [
                                        agentPool {
                                            name "linuxPool"
                                            count 3
                                        }
                                    ]

                                network_profile (kubenetNetworkProfile { load_balancer_sku LoadBalancer.Sku.Basic })
                                enable_private_cluster true
                            }

                        ())
                    "Should throw validation exception when trying to use a private cluster on a basic LB"
            }
            test "AKS API accessible to limited IP range." {
                let myAks =
                    aks {
                        name "k8s-cluster"
                        service_principal_client_id "some-spn-client-id"
                        dns_prefix "testaks"

                        add_agent_pools
                            [
                                agentPool {
                                    name "linuxPool"
                                    count 3
                                }
                            ]

                        add_api_server_authorized_ip_ranges [ "88.77.66.0/24" ]
                    }

                let template = arm { add_resource myAks }
                let json = template.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let authIpRanges =
                    jobj.SelectToken(
                        "resources[?(@.name=='k8s-cluster')].properties.apiServerAccessProfile.authorizedIPRanges"
                    )

                Expect.hasLength authIpRanges 1 ""

                Expect.equal
                    (authIpRanges.[0].ToString())
                    "88.77.66.0/24"
                    "Got incorrect value for authorized IP ranges."
            }
            test "AKS with MSI and Kubelet identity" {
                let kubeletMsi = createUserAssignedIdentity "kubeletIdentity"
                let clusterMsi = createUserAssignedIdentity "clusterIdentity"

                let assignMsiRoleNameExpr =
                    ArmExpression.create (
                        $"guid(concat(resourceGroup().id, '{clusterMsi.ResourceId.Name.Value}', '{Roles.ManagedIdentityOperator.Id}'))"
                    )

                let assignMsiRole =
                    {
                        Name = assignMsiRoleNameExpr.Eval() |> ResourceName
                        RoleDefinitionId = Roles.ManagedIdentityOperator
                        PrincipalId = clusterMsi.PrincipalId
                        PrincipalType = PrincipalType.ServicePrincipal
                        Scope = ResourceGroup
                        Dependencies = Set [ clusterMsi.ResourceId ]
                    }

                let myAcr = containerRegistry { name "farmercontainerregistry1234" }
                let myAcrResId = (myAcr :> IBuilder).ResourceId

                let acrPullRoleNameExpr =
                    ArmExpression.create (
                        $"guid(concat(resourceGroup().id, '{kubeletMsi.ResourceId.Name.Value}', '{Roles.AcrPull.Id}'))"
                    )

                let acrPullRole =
                    {
                        Name = acrPullRoleNameExpr.Eval() |> ResourceName
                        RoleDefinitionId = Roles.AcrPull
                        PrincipalId = kubeletMsi.PrincipalId
                        PrincipalType = PrincipalType.ServicePrincipal
                        Scope = AssignmentScope.SpecificResource myAcrResId
                        Dependencies = Set [ kubeletMsi.ResourceId ]
                    }

                let myAks =
                    aks {
                        name "aks-cluster"
                        dns_prefix "aks-cluster-223d2976"
                        add_identity clusterMsi
                        service_principal_use_msi
                        kubelet_identity kubeletMsi
                        depends_on clusterMsi
                        depends_on myAcr
                        depends_on_expression assignMsiRoleNameExpr
                        depends_on_expression acrPullRoleNameExpr
                    }

                let template =
                    arm {
                        location Location.EastUS
                        add_resource kubeletMsi
                        add_resource clusterMsi
                        add_resource myAcr
                        add_resource myAks
                        add_resource assignMsiRole
                        add_resource acrPullRole
                    }

                let json = template.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let identity =
                    jobj.SelectToken("resources[?(@.name=='aks-cluster')].identity.type") |> string

                Expect.equal identity "UserAssigned" "Should have a UserAssigned identity."

                let kubeletIdentityClientId =
                    jobj.SelectToken(
                        "resources[?(@.name=='aks-cluster')].properties.identityProfile.kubeletIdentity.clientId"
                    )
                    |> string

                Expect.equal
                    kubeletIdentityClientId
                    "[reference(resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'kubeletIdentity'), '2018-11-30').clientId]"
                    "Incorrect kubelet identity reference."
            }
            test "Basic AKS cluster with addons" {
                let myAppGateway = appGateway { name "app-gw" }
                let appGatewayMsi = createUserAssignedIdentity "app-gw-msi"

                let myAks =
                    aks {
                        name "aks-cluster"
                        service_principal_use_msi

                        addons
                            [
                                AciConnectorLinux Enabled
                                HttpApplicationRouting Enabled
                                KubeDashboard Enabled
                                IngressApplicationGateway
                                    {
                                        Status = Enabled
                                        ApplicationGatewayId = (myAppGateway :> IBuilder).ResourceId
                                        Identity = Some appGatewayMsi.UserAssignedIdentity
                                    }
                            ]
                    }

                let template = arm { add_resource myAks }
                let json = template.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let expectedAciConn =
                    """{
  "enabled": true
}"""

                let aciConnector =
                    jobj.SelectToken("resources[?(@.name=='aks-cluster')].properties.addonProfiles.aciConnectorLinux")
                    |> string

                Expect.equal aciConnector expectedAciConn "Unexpected value for addonProfiles.aciConnectorLinux."

                let expectedHttpAppRouting =
                    """{
  "enabled": true
}"""

                let httpAppRouting =
                    jobj.SelectToken(
                        "resources[?(@.name=='aks-cluster')].properties.addonProfiles.httpApplicationRouting"
                    )
                    |> string

                Expect.equal
                    httpAppRouting
                    expectedHttpAppRouting
                    "Unexpected value for addonProfiles.httpApplicationRouting."

                let expectedAppGateway =
                    """{
  "config": {
    "applicationGatewayId": "[resourceId('Microsoft.Network/applicationGateways', 'app-gw')]"
  },
  "enabled": true,
  "identity": {
    "clientId": "[reference(resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'app-gw-msi')).clientId]",
    "objectId": "[reference(resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'app-gw-msi')).principalId]",
    "resourceId": "[resourceId('Microsoft.Network/applicationGateways', 'app-gw')]"
  }
}"""

                let appGatewayIngress =
                    jobj.SelectToken(
                        "resources[?(@.name=='aks-cluster')].properties.addonProfiles.ingressApplicationGateway"
                    )
                    |> string

                Expect.equal
                    appGatewayIngress
                    expectedAppGateway
                    "Unexpected value for addonProfiles.ingressApplicationGateway."
            }
        ]
