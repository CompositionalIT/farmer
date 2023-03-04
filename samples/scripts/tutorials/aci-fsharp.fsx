#r "nuget: Farmer"

let script =
    """
#r "nuget: Suave, Version=2.6.0"
open Suave
let config = { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ] }
startWebServer config (Successful.OK "Hello Farmers!")
"""

open Farmer
open Farmer.Builders

let containers = containerGroup {
    name "my-app"

    add_instances [
        containerInstance {
            name "fsi"
            image "mcr.microsoft.com/dotnet/sdk:5.0"
            command_line ("dotnet fsi /src/main.fsx".Split null |> List.ofArray)
            add_volume_mount "script-source" "/src"
            add_public_ports [ 8080us ]
            cpu_cores 0.2
            memory 0.5<Gb>
        }
    ]

    public_dns "my-app-fsi-suave" [ TCP, 8080us ]
    add_volumes [ volume_mount.secret_string "script-source" "main.fsx" script ]
}

arm {
    location Location.EastUS
    add_resources [ containers ]
}
|> Writer.quickWrite "aci-fsharp"
