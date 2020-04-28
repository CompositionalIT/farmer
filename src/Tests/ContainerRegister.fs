module ContainerRegister

open Farmer
open Farmer.Resources
open Microsoft.Rest.Serialization
open Newtonsoft.Json.Linq
open Expecto

type RegistryJson =
    {|
      resources :
        {|
            name : string
            ``type`` : string
            apiVersion : string
            sku : {| name : string |}
            location : string
            properties : JObject
        |} array
    |}

    
let toTemplate loc (d : ContainerRegistryConfig) =
    let a = arm {
        location loc
        add_resource d
    }
    a.Template
let fromJson resource =
    let o = SafeJsonConvert.DeserializeObject<RegistryJson>(resource)
    o
let resource (r : RegistryJson) = r.resources.[0]
let whenWritten deploy = deploy |> toTemplate NorthEurope |> Writer.toJson |> fromJson
// resource assertions
let shouldHaveName name (r : RegistryJson) = Expect.equal name (resource(r).name) "Resource names do not match"; r
let shouldHaveSku (sku : ContainerRegistrySku) (r : RegistryJson) = Expect.equal (sku.ToString()) (resource(r).sku.name) "SKUs do not match"; r
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
                        
let tests = testList "Container Register tests" [
    test "Basic resource settings are written to template resource" {
        containerRegistry {
            name "test"
            sku ContainerRegistrySku.Premium
        }
        |> whenWritten
        |> shouldHaveType "Microsoft.ContainerRegistry/registries"
        |> shouldHaveApiVersion "2019-05-01"
        |> shouldHaveName "test"
        |> shouldHaveSku ContainerRegistrySku.Premium
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
    