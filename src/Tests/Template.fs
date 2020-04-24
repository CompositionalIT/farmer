module Template

open Farmer
open Xunit

module TestHelpers =
    let createSimpleDeployment outputs parameters =
        { Location = NorthEurope
          PostDeployTasks = []
          Template = {
              Outputs = outputs
              Parameters = parameters |> List.map SecureParameter
              Resources = []
          }
        }

open TestHelpers

let toTemplate (deployment:Deployment) =
    deployment.Template
    |> Writer.TemplateGeneration.processTemplate

[<Fact>]
let ``Can create a basic template`` () =
    let template = createSimpleDeployment [] [] |> toTemplate
    Assert.Equal(template.``$schema``, "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#")
    Assert.Empty template.outputs
    Assert.Empty template.parameters
    Assert.Empty template.resources

[<Fact>]
let ``Correct generates outputs`` () =
    let template = createSimpleDeployment [ "p1", "v1"; "p2", "v2" ] [] |> toTemplate
    Assert.Equal(template.outputs.["p1"].value, "v1")
    Assert.Equal(template.outputs.["p2"].value, "v2")
    Assert.Equal(template.outputs.Count, 2)

[<Fact>]
let ``Processes parameters correctly`` () =
    let template = createSimpleDeployment [] [ "p1"; "p2" ] |> toTemplate

    Assert.Equal(template.parameters.["p1"].``type``, "securestring")
    Assert.Equal(template.parameters.["p2"].``type``, "securestring")
    Assert.Equal(template.parameters.Count, 2)
