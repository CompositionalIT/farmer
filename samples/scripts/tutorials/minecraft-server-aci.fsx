#r "nuget: Farmer"
#r "nuget: MinecraftConfig"
#r "nuget: FSharp.Data"

open System
open Farmer
open Farmer.Builders
open FSharp.Data
open MinecraftConfig

(*
 * We want to get a Minecraft server with some customizations like the game mode and restricted users.
 * To get that we need a few things:
 * 1. A platform to host the server - for us that will be an Azure Container Group
 * 2. A place to store the world and config data - Azure Storage Files will do nicely for access from the container.
 * 3. Write the config data during deployment - we can generate it here and then pass it to a deploymentScript
 *    that can write the files to the storage account.
 *
 * That means the storageAccount and files need to be there first, then the deploymentScript should run to create
 * the config, and finally the containerGroup should be deployed, reading the config and starting the server.
 *)

/// Add bindings for fields that are referenced in a few places
/// Name of the share for the world.
let worldName = "world1"
/// Port for this world
let serverPort = 25565
/// Storage account name
let storageAccountName = "mcworlddata"

let operator = true
/// Our list of minecraft users - their username, uuid, and whether they are an operator.
let minecrafters = [
        "McUser1", "a6a66bfb-6ff7-46e3-981e-518e6a3f0e71", operator
        "McUser2", "d3f2e456-d6a4-47ac-a7f0-41a4dc8ed156", not operator
        "McUser3", "ceb50330-681a-4d9d-8e84-f76133d0fd28", not operator
    ]

/// Let's allow our list of minecrafters on the whitelist.
let whitelist =
    minecrafters
    |> List.map (fun (name, uuid, _) -> { Name=name; Uuid=uuid })
    |> Whitelist.format

/// Filter the minecrafters that aren't operators.
let ops =
    minecrafters
    |> List.filter (fun (_, _, op) -> op) // Filter anyone that isn't an operator
    |> List.map (fun (name, uuid, _) -> { Name=name; Level=OperatorLevel.Level4; Uuid=uuid })
    |> Ops.format


/// And accept the EULA.
let eula = Eula.format true

/// Write the customized server properties.
let serverProperties =
    [
        ServerPort serverPort
        RconPort (serverPort + 10)
        EnforceWhitelist true
        WhiteList true
        Motd "Azure Minecraft Server"
        LevelName worldName
        Gamemode "survival"
    ]
    |> ServerProperties.format

/// A storage account, with a file share for the server config and world data.
let serverStorage = storageAccount {
    name storageAccountName
    sku Storage.Sku.Standard_LRS
    add_file_share_with_quota worldName 5<Gb>
}

/// A deployment script to create the config in the file share.
let deployConfig =
    /// Helper function to base64 encode the files for embedding them in the deployment script.
    let b64 (s:string) =
        s |> System.Text.Encoding.UTF8.GetBytes |> Convert.ToBase64String
    
    /// Build a script that embeds the content of these files, writes to the deploymentScript instance and then copies
    /// to the storageAccount file share. We will include the contents of these files as base64 encoded strings so
    /// there is no need to worry about special characters in the embedded script.
    let uploadConfig =
        [
            whitelist, Whitelist.Filename
            ops, Ops.Filename
            eula, Eula.Filename
            serverProperties, ServerProperties.Filename
        ]
        |> List.map (fun (content, filename) ->
                     $"echo {b64 content} | base64 -d > {filename} && az storage file upload --account-name {storageAccountName} --share-name {worldName} --source {filename}")

    /// The script will also need to download the server.jar and upload it.
    let uploadServerJar =
        let results = HtmlDocument.Load "https://www.minecraft.net/en-us/download/server"
        // Scrape for anchor tags from this download page.
        results.Descendants ["a"]
        // where the inner text contains "minecraft_server" since that's what is displayed on that link
        |> Seq.filter (fun (x:HtmlNode) -> x.InnerText().StartsWith "minecraft_server")
        // And choose the "href" attribute if present
        |> Seq.choose(fun (x:HtmlNode) -> x.TryGetAttribute("href") |> Option.map(fun (a:HtmlAttribute) -> a.Value()))
        |> Seq.head // If it wasn't found, we'll get an error here.
        |> (fun url -> $"curl -O {url} && az storage file upload --account-name {storageAccountName} --share-name {worldName} --source server.jar")

    let scriptSource =
        uploadServerJar :: uploadConfig
        |> List.rev // do the server upload last so it won't start until the configs are in place.
        |> String.concat "; "
    
    deploymentScript {
        name "deployMinecraftConfig"
        // Depend on the storage account so this won't run until it's there.
        depends_on serverStorage
        script_content scriptSource
        force_update
    }

let serverContainer = containerGroup {
    name "minecraft-server"
    public_dns "azmcworld1" [ TCP, uint16 serverPort ]
    add_instances [
        containerInstance {
            name "minecraftserver"
            image "mcr.microsoft.com/java/jre-headless:8-zulu-alpine"
            // The command line needs to change to the directory for the file share and then start the server
            // It needs a little more memory than the defaults, -Xmx3G gives it 3 GiB of memory.
            command_line [
                "/bin/sh"
                "-c"
                // We will need to do a retry loop since we can't have a depends_on for the deploymentScript to finish.
                $"cd /data/{worldName}; while true; do java -Djava.net.preferIPv4Stack=true -Xms1G -Xmx3G -jar server.jar nogui && break; sleep 30; done"
            ]
            // If we chose a custom port in the settings, it should go here.
            add_public_ports [ uint16 serverPort ]
            // It needs a couple cores or the world may lag with a few players
            cpu_cores 2
            // Give it enough memory for the JVM
            memory 3.5<Gb>
            // Mount the path to the Azure Storage File share in the container
            add_volume_mount worldName $"/data/{worldName}"
        }
    ]
    // Add the file share for the world data and server configuration.
    add_volumes [
        volume_mount.azureFile worldName worldName serverStorage.Name.ResourceName.Value
    ]
}

/// Build the deployment with storage, deployment script, and container group.
let deployment = arm {
    location Location.EastUS
    add_resources [
        serverStorage
        deployConfig
        serverContainer
    ]
}

// Usually takes about 2 minutes to run, mostly the deploymentScript resources. Another minute later, the Minecraft
// world is generated and it's ready to use!
deployment |> Writer.quickWrite "minecraft-server-aci"
