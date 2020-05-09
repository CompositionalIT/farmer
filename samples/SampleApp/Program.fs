open Farmer
open Farmer.Resources

//TODO: Create resources here!

let deployment = arm {
    location NorthEurope

    //TODO: Assign resources here using the add_resource keyword
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite @"generated-template"

// Or deploy it directly to Azure here... (required Azure CLI installed!)
// deployment
// |> Deploy.quick "my-resource-group"