[<AutoOpen>]
module Farmer.Arm.Storage

open Farmer
open Farmer.Storage
open Farmer.CoreTypes
open System

let storageAccounts = ResourceType "Microsoft.Storage/storageAccounts"
let containers = ResourceType "Microsoft.Storage/storageAccounts/blobServices/containers"
let fileShares = ResourceType "Microsoft.Storage/storageAccounts/fileServices/shares"
let queues = ResourceType "Microsoft.Storage/storageAccounts/queueServices/queues"

type StorageAccountName =
    private | StorageAccountName of ResourceName
    static member Create name =
        if String.IsNullOrWhiteSpace name then Error "Storage account name cannot be empty"
        elif name.Length > 24 then Error (sprintf "Storage account name max length is 24, but here is %d ('%s')" name.Length name)
        elif name |> Seq.exists Char.IsUpper then Error (sprintf "Storage account name does not allow upper case letters ('%s')" name)
        elif name |> Seq.exists (Char.IsLetterOrDigit >> not) then Error (sprintf "Only alphanumeric characters are allowed ('%s')" name)
        else Ok (StorageAccountName (ResourceName name))
    static member Create (ResourceName name) = StorageAccountName.Create name
    member this.ResourceName = match this with StorageAccountName name -> name

type StorageAccount =
    { Name : StorageAccountName
      Location : Location
      Sku : Sku
      EnableHierarchicalNamespace : bool
      StaticWebsite : {| IndexPage : string; ErrorPage : string option; ContentPath : string |} option
      Tags: Map<string,string>}
    interface IArmResource with
        member this.ResourceName = this.Name.ResourceName
        member this.JsonModel =
            {| ``type`` = storageAccounts.ArmValue
               sku = {| name = this.Sku.ArmValue |}
               kind = "StorageV2"
               name = this.Name.ResourceName.Value
               apiVersion = "2018-07-01"
               location = this.Location.ArmValue
               properties = {| isHnsEnabled = this.EnableHierarchicalNamespace |}
               tags = this.Tags
            |} :> _
    interface IPostDeploy with
        member this.Run _ =
            this.StaticWebsite
            |> Option.map(fun staticWebsite -> result {
                let! enableStaticResponse = Deploy.Az.enableStaticWebsite this.Name.ResourceName.Value staticWebsite.IndexPage staticWebsite.ErrorPage
                printfn "Deploying content of %s folder to $web container for storage account %s" staticWebsite.ContentPath this.Name.ResourceName.Value
                let! uploadResponse = Deploy.Az.batchUploadStaticWebsite this.Name.ResourceName.Value staticWebsite.ContentPath
                return enableStaticResponse + ", " + uploadResponse
            })

module BlobServices =
    type Container =
        { Name : ResourceName
          StorageAccount : ResourceName
          Accessibility : StorageContainerAccess }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| ``type`` = containers.ArmValue
                   apiVersion = "2018-03-01-preview"
                   name = this.StorageAccount.Value + "/default/" + this.Name.Value
                   dependsOn = [ this.StorageAccount.Value ]
                   properties =
                    {| publicAccess =
                        match this.Accessibility with
                        | Private -> "None"
                        | Container -> "Container"
                        | Blob -> "Blob" |}
                |} :> _

module FileShares =
    type FileShare =
        { Name: ResourceName
          ShareQuota: int option
          StorageAccount: ResourceName }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| ``type`` = fileShares.ArmValue
                   apiVersion = "2019-06-01"
                   name = this.StorageAccount.Value + "/default/" + this.Name.Value
                   properties = {| shareQuota = this.ShareQuota |> Option.defaultValue 5120 |}
                   dependsOn = [ this.StorageAccount.Value ]
                |} :> _

module Queues =
    type Queue =
        { Name : ResourceName
          StorageAccount : ResourceName }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| name = this.StorageAccount.Value + "/default/" + this.Name.Value
                   ``type`` = queues.ArmValue
                   dependsOn = [ this.StorageAccount.Value ]
                   apiVersion = "2019-06-01"
                |} :> _