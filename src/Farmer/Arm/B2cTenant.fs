[<AutoOpen>]
module Farmer.Arm.B2cTenant

open Farmer

let b2cTenant =
    ResourceType("Microsoft.AzureActiveDirectory/b2cDirectories", "2021-04-01")

type B2cDomainName =
    | B2cDomainName of string

    static member internal Empty = B2cDomainName ""

    member this.AsResourceName =
        match this with
        | B2cDomainName name -> ResourceName name

type B2cTenant =
    {
        Name: B2cDomainName
        DisplayName: string
        DataResidency: Location
        CountryCode: string
        Tags: Map<string, string>
        Sku: B2cTenant.Sku
    }

    interface IArmResource with
        member this.ResourceId = b2cTenant.resourceId this.Name.AsResourceName

        member this.JsonModel =
            {| b2cTenant.Create(this.Name.AsResourceName, this.DataResidency, tags = this.Tags) with
                sku =
                    {|
                        name = string this.Sku
                        tier = "A0"
                    |}
                properties =
                    {|
                        createTenantProperties =
                            {|
                                countryCode = this.CountryCode
                                displayName = this.DisplayName
                            |}
                    |}
            |}
