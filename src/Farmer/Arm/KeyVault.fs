[<AutoOpen>]
module Farmer.Arm.KeyVault

open Farmer
open System

module Vaults =
    type Secret =
        { Name : ResourceName
          Location : Location
          Value : SecretValue
          ParentKeyVault : ResourceName
          ContentType : string option
          Enabled : bool Nullable
          ActivationDate : int Nullable
          ExpirationDate : int Nullable
          Dependencies : ResourceName list }
        interface IParameters with
            member this.SecureParameters =
                match this with
                | { Value = ParameterSecret secureParameter } -> [ secureParameter ]
                | _ -> []
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonValue =
                {| ``type`` = "Microsoft.KeyVault/vaults/secrets"
                   name = this.Name.Value
                   apiVersion = "2018-02-14"
                   location = this.Location.ArmValue
                   dependsOn = [
                       this.ParentKeyVault.Value
                       for dependency in this.Dependencies do
                           dependency.Value ]
                   properties =
                       {| value = this.Value.Value
                          contentType = this.ContentType |> Option.toObj
                          attributes =
                           {| enabled = this.Enabled
                              nbf = this.ActivationDate
                              exp = this.ExpirationDate
                           |}
                       |}
                   |} :> _

type Vault =
    { Name : ResourceName
      Location : Location
      TenantId : string
      Sku : string
      Uri : string option
      EnabledForDeployment : bool option
      EnabledForDiskEncryption : bool option
      EnabledForTemplateDeployment : bool option
      EnableSoftDelete : bool option
      CreateMode : string option
      EnablePurgeProtection : bool option
      AccessPolicies :
        {| ObjectId : string
           ApplicationId : string option
           Permissions :
            {| Keys : string array
               Secrets : string array
               Certificates : string array
               Storage : string array |}
        |} array
      DefaultAction : string option
      Bypass: string option
      IpRules : string list
      VnetRules : string list }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonValue =
            {| ``type``= "Microsoft.KeyVault/vaults"
               name = this.Name.Value
               apiVersion = "2018-02-14"
               location = this.Location.ArmValue
               properties =
                 {| tenantId = this.TenantId
                    sku = {| name = this.Sku; family = "A" |}
                    enabledForDeployment = this.EnabledForDeployment |> Option.toNullable
                    enabledForDiskEncryption = this.EnabledForDiskEncryption |> Option.toNullable
                    enabledForTemplateDeployment = this.EnabledForTemplateDeployment |> Option.toNullable
                    enablePurgeProtection = this.EnablePurgeProtection |> Option.toNullable
                    createMode = this.CreateMode |> Option.toObj
                    vaultUri = this.Uri |> Option.toObj
                    accessPolicies =
                         [| for policy in this.AccessPolicies do
                             {| objectId = policy.ObjectId
                                tenantId = this.TenantId
                                applicationId = policy.ApplicationId |> Option.toObj
                                permissions =
                                 {| keys = policy.Permissions.Keys
                                    storage = policy.Permissions.Storage
                                    certificates = policy.Permissions.Certificates
                                    secrets = policy.Permissions.Secrets |}
                             |}
                         |]
                    networkAcls =
                     {| defaultAction = this.DefaultAction |> Option.toObj
                        bypass = this.Bypass |> Option.toObj
                        ipRules = this.IpRules
                        virtualNetworkRules = this.VnetRules |}
                 |}
             |} :> _

