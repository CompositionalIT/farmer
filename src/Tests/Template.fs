module Template

open Farmer
open Xunit

module TestHelpers =
    let createDeployment outputs parameters =
        { Location = NorthEurope
          PostDeployTasks = []
          Template = {
              Outputs = outputs
              Parameters = parameters |> List.map SecureParameter
              Resources = []
          }
        }

let toTemplate (deployment:Deployment) =
    deployment.Template
    |> Writer.TemplateGeneration.processTemplate

[<Fact>]
let ``Can create a basic template`` () =
    let template = arm { location NorthEurope } |> toTemplate
    Assert.Equal(template.``$schema``, "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#")
    Assert.Empty template.outputs
    Assert.Empty template.parameters
    Assert.Empty template.resources

[<Fact>]
let ``Correct generates outputs`` () =
    let template =
        arm {
            output "foo" "bar"
            output "bar" "foo"
        }
        |> toTemplate
    Assert.Equal(template.outputs.["foo"].value, "bar")
    Assert.Equal(template.outputs.["bar"].value, "foo")
    Assert.Equal(template.outputs.Count, 2)

[<Fact>]
let ``Processes parameters correctly`` () =
    let template = TestHelpers.createDeployment [] [ "foo"; "bar" ] |> toTemplate

    Assert.Equal(template.parameters.["foo"].``type``, "securestring")
    Assert.Equal(template.parameters.["bar"].``type``, "securestring")
    Assert.Equal(template.parameters.Count, 2)
