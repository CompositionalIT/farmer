[<AutoOpen>]
module Farmer.Arm.KeyVault

open Farmer
open Farmer.CoreTypes
open Farmer.KeyVault
open System

let secrets = ResourceType ("Microsoft.KeyVault/vaults/secrets", "2018-02-14")
let vaults = ResourceType ("Microsoft.KeyVault/vaults", "2018-02-14")

module Vaults =
    type Secret =
        { Name : ResourceName
          Location : Location
          Value : SecretValue
          ContentType : string option
          Enabled : bool option
          ActivationDate : DateTime option
          ExpirationDate : DateTime option
          Dependencies : ResourceId list }
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
                {| secrets.Create(this.Name, this.Location, this.Dependencies) with
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
      VnetRules : string list
      Dependencies : ResourceId list
      Tags: Map<string,string>  }
      member this.PurgeProtection =
        match this.SoftDelete with
        | None
        | Some SoftDeletionOnly ->
            None
        | Some SoftDeleteWithPurgeProtection ->
            Some true
      member private _.ToStringArray s = s |> Set.map(fun s -> s.ToString().ToLower()) |> Set.toArray
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| vaults.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
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
                       accessPolicies = [|
                        for policy in this.AccessPolicies do
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

