[<AutoOpen>]
module Farmer.Builders.B2cTenant

open Farmer
open Farmer.Arm
open Farmer.Validation

type B2cDomainName with

    static member FromInitialDomainName initialDomainName =
        [ containsOnlyM [ lettersOrNumbers ] ]
        |> validate "B2c initial domain name" initialDomainName
        |> Result.map (fun x -> B2cDomainName $"{x}.onmicrosoft.com")

/// Check official documentation for more details: https://learn.microsoft.com/en-us/azure/active-directory-b2c/data-residency#data-residency
type B2cDataResidency =
    | UnitedStates
    | Europe
    | AsiaPacific
    | Japan
    | Australia

    member this.Location =
        match this with
        | UnitedStates -> Location "United States"
        | Europe -> Location "Europe"
        | AsiaPacific -> Location "Asia Pacific"
        | Japan -> Location "Japan"
        | Australia -> Location "Australia"

type B2cTenantConfig =
    {
        Name: B2cDomainName
        DisplayName: string
        DataResidency: Location
        CountryCode: string
        Sku: B2cTenant.Sku
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = b2cTenant.resourceId this.Name.AsResourceName

        member this.BuildResources _ =
            [
                {
                    B2cTenant.Name = this.Name
                    DisplayName = this.DisplayName
                    DataResidency = this.DataResidency
                    CountryCode = this.CountryCode
                    Tags = this.Tags
                    Sku = this.Sku
                }
            ]

type B2cTenantBuilder() =
    member _.Yield _ =
        {
            Name = B2cDomainName.Empty
            DisplayName = ""
            DataResidency = B2cDataResidency.Europe.Location
            CountryCode = "FR"
            Sku = B2cTenant.Sku.PremiumP1
            Tags = Map.empty
        }

    [<CustomOperation("initial_domain_name")>]
    member _.InitialDomainName(state: B2cTenantConfig, name: string) =
        { state with
            Name = B2cDomainName.FromInitialDomainName(name).OkValue
        }

    [<CustomOperation("display_name")>]
    member _.DisplayName(state: B2cTenantConfig, displayName: string) =
        { state with DisplayName = displayName }

    [<CustomOperation("sku")>]
    member _.Sku(state: B2cTenantConfig, sku: B2cTenant.Sku) = { state with Sku = sku }

    /// Data residency location as described in: https://learn.microsoft.com/en-us/azure/active-directory-b2c/data-residency#data-residency
    [<CustomOperation("data_residency")>]
    member _.DataResidency(state: B2cTenantConfig, b2cDataResidency: B2cDataResidency) =
        { state with
            DataResidency = b2cDataResidency.Location
        }

    /// Country Code defined by two capital letters (example: FR), as described in: https://learn.microsoft.com/en-us/azure/active-directory-b2c/data-residency#data-residency
    [<CustomOperation("country_code")>]
    member _.CountryCode(state: B2cTenantConfig, countryCode: string) =
        { state with CountryCode = countryCode }

    interface ITaggable<B2cTenantConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

let b2cTenant = B2cTenantBuilder()
