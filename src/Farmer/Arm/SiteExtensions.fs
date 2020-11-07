[<AutoOpen>]
module Farmer.Arm.SiteExtensions

open Farmer
open Farmer.CoreTypes

// https://docs.microsoft.com/en-us/azure/templates/microsoft.web/2020-06-01/sites/siteextensions
let siteExtensions =
    ResourceType("Microsoft.Web/sites/siteextensions", "2020-06-01")

type SiteExtension =
    { SiteName  : ResourceName
      Name      : ResourceName
      Location  : Location }
    interface IArmResource with
        member this.ResourceName = sprintf "%s/%s" this.SiteName.Value this.Name.Value |> ResourceName

        member this.JsonModel =
            {|
                siteExtensions.Create(this.Name, this.Location) with
                    dependsOn = [
                        sprintf "resourceId('Microsoft.Web/sites', '%s')" this.SiteName.Value
                    ]
            |} :> _


//
//  What we're trying to generate in JsonModel. Note that we've short-circuited the `parameters()` calls
//
(*
    {
        "type": "Microsoft.Web/sites/siteextensions",
        "apiVersion": "2018-11-01",
        "name": "[concat(parameters('sites_myapp_name'), '/Microsoft.AspNetCore.AzureAppServices.SiteExtension')]",
        "location": "West Europe",
        "dependsOn": [
            "[resourceId('Microsoft.Web/sites', parameters('sites_myapp_name'))]"
        ]
    }

*)
