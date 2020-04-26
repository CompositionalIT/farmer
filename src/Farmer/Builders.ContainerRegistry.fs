[<AutoOpen>]
module Farmer.Resources.ContainerRegistry

open Farmer
open Farmer.Models
// Requirements 
// minimum version is 2.3.1 of Azure CLI

// Step 1: Check feature does not already exist then open an issue asking about progress on feature and whether you want to pick it up.
//    Result1: https://github.com/CompositionalIT/farmer/issues/48
// Step 2: Find a good template to see structure and glean DU for options like SKUs
//    Result1: https//docs.microsoft.com/en-us/azure/templates/microsoft.containerregistry/2017-10-01/registries
//    Result2: https://github.com/Azure/azure-quickstart-templates/tree/master/101-container-registry-geo-replication
// Step 3: Define DU types found in step 2

// Step 4: Define config type

// Step 5: Define builder

// Step 5: Define Resource type in Farmer.fs. Add to SupportedResources

// Step 6: Tests

// Step 7: Converters + Outputters

// Step 8: Add to Writer

/// Container Registry SKU
type ContainerRegistrySku =
    | Basic
    | Standard
    | Premium

// TODO: networkRuleSet
// TODO: policies
// TODO: encryption
// TODO: dataEndpointEnabled

type ContainerRegistryConfig =
    {
      Name : ResourceName
      Sku : ContainerRegistrySku
      AdminUserEnabled : bool }
    
type ContainerRegistryBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = Basic
          AdminUserEnabled = false }
    [<CustomOperation "name">]
    member _.Name (state:ContainerRegistryConfig, name) = { state with Name = ResourceName name }
    [<CustomOperation "sku">]
    member _.Sku (state:ContainerRegistryConfig, sku) = { state with Sku = sku }
    [<CustomOperation "enable_admin_user">]
    member _.EnableAdminUser (state:ContainerRegistryConfig) = { state with AdminUserEnabled = true }

module Converters =
    let containerRegistry location (config:ContainerRegistryConfig) : ContainerRegistry =
        { Name = config.Name
          Location = location
          Sku = config.Sku.ToString().Replace("_", ".")
          AdminUserEnabled = config.AdminUserEnabled }

    module Outputters =
        let containerRegistry (service:Farmer.Models.ContainerRegistry) =
            {| name = service.Name.Value
               ``type`` = "Microsoft.ContainerRegistry/registries"
               apiVersion = "2019-05-01"
               sku = {| name = service.Sku |}
               location = service.Location.Value
               tags = {||}
               properties = {|
                              adminUserEnabled = service.AdminUserEnabled |} |}

let containerRegistry = ContainerRegistryBuilder()

type Farmer.ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config) =
        { state with
            Resources = ContainerRegistry (Converters.containerRegistry state.Location config) :: state.Resources
        }
    member this.AddResources (state, configs) = addResources<ContainerRegistryConfig> this.AddResource state configs
