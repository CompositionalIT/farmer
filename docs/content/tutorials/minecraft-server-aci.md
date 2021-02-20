---
title: "Create your own Minecraft Server"
date: 2021-02-13
draft: false
---

#### Introduction

In this tutorial, you'll use dotnet framework features and F# language techniques when building a template to create a fully functional [Minecraft Server](https://www.minecraft.net) running on an Azure Container Instance with its world data stored in an Azure Storage Account. Because Farmer is a domain specific language embedded within F#, you are able to utilize the rich dotnet ecosystem and a static type system to "craft" an advanced deployment.

1. Programmatically build our Minecraft configuration files.
1. Create a storage account for the world data.
1. Define a deployment script that will download the Minecraft Server .jar file and upload it to the storage account, along with the configuration files.
1. Create a container group for running the service;.

#### A few dependencies

We will need a few dependencies here, so first let's reference the packages and open the namespaces:

* Farmer - for building that Azure resources and deployment template.
* MinecraftConfig - for building the server configuration files.
* FSharp.Data - for scraping the server download page for the current version of the server .jar file.

```fsharp
#r "nuget: Farmer"
#r "nuget: MinecraftConfig"
#r "nuget: FSharp.Data"

open System
open Farmer
open Farmer.Builders
open FSharp.Data
open MinecraftConfig
```

#### Building the Minecraft server configuration files

Deploying infrastructure often also means building configuration files, and Minecraft is no different. There are four critical files for a server:

* ops.json - this includes the list of operators that can manage the server.
* whitelist.json - this includes a list of gamers who are allowed to use the server.
* eula.txt - this indicates that you've accepted the [End User License Agreement](https://www.minecraft.net/en-us/eula/) for running a Minecraft Server, and you should read this since downloading and using the game implies you agree with it.
* server.properties - the server is a Java application, and this is the configuration for the application itself.


First we build a list of users that we will allow on our server. We'll use this list to build both the ops.json (operators) and whitelist.json (allowed gamers), so we will indicate which ones are operators.

```fsharp
let operator = true
/// Our list of minecraft users - their username, uuid, and whether or not they are an operator.
let minecrafters = [
        "McUser1", "a6a66bfb-6ff7-46e3-981e-518e6a3f0e71", operator
        "McUser2", "d3f2e456-d6a4-47ac-a7f0-41a4dc8ed156", not operator
        "McUser3", "ceb50330-681a-4d9d-8e84-f76133d0fd28", not operator
    ]
```

Now we build the whitelist.json and ops.json files - the MinecraftConfig application handles formatting the configuration file, we just need to map from our list of `minecrafters` to the lists of the records for the whitelist and the operator. `whitelist` holds the contents of our whitelist.json file, and `ops` hosts the contents of the ops.json file. We will write use those later.

```fsharp
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
```

And we can generate an accepted EULA, storing this content in `eula`.

```fsharp
/// And accept the EULA.
let eula = Eula.format true
```

Now we need a few properties that are used both for the server.properties and for the resulting infrastructure. The `worldName` tells Minecraft where to store the world data. Since this will be mounted to an Azure Storage File share, we create a binding for it to make sure the name we use in the server.properties file matches what we use in the storage account.

Same for the `serverPort`, which is both used in the server.properties file and must be exposed publicly on the Azure Container Group.

The name of the storage account is used in three places: the storage account itself, in the deployment script that will upload files to the storage account, and in the container group that will mount a volume from it. The `storageAccountName` can be referenced in all three uses.

```fsharp
/// Add bindings for fields that are referenced in a few places
/// Name of the share for the world.
let worldName = "world1"
/// Port for this world
let serverPort = 25565
/// Storage account name
let storageAccountName = "mcworlddata"
```

And now we create the server.properties file, storing it in `serverProperties`. With that, we completed generating all of the configuration files for the server and can move on to defining and deploying the infrastructure.

```fsharp
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
```

#### Creating the Storage Account

A Minecraft server stores some data for the world that is generated and people play in. That data, along with the configuration files, is stored in a directory that must be accessible to the server. Azure Container Groups are able to attach an Azure Storage Account File share as a volume, so we will create a storage account with a file share.

```fsharp
/// A storage account, with a file share for the server config and world data.
let serverStorage = storageAccount {
    name storageAccountName
    sku Storage.Sku.Standard_LRS
    add_file_share_with_quota worldName 5<Gb>
}
```

#### Defining the Deployment Script

There are some deployment orchestration tasks that cannot be fully represented by Azure resources, but we need ARM to carry them out for us. We can use `deploymentScripts` as an Azure resource to represent script execution. This allows us to specify orchestration properties, such as that ARM should execute this deployment script _after_ the storage account is deployed.

The script itself runs in a temporary container that has the Azure CLI ready and authenticated with a user that has the "Contributor" role over everything in this deployment. This is helpful because it means our script runs as a user that can access the storage account to upload content.

We need this deployment script to do three things:

1. Copy the configuration files to the storage account.
1. Download the current server.jar to the script container's temporary storage.
1. Upload the server.jar to the storage account.

##### Embedding Configuration Files

First we will tackle the configuration files. We are going use F# to generate the CLI script, so we can actually embed these in the deployment script itself. To avoid any trouble with escaping characters for our script, we will encode all of the configuration files as base64 strings when we build the script and then the script will decode the base64 data and write files out to the container file system where the Azure CLI can upload them.

1. Convert each configuration file to base64.
1. Embed in shell script run by deployment.
1. When the deployment script runs, it will decode and save as files.
1. And then it will use `az storage file upload` to transfer them to the storage account.

