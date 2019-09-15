open Farmer

//TODO: Create resources here!



let template = arm {
    location Locations.NorthEurope

    //TODO: Assign resources here!
}

// Generate the ARM template here...
template
|> Writer.toJson
|> Writer.toFile @"generated-template.json"