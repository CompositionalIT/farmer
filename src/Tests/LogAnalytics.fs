module LogAnalytics
open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open Farmer.LogAnalytics

let tests = testList "Log analytics" [
    test "create log analytics" {
        let b = logAnalytics { name "myFarmer"  
                               sku  PerGB2018
                               retentionInDays 30
                               publicNetworkAccessForQuery
                               publicNetworkAccessForIngestion
                              } :> IBuilder
        let resources = b.BuildResources Location.WestEurope
        let t = resources.[0] :?> WorkSpace
        Expect.equal t.Location Location.WestEurope "Incorrect Location"
        Expect.equal t.Name (ResourceName "myFarmer") "Incorrect name"     
        Expect.equal t.publicNetworkAccessForIngestion (Some "Enabled") "Incorrect publicNetworkAccessForIngestiont"
        Expect.equal t.publicNetworkAccessForQuery (Some "Enabled") "Incorrect publicNetworkAccessForQuery"
        Expect.equal t.retentionInDays (Some 30) "Incorrect retentionInDays"
    }
    test "can't create log analytics  with Sku eqaul to Standalone,PerNode or PerGB2018 and retentionInDays is not bettwen 30 and 730 " {
    let c = logAnalytics { name "myFarmer"
                           sku PerGB2018 
                           retentionInDays 29
                         } :> IBuilder
    Expect.throws (fun _ -> (c.BuildResources Location.WestEurope |> ignore)) "" 

    let d = logAnalytics { name "myFarmer"
                           sku PerNode 
                           retentionInDays 29
                         } :> IBuilder
    Expect.throws (fun _ -> (d.BuildResources Location.WestEurope |> ignore)) "" 
    let a = logAnalytics { name "myFarmer"
                           sku Standalone 
                           retentionInDays 29
                         } :> IBuilder
    Expect.throws (fun _ -> (a.BuildResources Location.WestEurope |> ignore)) "" 
    }        
    test "can't create log analytics  with Sku eqaul to Standard and retentionInDays doesn't eqaul to 30 " {
    let e = logAnalytics { name "myFarmer"
                           sku Standard
                           retentionInDays 29
                         } :> IBuilder
    Expect.throws (fun _ -> (e.BuildResources Location.WestEurope |> ignore)) ""
    }        
    test "can't create log analytics  with Sku eqaul to Premium and retentionInDays doesn't eqaul to 365 " {
    let f = logAnalytics { name "myFarmer"
                           sku Premium
                           retentionInDays 300 
                         } :> IBuilder
    Expect.throws (fun _ -> (f.BuildResources Location.WestEurope |> ignore)) "" 
    }        
     ]
