[<AutoOpen>]
module Farmer.Builders.StaticWebApp

open Farmer
open Farmer.Arm.Web
open Farmer.Arm.Web.StaticSites
open System

type StaticWebAppConfig =
    {
        Name: ResourceName
        Repository: Uri option
        Branch: string
        RepositoryToken: SecureParameter
        AppLocation: string
        ApiLocation: string option
        AppArtifactLocation: string option
        AppSettings: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = staticSites.resourceId this.Name

        member this.BuildResources location =
            [
                match this with
                | { Repository = Some uri } ->
                    {
                        Name = this.Name
                        Location = location
                        Repository = uri
                        Branch = this.Branch
                        RepositoryToken = this.RepositoryToken
                        AppLocation = this.AppLocation
                        ApiLocation = this.ApiLocation
                        AppArtifactLocation = this.AppArtifactLocation
                    }

                    if not this.AppSettings.IsEmpty then
                        {
                            Config.StaticSite = this.Name
                            Properties = this.AppSettings
                        }
                | _ -> raiseFarmer "You must set the repository URI."
            ]

    member this.RepositoryParameter = $"repositorytoken-for-{this.Name.Value}"

type StaticWebAppBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Repository = None
            Branch = "master"
            RepositoryToken = SecureParameter ""
            AppLocation = ""
            ApiLocation = None
            AppArtifactLocation = None
            AppSettings = Map.empty
        }

    member _.Run(state: StaticWebAppConfig) =
        { state with
            RepositoryToken = SecureParameter state.RepositoryParameter
        }

    [<CustomOperation "name">]
    member _.Name(state: StaticWebAppConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "repository">]
    member _.Repository(state: StaticWebAppConfig, uri) =
        { state with
            Repository = Some(Uri uri)
        }

    [<CustomOperation "branch">]
    member _.Branch(state: StaticWebAppConfig, branch) = { state with Branch = branch }

    [<CustomOperation "api_location">]
    member _.ApiLocation(state: StaticWebAppConfig, location) =
        { state with
            ApiLocation = Some location
        }

    [<CustomOperation "app_location">]
    member _.AppLocation(state: StaticWebAppConfig, location) = { state with AppLocation = location }

    [<CustomOperation "artifact_location">]
    member _.ArtifactLocation(state: StaticWebAppConfig, location) =
        { state with
            AppArtifactLocation = Some location
        }

    [<CustomOperation "app_settings">]
    member _.AppSettings(state: StaticWebAppConfig, settings) =
        { state with
            AppSettings = Map settings
        }

let staticWebApp = StaticWebAppBuilder()
