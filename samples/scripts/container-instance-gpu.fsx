#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.ContainerInstanceGpu

let template = arm {
    location Location.WestEurope
    add_resources [
        containerGroup {
            name "container-group-with-gpu"
            operating_system Linux
            restart_policy ContainerGroup.RestartOnFailure
            add_instances [
                containerInstance {
                    name "gpucontainer"
                    image "mcr.microsoft.com/azuredocs/samples-tf-mnist-demo:gpu"
                    memory 12.0<Gb>
                    cpu_cores 4.0
                    gpu ( containerInstanceGpu { count 1
                                                 sku V100 } )
                }
            ]
        }
    ]
}

Writer.quickWrite "container-instance" template

