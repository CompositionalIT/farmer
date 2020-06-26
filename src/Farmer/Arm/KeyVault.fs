[<AutoOpen>]
module Farmer.Arm.KeyVault

open Farmer
open Farmer.CoreTypes
open Farmer.KeyVault
open System

module Vaults =
    type Secret =
        { Name : ResourceName
          Location : Location
          Value : SecretValue
          ContentType : string option
          Enabled : bool option
          ActivationDate : DateTime option
          ExpirationDate : DateTime option
          Dependencies : ResourceName list }
        static member ``1970`` = DateTime(1970,1,1,0,0,0)
        static member TotalSecondsSince1970 (d:DateTime) = (d.Subtract Secret.``1970``).TotalSeconds |> int
        interface IParameters with
            member this.SecureParameters =
                match this with
                | { Value = ParameterSecret secureParameter } -> [ secureParameter ]
                | _ -> []
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| ``type`` = "Microsoft.KeyVault/vaults/secrets"
                   name = this.Name.Value
                   apiVersion = "2018-02-14"
                   location = this.Location.ArmValue
                   dependsOn = [
                       for dependency in this.Dependencies do
                           dependency.Value ]
                   properties =
                       {| value = this.Value.Value
                          contentType = this.ContentType |> Option.toObj
                          attributes =
                           {| enabled = this.Enabled |> Option.toNullable
                              nbf = this.ActivationDate |> Option.map Secret.TotalSecondsSince1970 |> Option.toNullable
                              exp = this.ExpirationDate |> Option.map Secret.TotalSecondsSince1970 |> Option.toNullable
                           |}
                       |}
                   |} :> _

type CreateMode = Recover | Default
type Vault =
    { Name : ResourceName
      Location : Location
      TenantId : string
      Sku : KeyVault.Sku
      Uri : Uri option
      Deployment : FeatureFlag option
      DiskEncryption : FeatureFlag option
      TemplateDeployment : FeatureFlag option
      SoftDelete : SoftDeletionMode option
      CreateMode : CreateMode option
      AccessPolicies :
        {| ObjectId : ArmExpression
           ApplicationId : Guid option
           Permissions :
            {| Keys : Key Set
               Secrets : Secret Set
               Certificates : Certificate Set
               Storage : Storage Set |}
        |} array
      DefaultAction : DefaultAction option
      Bypass: Bypass option
      IpRules : string list
      VnetRules : string list }
      member this.PurgeProtection =
        match this.SoftDelete with
        | None
        | Some SoftDeletionOnly ->
            None
        | Some SoftDeleteWithPurgeProtection ->
            Some true
      member private this.ToStringArray s = s |> Set.map(fun s -> s.ToString().ToLower()) |> Set.toArray
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type``= "Microsoft.KeyVault/vaults"
               name = this.Name.Value
               apiVersion = "2018-02-14"
               location = this.Location.ArmValue
               properties =
                 {| tenantId = this.TenantId
                    sku = {| name = this.Sku.ArmValue; family = "A" |}
                    enabledForDeployment = this.Deployment |> Option.map(fun f -> f.AsBoolean) |> Option.toNullable
                    enabledForDiskEncryption = this.DiskEncryption |> Option.map(fun f -> f.AsBoolean) |> Option.toNullable
                    enabledForTemplateDeployment = this.TemplateDeployment |> Option.map(fun f -> f.AsBoolean) |> Option.toNullable
                    enableSoftDelete =
                        match this.SoftDelete with
                        | None ->
                            Nullable()
                        | Some SoftDeleteWithPurgeProtection
                        | Some SoftDeletionOnly ->
                            Nullable true
                    createMode = this.CreateMode |> Option.map(fun m -> m.ToString().ToLower()) |> Option.toObj
                    enablePurgeProtection = this.PurgeProtection
                    vaultUri = this.Uri |> Option.map string |> Option.toObj
                    accessPolicies =
                         [| for policy in this.AccessPolicies do
                             {| objectId = ArmExpression.Eval policy.ObjectId
                                tenantId = this.TenantId
                                applicationId = policy.ApplicationId |> Option.map string |> Option.toObj
                                permissions =
                                 {| keys = this.ToStringArray policy.Permissions.Keys
                                    storage = this.ToStringArray policy.Permissions.Storage
                                    certificates = this.ToStringArray policy.Permissions.Certificates
                                    secrets = this.ToStringArray policy.Permissions.Secrets |}
                             |}
                         |]
                    networkAcls =
                     {| defaultAction = this.DefaultAction  |> Option.map string |> Option.toObj
                        bypass = this.Bypass  |> Option.map string |> Option.toObj
                        ipRules = this.IpRules
                        virtualNetworkRules = this.VnetRules |}
                 |}
             |} :> _

