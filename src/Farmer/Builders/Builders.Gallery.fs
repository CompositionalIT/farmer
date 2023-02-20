[<AutoOpen>]
module Farmer.Builders.Gallery

open Farmer
open Farmer.GalleryValidation
open Farmer.Arm.Gallery

type GalleryConfig =
    {
        Name: GalleryName
        Description: string option
        SharingProfile: SharingProfile option
        SoftDelete: FeatureFlag option
        Tags: Map<string, string>
        Dependencies: Set<ResourceId>
    }

    interface IBuilder with
        member this.ResourceId = galleries.resourceId this.Name.ResourceName

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    Description = this.Description
                    SharingProfile = this.SharingProfile
                    SoftDeletePolicy =
                        this.SoftDelete
                        |> Option.map (fun flag -> { IsSoftDeleteEnabled = flag.AsBoolean })
                    Tags = this.Tags
                    Dependencies = this.Dependencies
                }
            ]

type GalleryBuilder() =
    member _.Yield _ =
        {
            Name = GalleryName.Empty
            Description = None
            SharingProfile = None
            SoftDelete = None
            Tags = Map.empty
            Dependencies = Set.empty
        }

    [<CustomOperation "name">]
    member _.Name(config: GalleryConfig, name: string) =
        { config with
            Name = GalleryName.Create(name).OkValue
        }

    [<CustomOperation "description">]
    member _.Description(config: GalleryConfig, description) =
        { config with
            Description = Some description
        }

    [<CustomOperation "sharing_profile">]
    member _.SharingProfile(config: GalleryConfig, sharingProfile) =
        { config with
            SharingProfile = Some sharingProfile
        }

    [<CustomOperation "soft_delete">]
    member _.SoftDelete(config: GalleryConfig, flag: FeatureFlag) = { config with SoftDelete = Some flag }

    interface ITaggable<GalleryConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    interface IDependable<GalleryConfig> with
        member _.Add state resources =
            { state with
                Dependencies = state.Dependencies + resources
            }

let gallery = GalleryBuilder()
