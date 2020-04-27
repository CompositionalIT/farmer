module ContainerRegisterTests

open Farmer
open Farmer.Resources
open Microsoft.Rest.Serialization
open Newtonsoft.Json.Linq
open Xunit

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
let shouldHaveName name (r : RegistryJson) = Assert.Equal(name, resource(r).name); r
let shouldHaveSku (sku : ContainerRegistrySku) (r : RegistryJson) = Assert.Equal(sku.ToString(), resource(r).sku.name); r
let shouldHaveType t (r : RegistryJson) = Assert.Equal(t, resource(r).``type``); r
let shouldHaveApiVersion v (r : RegistryJson) = Assert.Equal(v, resource(r).apiVersion); r
let shouldHaveALocation (r : RegistryJson) = Assert.NotEmpty(resource(r).location); r
let shouldHaveAdminUserEnabled (r : RegistryJson) =
    let b = resource(r).properties.Value<bool> "adminUserEnabled"
    Assert.True(b)
    r
                            
[<Fact>]
let ``Basic resource settings are written to template resource``() =
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
    
[<Fact>]
let ``When enable_admin_user is set it is written to resource properties``() =
    containerRegistry {
        name "test"
        enable_admin_user
    }
    |> whenWritten
    |> shouldHaveAdminUserEnabled
    