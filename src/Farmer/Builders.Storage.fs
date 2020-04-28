[<AutoOpen>]
module Farmer.Resources.Storage

open Farmer
open Farmer.Models
let internal buildKey (ResourceName name) =
    sprintf
        "concat('DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=', listKeys('%s', '2017-10-01').keys[0].value)"
            name
            name
    |> ArmExpression

type StorageAccountConfig =
    { /// The name of the storage account.
      Name : ResourceName
      /// The sku of the storage account.
      Sku : Sku
      /// Containers for the storage account.
      Containers : (string * StorageContainerAccess) list}
    /// Gets the ARM expression path to the key of this storage account.
    member this.Key = buildKey this.Name
type StorageAccountBuilder() =
    member __.Yield _ = { Name = ResourceName.Empty; Sku = Sku.StandardLRS; Containers = [] }
    member _.Run(state:StorageAccountConfig) =
        { state with
            Name = state.Name |> Helpers.sanitiseStorage |> ResourceName }
    /// Sets the name of the storage account.
    [<CustomOperation "name">]
    member __.Name(state:StorageAccountConfig, name) = { state with Name = name }
    member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the storage account.
    [<CustomOperation "sku">]
    member __.Sku(state:StorageAccountConfig, sku) = { state with Sku = sku }
    /// Adds private container.
    [<CustomOperation "add_private_container">]
    member __.AddPrivateContainer(state:StorageAccountConfig, name) = { state with Containers = (name, StorageContainerAccess.Private) :: state.Containers }
    /// Adds container with anonymous read access for blobs and containers.
    [<CustomOperation "add_public_container">]
    member __.AddPublicContainer(state:StorageAccountConfig, name) = { state with Containers = (name, StorageContainerAccess.Container) :: state.Containers }
    /// Adds container with anonymous read access for blobs only.
    [<CustomOperation "add_blob_container">]
    member __.AddBlobContainer(state:StorageAccountConfig, name) = { state with Containers = (name, StorageContainerAccess.Blob) :: state.Containers }

module Converters =
    let storage location (sac:StorageAccountConfig) =
        { Location = location
          Name = sac.Name
          Sku = sac.Sku
          Containers = sac.Containers }

    module Outputters =
        let private containerAccess (a:StorageContainerAccess) =
            match a with
            | Private -> "None"
            | Container -> "Container"
            | Blob -> "Blob"

        let private storageAccountContainer (parent:StorageAccount) (name, access) = {|
            ``type`` = "blobServices/containers"
            apiVersion = "2018-03-01-preview"
            name = "default/" + name
            dependsOn = [ parent.Name.Value ]
            properties = {| publicAccess = containerAccess access |}
        |}

        let storageAccount (resource:StorageAccount) = {|
            ``type`` = "Microsoft.Storage/storageAccounts"
            sku = {| name = resource.Sku.ArmValue |}
            kind = "StorageV2"
            name = resource.Name.Value
            apiVersion = "2018-07-01"
            location = resource.Location.ArmValue
            resources = resource.Containers |> List.map (storageAccountContainer resource)
        |}


type Farmer.ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config:StorageAccountConfig) =
        { state with
            Resources = StorageAccount (Converters.storage state.Location config) :: state.Resources
        }
    member this.AddResources (state, configs) = addResources<StorageAccountConfig> this.AddResource state configs

let storageAccount = StorageAccountBuilder()