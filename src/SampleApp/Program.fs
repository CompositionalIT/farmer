open Farmer

//TODO: Create resources here!



let location, template = arm {
    location NorthEurope

    //TODO: Assign resources here using the add_resource keyword
}

// Generate the ARM template here...
let outputFilename =
    template
    |> Writer.toJson
    |> Writer.toFile @"generated-template"

// Or deploy it directly to Azure here... (required Azure CLI installed!)
(location, template)
|> Writer.quickDeploy "my-resource-group"
