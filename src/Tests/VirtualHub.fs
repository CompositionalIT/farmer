module VirtualHub

open Expecto
open Farmer
open Farmer
open Farmer.Arm.VirtualHub
open Farmer.Builders
open Farmer.VirtualHub
open Farmer.VirtualHub.HubRouteTable
open Microsoft.Azure.Management.Network
open Microsoft.Azure.Management.Network.Models
open Microsoft.Rest
open System
open Microsoft.Rest.Serialization

let getResource<'T when 'T :> IArmResource> (data:IArmResource list) = data |> List.choose(function :? 'T as x -> Some x | _ -> None)

let getVirtualHubResource = getResource<Farmer.Arm.VirtualHub.VirtualHub>
let getHubRouteTableResource = getResource<Farmer.Arm.VirtualHub.HubRouteTable>
/// Client instance needed to get the serializer settings.
let dummyClient = new NetworkManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let asAzureResource (virtualHub:VirtualHubConfig) =
    arm { add_resource virtualHub }
    |> findAzureResources<VirtualHub> dummyClient.SerializationSettings
    |> List.head
    |> fun r ->
        r
        
// https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.network.models.hubroutetable?view=azure-dotnet
let hubRouteTableAsAzureResource (routeTable:HubRouteTableConfig)=
    arm { add_resource routeTable }
    |> findAzureResources<Microsoft.Azure.Management.Network.Models.VirtualHubRouteTableV2> dummyClient.SerializationSettings
    |> List.head
    |> fun r ->
        r
let getResourceAtIndex o = o |> getResourceAtIndex dummyClient.SerializationSettings

let getResources (v:IBuilder) = v.BuildResources Location.WestUS

let getResourceDependsOnByName (template:Deployment) (resourceName:ResourceName) =
    let json = template.Template |> Writer.toJson
    let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)
    let dependsOn = jobj.SelectToken($"resources[?(@.name=='{resourceName.Value}')].dependsOn")
    let jarray = dependsOn :?> Newtonsoft.Json.Linq.JArray
    [for jvalue in jarray do jvalue.ToString()]
   
let virtualHubTests = testList "VirtualHub only Tests" [
    test "VirtualHub is correctly created" { 
        let vhub =
            vhub {
                name "my-vhub"                
            }
            |> asAzureResource
        Expect.equal vhub.Name "my-vhub" ""
        Expect.equal vhub.Sku Sku.Standard.ArmValue ""
    }
    test "VirtualHub with address prefix" {
        let expectedAddressPrefix = (IPAddressCidr.parse "10.0.0.0/24")
        let vhub =
            vhub {
                name "my-vhub"
                address_prefix expectedAddressPrefix
            }
            |> asAzureResource
        Expect.equal vhub.AddressPrefix (IPAddressCidr.format expectedAddressPrefix) ""
    }
    test "VirtualHub does not create resources for unmanaged linked resources" {
        let resources =
            vhub {
                name "my-vhub"
                link_to_unmanaged_vwan (Farmer.Arm.VirtualWan.virtualWans.resourceId "my-vwan")
            }
            |> getResources
        Expect.hasLength resources 1 ""
    }
    test "VirtualHub does not create resources for managed linked resources" {
        let resources =
            vhub {
                name "my-vhub"
                link_to_vwan (vwan { name "my-vwan"})
            }
            |> getResources
        Expect.hasLength resources 1 ""
    }
    test "VirtualHub does not create dependencies for unmanaged linked resources" {
        let resource =
            vhub {
                name "my-vhub"
                link_to_unmanaged_vwan (Farmer.Arm.VirtualWan.virtualWans.resourceId "my-vwan")
            }
            |> getResources |> getVirtualHubResource |> List.head
        Expect.isEmpty resource.Dependencies ""
    }
    test "VirtualHub creates dependencies for managed linked resources" {
        let resource =
            vhub {
                name "my-vhub"
                link_to_vwan (vwan { name "my-vwan"})
            }
            |> getResources |> getVirtualHubResource |> List.head
        Expect.containsAll resource.Dependencies [
            ResourceId.create(Farmer.Arm.VirtualWan.virtualWans, ResourceName "my-vwan");]
            ""
    }
    test "VirtualHub creates empty dependsOn in arm template json for unmanaged linked resources" {
        let template =
            arm {
                add_resources [
                    vhub {
                        name "my-vhub"
                        link_to_unmanaged_vwan (Farmer.Arm.VirtualWan.virtualWans.resourceId "my-vwan")
                    }
                ]
            } :> IDeploymentSource
        let dependsOn = getResourceDependsOnByName template.Deployment (ResourceName "my-vhub")    
        Expect.hasLength dependsOn 0 ""
    }
    test "VirtualHub creates dependsOn in arm template json for managed linked resources" {
        let template =
            arm {
                add_resources [
                    vhub {
                        name "my-vhub"
                        link_to_vwan (vwan { name "my-vwan"})
                    }
                ]
            } :> IDeploymentSource
        let dependsOn = getResourceDependsOnByName template.Deployment (ResourceName "my-vhub")
        Expect.hasLength dependsOn 1 ""
        let expectedVwanDependency = "[resourceId('Microsoft.Network/virtualWans', 'my-vwan')]"
        Expect.equal dependsOn.Head expectedVwanDependency "" 
    }
]

