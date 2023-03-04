#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let er = expressRoute {
    name "my-test-circuit"
    service_provider "Equinix"
    peering_location "Frankfurt"
}

// Build an ARM resourceId type for the circuit.
let erId = ResourceId.create (Arm.Network.expressRouteCircuits, er.Name)

// Use that ID to build a reference expression and get a property of the referenced resource.
let serviceKeyRef =
    ArmExpression.create ($"reference({erId.ArmExpression.Value}).serviceKey")

// That reference can be used in the output for the template so that ARM can populate it from the newly deployed resource
arm {
    location Location.WestEurope
    add_resource er
    output "er-service-key" serviceKeyRef
}
|> Writer.quickWrite "custom-output"
