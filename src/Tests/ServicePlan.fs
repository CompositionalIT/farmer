module ServicePlan

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open System

let getResource<'T when 'T :> IArmResource> (data:IArmResource list) = data |> List.choose(function :? 'T as x -> Some x | _ -> None)
let getResources (v:IBuilder) = v.BuildResources Location.WestEurope

let tests = testList "Service Plan Tests"[
    test "Enable zoneRedundant in service plan" {
       let resources = webApp { name "test"; enable_zone_redundant } |> getResources
       let sf = resources |> getResource<Web.ServerFarm> |> List.head
       
       Expect.equal sf.ZoneRedundant (Some true) "ZoneRedundant should be enabled"
   }
]