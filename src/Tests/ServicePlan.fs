module ServicePlan

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open System

let getResource<'T when 'T :> IArmResource> (data:IArmResource list) = data |> List.choose(function :? 'T as x -> Some x | _ -> None)
let getResources (v:IBuilder) = v.BuildResources Location.WestEurope

let tests = testList "Service Plan Tests" [
    test "Basic service plan does not have zone redundancy" {
        let servicePlan = servicePlan { name "test" } 
        let sf = servicePlan |> getResources |> getResource<Web.ServerFarm> |> List.head

        let template = arm{ add_resource servicePlan}
        let jobj = template.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse

        let zoneRedundant = 
            jobj.SelectToken($"$..resources[?(@.type=='Microsoft.Web/serverfarms')].properties.zoneRedundant")
       
        Expect.equal sf.ZoneRedundant None "ZoneRedundant should not be set"
        Expect.isNull zoneRedundant "Template should not include zone redundancy information"
    }

    test "Enable zoneRedundant in service plan" {
        let servicePlan = servicePlan { name "test"; zone_redundant Enabled } 
        let sf = servicePlan |> getResources |> getResource<Web.ServerFarm> |> List.head

        let template = arm{ add_resource servicePlan}
        let jobj = template.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse

        let zoneRedundant = 
            jobj.SelectToken($"$..resources[?(@.type=='Microsoft.Web/serverfarms')].properties.zoneRedundant")
       
        Expect.equal sf.ZoneRedundant (Some Enabled) "ZoneRedundant should be enabled"
        Expect.isNotNull zoneRedundant "Template should include zone redundancy information"
        Expect.equal (zoneRedundant.ToString().ToLower()) "true" "ZoneRedundant should be set to true"
    }

    test "Disable zoneRedundant in service plan" {
        let servicePlan = servicePlan { name "test"; zone_redundant Disabled } 
        let sf = servicePlan |> getResources |> getResource<Web.ServerFarm> |> List.head

        let template = arm{ add_resource servicePlan}
        let jobj = template.Template |> Writer.toJson |> Newtonsoft.Json.Linq.JObject.Parse

        let zoneRedundant = 
            jobj.SelectToken($"$..resources[?(@.type=='Microsoft.Web/serverfarms')].properties.zoneRedundant")
       
        Expect.equal sf.ZoneRedundant (Some Disabled) "ZoneRedundant should be disabled"
        Expect.isNotNull zoneRedundant "Template should include zone redundancy information"
        Expect.equal (zoneRedundant.ToString().ToLower()) "false" "ZoneRedundant should be set to false"
    }
]