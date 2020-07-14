[<AutoOpen>]
module Farmer.Arm.Storage

open Farmer
open Farmer.Storage
open Farmer.CoreTypes

let storageAccounts = ResourceType "Microsoft.Storage/storageAccounts"
let containers = ResourceType "Microsoft.Storage/storageAccounts/blobServices/containers"
let fileShares = ResourceType "Microsoft.Storage/storageAccounts/fileServices/shares"
let queues = ResourceType "Microsoft.Storage/storageAccounts/queueServices/queues"

type StorageAccount =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      StaticWebsite : (string * string * string option) option }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = storageAccounts.ArmValue
               sku = {| name = this.Sku.ArmValue |}
               kind = "StorageV2"
               name = this.Name.Value
               apiVersion = "2018-07-01"
               location = this.Location.ArmValue
            |} :> _
    interface IPostDeploy with
        member this.Run resourceGroupName =
            match this with
            | { StaticWebsite = Some (indexDoc, errorDoc, folder); Name = name } ->
                printfn "Enabling static web site for storage account %s with Index document as %s, Error document as %s" name.Value indexDoc errorDoc 
                Deploy.Az.enableStaticWebsite name.Value indexDoc errorDoc
                |> Some
                |> Option.bind (fun r1 ->
                    folder
                    |> Option.map (fun f ->
                        printfn "Deploying content of %s folder to $web container for storage account %s" f name.Value
                        Deploy.Az.batchUploadStaticWebsite name.Value f
                    )
                    |> Option.map (fun r2 -> [r1;r2] |> Result.sequence |> Result.map (String.concat ", "))
                )
            | _ -> None
                    
                            
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