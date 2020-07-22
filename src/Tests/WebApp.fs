module WebApp

open Expecto
open Farmer
open Farmer.Builders
open Farmer.WebApp
open Farmer.Arm
open System

let getResource<'T when 'T :> IArmResource> (data:IArmResource list) = data |> List.choose(function :? 'T as x -> Some x | _ -> None)
let tests = testList "Web App Tests" [
    let getResources (wa:WebAppConfig) = (wa :> IBuilder).BuildResources Location.WestEurope
    test "Basic Web App has service plan and AI dependencies set" {
        let resources = webApp { name "test" } |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head

        Expect.containsAll wa.Dependencies [ ResourceName "test-ai"; ResourceName "test-farm" ] "Missing dependencies"
        Expect.hasLength (resources |> getResource<Insights.Components>) 1 "Should be one AI component"
        Expect.hasLength (resources |> getResource<Web.ServerFarm>) 1 "Should be one server farm"
    }
    test "Web App allows renaming of service plan and AI" {
        let resources = webApp { name "test"; service_plan_name "supersp"; app_insights_name "superai" } |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head

        Expect.containsAll wa.Dependencies [ ResourceName "supersp"; ResourceName "superai" ] "Missing dependencies"
        Expect.hasLength (resources |> getResource<Insights.Components>) 1 "Should be one AI component"
        Expect.hasLength (resources |> getResource<Web.ServerFarm>) 1 "Should be one server farm"
    }
    test "Web App creates dependencies but no resources with linked AI and Server Farm configs" {
        let sp = servicePlan { name "plan" }
        let ai = appInsights { name "ai" }
        let resources = webApp { name "test"; link_to_app_insights ai; link_to_service_plan sp } |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head
        Expect.containsAll wa.Dependencies [ ResourceName "plan"; ResourceName "ai" ] "Missing dependencies"
        Expect.isEmpty (resources |> getResource<Insights.Components>) "Should be no AI component"
        Expect.isEmpty (resources |> getResource<Web.ServerFarm>) "Should be no server farm"
    }
    test "Web App does not create dependencies for unmanaged linked resources" {
        let resources = webApp { name "test"; link_to_unmanaged_app_insights (ResourceName "test"); link_to_unmanaged_service_plan (ResourceName "test2") } |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head
        Expect.isEmpty wa.Dependencies "Should be no dependencies"
        Expect.isEmpty (resources |> getResource<Insights.Components>) "Should be no AI component"
        Expect.isEmpty (resources |> getResource<Web.ServerFarm>) "Should be no server farm"
    }
]