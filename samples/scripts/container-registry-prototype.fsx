// this can be enabled  by going to Settings > F# > Fsi Extra Parameters > and add "--langversion:preview" to the FSharp.fsiExtraParameters list
#r "nuget: farmer"
#r "nuget: Newtonsoft.Json"

open Farmer

let quickContainerRegistry name sku enableAdmin =
    { new IBuilder with
        member _.BuildResources location _ = [
            NewResource
                { new IArmResource with
                    member _.ResourceName = name
                    member _.ToArmObject() =
                        {| name = this.Name.Value
                           ``type`` = "Microsoft.ContainerRegistry/registries"
                           apiVersion = "2019-05-01"
                           sku = {| name = this.Sku |}
                           location = this.Location.ArmValue
                           tags = {||}
                           properties = {| adminUserEnabled = this.AdminUserEnabled |}
                        |} :> _
                }
        ]
    }

let deployment = arm {
    location NorthEurope
    add_resource (quickContainerRegistry "TestRegistry" "Basic" true)
}

deployment
|> Deploy.whatIf "FarmerTest" Deploy.NoParameters
|> printfn "%A"