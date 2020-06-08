open Farmer
open Farmer.Builders
open Farmer.CoreTypes

//TODO: Create resources here!

let s = storageAccount {
    name "myStorage"
}

let x = ((s :> IBuilder).BuildResources Location.NorthEurope []).[0]

let w = webApp {
    name "myWebApp"
    depends_on s.Name
    depends_on x
    depends_on s
}

let deployment = arm {
    location Location.NorthEurope

    //TODO: Assign resources here using the add_resource keyword
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite @"generated-template"

// Or deploy it directly to Azure here... (required Azure CLI installed!)
// deployment
// |> Deploy.execute "my-resource-group" Deploy.NoParameters