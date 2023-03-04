#r "nuget: Farmer"
#r "nuget: FSharp.Text.Docker"

open System
open Farmer
open Farmer.ContainerApp
open Farmer.Builders
open FSharp.Text.Docker.Builders

// Create Container Registry
let myAcr = containerRegistry {
    name "farmercontainers"
    sku ContainerRegistry.Basic
    enable_admin_user
}

// Some quick app we want to run. In the real world, you'll pull this source when building
// the image, but here we will just embed it.
let fsharpAppSource =
    """
#r "nuget: Suave, Version=2.6.0"
open Suave
let config = { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ] }
startWebServer config (Successful.OK "Hello Container App Farmers!")
"""

let encodedSource =
    fsharpAppSource |> System.Text.Encoding.UTF8.GetBytes |> Convert.ToBase64String

// Define Docker image
let dockerfile = dockerfile {
    from "mcr.microsoft.com/dotnet/sdk:5.0.404"
    expose [ 8080 ]
    run $"echo {encodedSource} | base64 -d > main.fsx"
    cmd "dotnet fsi main.fsx"
}

let encodedDockerfile =
    dockerfile.Build()
    |> System.Text.Encoding.UTF8.GetBytes
    |> Convert.ToBase64String

let scriptIdentity = createUserAssignedIdentity "deployment-identity"

// Build and push to that ACR from a deploymentScript
let buildImage = deploymentScript {
    name "build-image"
    env_vars [ "ACR_NAME", "farmercontainers" ]
    identity scriptIdentity
    depends_on myAcr

    script_content (
        [
            "set -eux"
            $"echo {encodedDockerfile} | base64 -d > Dockerfile"
            $"az acr build --registry $ACR_NAME --image fsharpwebapp:1.0.0 ."
        ]
        |> String.concat " ; "
    )
}

/// Deploy a container app after the image is built. It will reference the container registry to get credentials.
let farmerlogs = logAnalytics { name "farmerlogs" }
let containerRegistryDomain = "myregistry.azurecr.io"
let version = "1.0.0"

let containerEnv = containerEnvironment {
    name "farmercontainers"
    depends_on buildImage
    log_analytics_instance farmerlogs

    add_containers [
        containerApp {
            name "http"
            active_revision_mode ActiveRevisionsMode.Single
            reference_registry_credentials [ Farmer.Arm.ContainerRegistry.registries.resourceId myAcr.Name ]

            add_containers [
                container {
                    container_name "fsharpwebapp"
                    private_docker_image $"{myAcr.Name.Value}.azurecr.io" "fsharpwebapp" version
                    cpu_cores 0.3<VCores>
                    memory 0.8<Gb>
                }
                container {
                    container_name "http-frontend"
                    public_docker_image "nginx" "latest"
                    cpu_cores 0.2<VCores>
                    memory 0.2<Gb>
                }
            ]

            replicas 1 5
            ingress_visibility External
            ingress_target_port 8080us
            ingress_transport Auto
            dapr_app_id "http"
            add_scale_rule "http-rule" (ScaleRule.Http { ConcurrentRequests = 100 })
        }
    ]
}

/// Deployment template to orchestrate all of these.
let template = arm {
    location Location.EastUS
    add_resources [ scriptIdentity; farmerlogs; myAcr; buildImage; containerEnv ]
}

template.Template |> Writer.toJson |> Console.WriteLine