let hubRouteTableTests = testList "Hub Route Table Tests" [
    test "HubRouteTable is correctly created" {
        let routeTableResourceName = ResourceName "my-routetable"
        let vhubResourceName = ResourceName "my-vhub"
        let routeTable =
            hubRouteTable {
                name routeTableResourceName
                link_to_vhub (vhub {name vhubResourceName})
            }
            |> hubRouteTableAsAzureResource
        Expect.equal routeTable.Name (vhubResourceName/routeTableResourceName).Value ""
        Expect.isEmpty routeTable.Routes ""
    }
    ptest "HubRouteTable adds routes with same NextHop resourceId" {
        let routeTableResourceName = ResourceName "my-routetable"
        let vhubResourceName = ResourceName "my-vhub"
        let routeTable =
            hubRouteTable {
                name routeTableResourceName
                link_to_vhub (vhub {name vhubResourceName})
                add_routes [
                    { Name = "route1"
                      Destination = Destination.CidrDestination [(IPAddressCidr.parse "10.0.0.0/24")]
                      NextHop = NextHop.ResourceId (LinkedResource.Unmanaged (virtualHubs.resourceId "next-hub"))  }
                ]
            }
            |> hubRouteTableAsAzureResource
        Expect.equal routeTable.Name (vhubResourceName/routeTableResourceName).Value ""
        Expect.isEmpty routeTable.Routes ""
    }
    test "HubRouteTable does not create dependencies for unmanaged linked resources" {
        let routeTableResourceName = "my-routetable"
        let vhubResourceName = "my-vhub"
        let resource =
            hubRouteTable {
                name routeTableResourceName
                link_to_unmanaged_vhub (virtualHubs.resourceId vhubResourceName)
            }
            |> getResources |> getHubRouteTableResource |> List.head
        Expect.isEmpty resource.Dependencies ""
    }
    test "HubRouteTable creates dependencies for managed linked resources" {
        let routeTableResourceName = "my-routetable"
        let vhubResourceName = "my-vhub"
        let resource =
            hubRouteTable {
                name routeTableResourceName
                link_to_vhub (vhub {name vhubResourceName})
            }
            |> getResources |> getHubRouteTableResource |> List.head
        Expect.containsAll resource.Dependencies [
            ResourceId.create(virtualHubs, (ResourceName vhubResourceName));]
            ""
    }
    test "HubRouteTable creates empty dependsOn in arm template json for unmanaged linked resources" {
        let routeTableResourceName = ResourceName "my-routetable"
        let vhubResourceName = ResourceName "my-vhub"
        let template =
            arm {
                add_resources [
                     hubRouteTable {
                        name routeTableResourceName
                        link_to_unmanaged_vhub (virtualHubs.resourceId vhubResourceName)
                    }
                ]
            } :> IDeploymentSource
        let dependsOn = getResourceDependsOnByName template.Deployment (vhubResourceName/routeTableResourceName)    
        Expect.hasLength dependsOn 0 ""
    }
    test "HubRouteTable creates dependsOn in arm template json for managed linked resources" {
        let routeTableResourceName = ResourceName "my-routetable"
        let vhubResourceName = ResourceName "my-vhub"
        let template =
            arm {
                add_resources [
                     hubRouteTable {
                        name routeTableResourceName
                        link_to_vhub (vhub {name vhubResourceName})
                    }
                ]
            } :> IDeploymentSource
        let dependsOn = getResourceDependsOnByName template.Deployment (vhubResourceName/routeTableResourceName)
        Expect.hasLength dependsOn 1 ""
        let expectedDependency = $"[resourceId('Microsoft.Network/virtualHubs', '{vhubResourceName.Value}')]"
        Expect.equal dependsOn.Head expectedDependency "" 
    }
    test "HubRouteTable appends labels" {
        let routeTableResourceName = "my-routetable"
        let vhubResourceName = "my-vhub"
        let expectedLabels = ["label1"; "label2"]
        let resource =
            hubRouteTable {
                name routeTableResourceName
                link_to_unmanaged_vhub (virtualHubs.resourceId vhubResourceName)
                add_labels expectedLabels
            }
            |> getResources |> getHubRouteTableResource |> List.head
        Expect.equal resource.Labels expectedLabels ""
    }
]

let tests = testList "Virtual Hub Tests" [virtualHubTests; hubRouteTableTests]