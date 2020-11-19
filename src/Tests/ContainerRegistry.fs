module ContainerRegistry

open Expecto
open Farmer
open Farmer.ContainerRegistry
open Farmer.Builders
open Microsoft.Rest.Serialization
open Newtonsoft.Json.Linq
open Farmer.CoreTypes

type RegistryJson =
    { resources :
        {| name : string
           ``type`` : string
           apiVersion : string
           sku : {| name : string |}
           location : string
           properties : JObject
        |} array
    }

let toTemplate loc (d : ContainerRegistryConfig) =
    arm {
        location loc
        add_resource d
    }
    |> Deployment.getTemplate "farmer-resources" 

let fromJson = SafeJsonConvert.DeserializeObject<RegistryJson>
let resource (r : RegistryJson) = r.resources.[0]
let whenWritten deploy = deploy |> toTemplate Location.NorthEurope |> Writer.toJson |> fromJson
// resource assertions
let shouldHaveName name (r : RegistryJson) = Expect.equal name (resource(r).name) "Resource names do not match"; r
let shouldHaveSku (sku : Sku) (r : RegistryJson) = Expect.equal (sku.ToString()) (resource(r).sku.name) "SKUs do not match"; r
let shouldHaveType t (r : RegistryJson) = Expect.equal t (resource(r).``type``) "Types do not match"; r
let shouldHaveApiVersion v (r : RegistryJson) = Expect.equal v (resource(r).apiVersion) "API version is not expected version"; r
let shouldHaveALocation (r : RegistryJson) = Expect.isNotEmpty (resource(r).location) "Location should be set"; r
// property assertions
// admin user
let shouldHaveAdminUserEnabled (r : RegistryJson) =
    let b = resource(r).properties.Value<bool> "adminUserEnabled"
    Expect.isTrue b "adminUserEnabled was expected to be enabled"
    r
let shouldHaveAdminUserDisabled (r : RegistryJson) =
    let b = resource(r).properties.Value<bool> "adminUserEnabled"
    Expect.isFalse b "adminUserEnabled was expected to be disabled"
    r

let tests = testList "Container Registry" [
    test "Basic resource settings are written to template resource" {
        containerRegistry {
            name "test"
            sku Premium
        }
        |> whenWritten
        |> shouldHaveType "Microsoft.ContainerRegistry/registries"
        |> shouldHaveApiVersion "2019-05-01"
        |> shouldHaveName "test"
        |> shouldHaveSku Premium
        |> shouldHaveALocation
        |> shouldHaveAdminUserDisabled
        |> ignore
    }

    test "When enable_admin_user is set it is written to resource properties" {
        containerRegistry {
            name "test"
            enable_admin_user
        }
        |> whenWritten
        |> shouldHaveAdminUserEnabled
        |> ignore
    }
]