[<AutoOpen>]
module Farmer.ArmBuilder

open System.IO
open System.IO.Compression

/// Represents all configuration information to generate an ARM template.
type ArmConfig =
    { Parameters : string Set
      Outputs : (string * string) list
      Location : Location
      Resources : IResource list }

type ZipDeployKind =
    | DeployFolder of string
    | DeployZip of string
    member this.Value = match this with DeployFolder s | DeployZip s -> s
    /// Tries to create a ZipDeployKind from a string path.
    static member TryParse path =
        if (File.GetAttributes path).HasFlag FileAttributes.Directory then
            Some(DeployFolder path)
        else if Path.GetExtension path = ".zip" then
            Some(DeployZip path)
        else
            None
    /// Processes a ZipDeployKind and returns the filename of the zip file.
    /// If the ZipDeployKind is a DeployFolder, the folder will be zipped first and the generated zip file returned.
    member this.GetZipPath targetFolder =
        match this with
        | DeployFolder appFolder ->
            let packageFilename = Path.Combine(targetFolder, (Path.GetFileName appFolder) + ".zip")
            File.Delete packageFilename
            ZipFile.CreateFromDirectory(appFolder, packageFilename)
            packageFilename
        | DeployZip zipFilePath ->
            zipFilePath

type PostDeployTask =
    | RunFromZip of {| WebApp:ResourceName; Path : ZipDeployKind |}
type Deployment =
    { Location : Location
      Template : ArmTemplate
      PostDeployTasks : PostDeployTask list }

type ArmBuilder() =
    member __.Yield _ =
        { Parameters = Set.empty
          Outputs = List.empty
          Resources = List.empty
          Location = WestEurope }

    member __.Run (state:ArmConfig) =
        let resources =
            state.Resources
            |> List.groupBy(fun r -> r.ResourceName)
            |> List.choose(fun (resourceName, instances) ->
                match instances with
                | [] ->
                   None
                | [ resource ] ->
                   Some resource
                | resource :: _ ->
                   printfn "Warning: %d resources were found with the same name of '%s'. The first one will be used." instances.Length resourceName.Value
                   Some resource)
        let output =
            { Parameters = [
                for resource in resources do
                    //TODO: Make an IParameterOutput?
                    match resource with
                    | :? Resources.SqlAzure.SqlAzure as sql -> sql.Credentials.Password
                    | :? Resources.VirtualMachine.VirtualMachine as vm -> vm.Credentials.Password
                    | :? Resources.KeyVault.KeyVaultSecret as kvs ->
                        match kvs with
                        | { Value = ParameterSecret secureParameter } -> secureParameter
                        | _ -> ()
                    | :? Farmer.Resources.WebApp.WebApp as wa -> yield! wa.Parameters
                    | _ -> ()
              ] |> List.distinct
              Outputs = state.Outputs
              Resources = resources }

        //TODO: Replace with IPostDeploy?
        let webDeploys = [
            for resource in resources do
                match resource with
                | :? Farmer.Resources.WebApp.WebApp as wa ->
                    match wa with
                    | { ZipDeployPath = Some path; Name = name } ->
                        let path =
                            ZipDeployKind.TryParse path
                            |> Option.defaultWith (fun () ->
                                failwithf "Path '%s' must either be a folder to be zipped, or an existing zip." path)
                        RunFromZip {| Path = path; WebApp = name |}
                    | _ ->
                        ()
                | _ ->
                    ()
            ]

        { Location = state.Location
          Template = output
          PostDeployTasks = webDeploys }

    /// Creates an output value that will be returned by the ARM template.
    [<CustomOperation "output">]
    member __.Output (state, outputName, outputValue) : ArmConfig = { state with Outputs = (outputName, outputValue) :: state.Outputs }
    member this.Output (state:ArmConfig, outputName:string, (ResourceName outputValue)) = this.Output(state, outputName, outputValue)
    member this.Output (state:ArmConfig, outputName:string, outputValue:ArmExpression) = this.Output(state, outputName, outputValue.Eval())
    member this.Output (state:ArmConfig, outputName:string, outputValue:string option) =
        match outputValue with
        | Some outputValue -> this.Output(state, outputName, outputValue)
        | None -> state
    member this.Output (state:ArmConfig, outputName:string, outputValue:ArmExpression option) =
        match outputValue with
        | Some outputValue -> this.Output(state, outputName, outputValue)
        | None -> state

    /// Sets the default location of all resources.
    [<CustomOperation "location">]
    member __.Location (state, location) : ArmConfig = { state with Location = location }
    [<CustomOperation "add_resource">]

    (* These two "fake" methods are needed to ensure that extension members for each builder
       is always available. *)

    /// Adds a single resource to the ARM template.
    member __.AddResource (state:ArmConfig, builder:IResourceBuilder) =
        let resources =
            builder.BuildResources state.Location state.Resources
            |> List.fold(fun resources action ->
                match action with
                | NewResource newResource -> resources @ [ newResource ]
                | MergedResource(oldVersion, newVersion) -> (resources |> List.filter ((<>) oldVersion)) @ [ newVersion ]
                | CouldNotLocate (ResourceName resourceName) -> failwithf "Could not locate the parent resource ('%s'). Make sure you have correctly specified the name, and that it was added to the arm { } builder before this one." resourceName
                | NotSet -> failwith "No parent resource name was set for this resource to link to.") state.Resources

        { state with Resources = resources }
    [<CustomOperation "add_resources">]
    /// Adds a sequence of resources to the ARM template.
    member __.AddResources (state:ArmConfig, ()) = state

let internal addResources<'a> (addOne:ArmConfig * 'a -> ArmConfig) (state:ArmConfig) resources =
    (state, resources)
    ||> Seq.fold(fun state resource -> addOne (state, resource))

let arm = ArmBuilder()