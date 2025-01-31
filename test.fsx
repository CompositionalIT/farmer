#r "./src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"
//#r "nuget:Farmer"

open System
open System.IO
open Farmer
open Farmer.Arm.ContainerService
open Farmer.Builders
open Farmer.ContainerService

type AksDeploymentRequestV1 =
    { ManagementResourceGroupName: string
      TenantMsi: UserAssignedIdentityConfig
      PodSubnet: ResourceId
      NodeSubnet: ResourceId }

let aksResourceV1 (req: AksDeploymentRequestV1) =
    aks {
        name $"{req.ManagementResourceGroupName}-aks"
        tier Tier.Standard
        service_principal_use_msi
        add_identity req.TenantMsi
        kubelet_identity req.TenantMsi
        enable_workload_identity
        enable_image_cleaner
        enable_private_cluster
        dns_prefix "aks"
        add_agent_pools
            [ agentPool {
                  name $"{req.ManagementResourceGroupName}-aks-systempool"
                  count 2
                  disk_size 128<Gb>
                  add_availability_zones [ "1"; "2"; "3" ]
                  vm_size (Vm.CustomImage "Standard_D2s_v3")
                  link_to_subnet req.NodeSubnet
                  link_to_pod_subnet req.PodSubnet
              }
              agentPool {
                  name $"{req.ManagementResourceGroupName}-aks-userpool"
                  user_mode
                  disk_size 128<Gb>
                  add_availability_zones [ "1"; "2"; "3" ]
                  enable_autoscale
                  autoscale_min_count 2
                  autoscale_max_count 4
                  vm_size (Vm.CustomImage "Standard_D4s_v3")
                  link_to_subnet req.NodeSubnet
                  link_to_pod_subnet req.PodSubnet
              } ]
    }

let aksDeploymentV1 (req: AksDeploymentRequestV1) (deps: ResourceId list) =
    let aksDeployment = aksResourceV1 req
    let aksDeployment =
        { aksDeployment with
            Dependencies = Set [ req.TenantMsi.ResourceId ] }
    resourceGroup {
        location (Location "[resourceGroup().location]")
        depends_on deps
        add_resources [ aksDeployment; req.TenantMsi ]
        name $"{req.ManagementResourceGroupName}"
        deployment_name $"{req.ManagementResourceGroupName}-aks-nested"
    }

let msi = userAssignedIdentity { name "aks-user" }
let aksDeploy = 
    { ManagementResourceGroupName = "tnt160-mgmt-p01-eastus2"
      TenantMsi = msi
      PodSubnet = Arm.Network.subnets.resourceId (ResourceName "tnt160-mgmt-p01-eastus2", ResourceName "aksPod" )
      NodeSubnet = Arm.Network.subnets.resourceId (ResourceName "tnt160-mgmt-p01-eastus2", ResourceName "aksNode" ) }

arm {
    location Location.EastUS
    add_resources [ 
        msi
        aksDeploymentV1 aksDeploy []
    ]
}
|> Writer.quickWrite "aks-on-vnet"