```fsharp
/// A deployment script to create the config in the file share.
let deployConfig =
    // Helper function to base64 encode the files for embedding them in the
    // deployment script.
    let b64 (s:string) =
        s |> System.Text.Encoding.UTF8.GetBytes |> Convert.ToBase64String

    // Build a script that embeds the content of these files, writes to the
    // deploymentScript instance and then copies
    // to the storageAccount file share. We will include the contents of these
    // files as base64 encoded strings so there is no need to worry about
    // special characters in the embedded script.
    let uploadConfig =
        [
            whitelist, Whitelist.Filename
            ops, Ops.Filename
            eula, Eula.Filename
            serverProperties, ServerProperties.Filename
        ]
        |> List.map (fun (content, filename) ->
                     $"echo {b64 content} | base64 -d > {filename} && az storage file upload --account-name {storageAccountName} --share-name {worldName} --source {filename}")

```

That seemed a bit complicated, but using the best of both F# and the Azure CLI, the actual code to do this is minimal. The `b64` function converts any string you give it to bytes and then base64 encodes those bytes into a string we can embed in the script.

Next we have a list that contains the contents of each configuration file paired with the filename we need to write. We map each of those items to an interpolated string, which is where F# can execute little bits of code when building the string. Within the interpolated string, we call the `b64` function to encode the contents of each file, which is what `$"echo {b64 content}"` does. When the script executes, it will pass that string into `base64 -d` which decides the base64 back into bytes that are written to a file. After each file is written, it's uploaded with `az storage file upload` which again uses interpolated string values to get the `storageAccountName`, `worldName`, and `filename` values.

##### Deploying the Server Software

Having embedded the configuration files, now we need to add a line to the script to download the Minecraft server.jar and upload it as well. Whenever a new Minecraft Server is released, they update this page with a link that is named for the server version.

Without F#, we would probably stop here and just use the link for whatever version is out today. But F# has nice toys for reading and exploring data, like FSharp.Data which can parse HTML files, so we're only a few lines away from scraping the download page for the link to the current version.

When this F# code is executed to build the ARM template, it will load the Download page, find the link starting with `minecraft_server`, and copy the URL from the `href` on that link. We will embed that URL into our deployment script as a parameter to a `curl` call which will download the file before calling `az storage file upload` to copy the file to the storage account.

```fsharp
    /// The script will also need to download the server.jar and upload it.
    let uploadServerJar =
        let results = HtmlDocument.Load "https://www.minecraft.net/en-us/download/server"
        // Scrape for anchor tags from this download page.
        results.Descendants ["a"]
        // where the inner text contains "minecraft_server" since that's what is
        // displayed on that link
        |> Seq.filter (fun (x:HtmlNode) -> x.InnerText().StartsWith "minecraft_server")
        // And choose the "href" attribute if present
        |> Seq.choose(fun (x:HtmlNode) -> x.TryGetAttribute("href") |> Option.map(fun (a:HtmlAttribute) -> a.Value()))
        |> Seq.head // If it wasn't found, we'll get an error here.
        |> (fun url -> $"curl -O {url} && az storage file upload --account-name {storageAccountName} --share-name {worldName} --source server.jar")
```

Now we have two lists:
* `uploadConfig` is a list of the four lines of `bash` that will decode and then upload the configuration files to the storage account.
* `uploadServerJar` is a line of `bash` to download the server software and upload it to the storage account.

We concat those lines together with a semicolon `; ` to break up our commands, and we have a full script we can run. The `deploymentScript` resource itself is fairly simple, and we use `depends_on serverStorage` to make sure this only runs _after_ our storage account is deployed.

```fsharp
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
```

#### Creating the Container Instance

The container instance runs a Java Runtime Environmennt, giving it enough CPU and memory for a small server with a few players. It has a volume mounted to the Azure Storage Account File share where the configuration files and server.jar are uploaded.

The `containerGroup` has a dependency on the `storageAccount` so it won't be deployed until the storageAccount is deployed. There is a bit of a race condition since the container group could be deployed and start before the `deploymentScript` uploads the server.jar and configuration files. To prevent this issue the container runs a `while` loop in `bash` until the server starts successfully.

```fsharp
let serverContainer = containerGroup {
    name "minecraft-server"
    public_dns "azmcworld1" [ TCP, uint16 serverPort ]
    add_instances [
        containerInstance {
            name "minecraftserver"
            image "mcr.microsoft.com/java/jre-headless:8-zulu-alpine"
            // The command line needs to change to the directory for the file share
            // and then start the server.
            // It needs a little more memory than the defaults, -Xmx3G gives it 3 GiB
            // of memory.
            command_line [
                "/bin/sh"
                "-c"
                // We will need to do a retry loop since we can't have a depends_on
                // for the deploymentScript to finish.
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
```

Here we will build the template. The `deployConfig` deployment script is especially interesting as it contains the embedded configuration files and the `curl` command with the link to the current `server.jar` from scraping the download page.

```fsharp
/// Build the deployment with storage, deployment script, and container group.
let deployment = arm {
    location Location.EastUS
    add_resources [
        serverStorage
        deployConfig
        serverContainer
    ]
}
deployment |> Writer.quickWrite "minecraft-server"
```

After running this deployment, view the container group in the Azure Portal or with `az container logs` to watch the server start up and generate a world. Once the world is generated, it's ready to connect from your Minecraft Java Edition client by entering the DNS name for the container group!

If you need to change the configuration you could connect to the terminal of the container instance. But in the spirit of mature configuration management and immutable infrastructure, you should rebuild the config, stop the container group, and redeploy. The existing state - the minecraft world data - is left intact in the storage account and the configuration is replaced with your updates. Once the update is deployed, you can restart the container group.
