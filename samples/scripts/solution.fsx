#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"
open Farmer
open Farmer.Builders
let logs = logAnalytics { name "isaacsuperlogs" }
let mysolution = solution {
     name "LogicAppsManagement(tryw)"
     product "OMSGallery/LogicAppsManagement"
     publisher "Microsoft"
     workspace logs
     depends_on logs
   }
let deployment = arm {
    location Location.WestEurope
    add_resource mysolution
}

deployment
|> Writer.quickWrite "solution"

deployment
|> Deploy.execute "Mysolution" []

