module ContainerService

open Expecto
open Farmer.Builders
open Farmer
open Microsoft.Azure.Management.Compute.Models
open Microsoft.Azure.Management.ContainerService
open Microsoft.Rest
open System

let dummyClient = new ContainerServiceClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "AKS" [
    test "Basic AKS cluster" {
        let myAks = aks {
            name "k8s-cluster"
            dns_prefix "testaks"
            add_agent_pools [
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
            |> findAzureResources<ContainerService> dummyClient.SerializationSettings
            |> Seq.head
        Expect.equal aks.Name "k8s-cluster" ""
        Expect.equal aks.AgentPoolProfiles.Count 1 ""
        Expect.equal aks.AgentPoolProfiles.[0].Name "linuxpool" ""
        Expect.equal aks.AgentPoolProfiles.[0].Count 3 ""
        Expect.equal aks.AgentPoolProfiles.[0].VmSize "Standard_DS2_v2" ""
        Expect.equal aks.LinuxProfile.AdminUsername "aksuser" ""
        Expect.equal aks.LinuxProfile.Ssh.PublicKeys.Count 1 ""
        Expect.equal aks.LinuxProfile.Ssh.PublicKeys.[0].KeyData "public-key-here" ""
        Expect.equal aks.ServicePrincipalProfile.ClientId "some-spn-client-id" ""
        Expect.equal aks.ServicePrincipalProfile.Secret "[parameters('client-secret-for-k8s-cluster')]" ""
    }
]
