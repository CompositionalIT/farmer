[<AutoOpen>]
module Farmer.Resources.KeyVault

open Farmer
open System

type [<RequireQualifiedAccess>] KeyPermissions = Encrypt | Decrypt | WrapKey | UnwrapKey | Sign | Verify | Get | List | Create | Update | Import | Delete | Backup | Restore | Recover | Purge
type [<RequireQualifiedAccess>] SecretPermissions = Get | List | Set | Delete | Backup | Restore | Recover | Purge
type [<RequireQualifiedAccess>] CertificatePermissions = Get | List | Delete | Create | Import | Update | ManageContacts | GetIssuers | ListIssuers | SetIssuers | DeleteIssuers | ManageIssuers | Recover | Purge | Backup | Restore
type [<RequireQualifiedAccess>] StoragePermissions = Get | List | Delete | Set | Update | RegenerateKey | Recover | Purge | Backup | Restore | SetSas | ListSas | GetSas | DeleteSas
type AccessPolicy =
    { ObjectId : string
      ApplicationId : Guid
      Permissions :
        {| Keys : KeyPermissions Set
           Secrets : SecretPermissions Set
           Certificates : CertificatePermissions Set
           Storage : StoragePermissions Set |}
    }
type NonEmptyList<'T> = 'T * 'T List

type CreateMode = Recover of NonEmptyList<AccessPolicy> | Default of AccessPolicy list
type SoftDeletionFlag = SoftDeletionAndPurgeProtectionEnabled | SoftDeletionEnabled | SoftDeletionDisabled
type KeyVaultSku = Standard | Premium
type KeyVaultConfig =
    { Name : ResourceName
      Access :
        {| /// Specifies whether Azure Virtual Machines are permitted to retrieve certificates stored as secrets from the key vault.
           VirtualMachineAccess : FeatureFlag option
           /// Specifies whether Azure Resource Manager is permitted to retrieve secrets from the key vault.
           ResourceManagerAccess : FeatureFlag option
           /// Specifies whether Azure Disk Encryption is permitted to retrieve secrets from the vault and unwrap keys.
           AzureDiskEncryptionAccess : FeatureFlag option
           /// Specifies whether Soft Deletion is enabled for the vault
           SoftDelete : SoftDeletionFlag option |}
      Sku : KeyVaultSku
      Policies : CreateMode option
      /// Specifies the Azure Active Directory tenant ID that should be used for authenticating requests to the key vault.
      TenantId : Guid
      Uri : Uri option }


module Converters =
    let inline toStringArray theSet = theSet |> Set.map(fun s -> s.ToString().ToLower()) |> Set.toArray
    let inline maybeBoolean (f:FeatureFlag) = f.AsBoolean
    let keyVault location (kvc:KeyVaultConfig) : Models.KeyVault =
        { Name = kvc.Name
          Location = location          
          TenantId = kvc.TenantId.ToString()
          Sku = kvc.Sku.ToString().ToLower()

          EnabledForTemplateDeployment = kvc.Access.ResourceManagerAccess |> Option.map maybeBoolean
          EnabledForDiskEncryption = kvc.Access.AzureDiskEncryptionAccess |> Option.map maybeBoolean
          EnabledForDeployment = kvc.Access.VirtualMachineAccess |> Option.map maybeBoolean
          EnableSoftDelete =
            match kvc.Access.SoftDelete with
            | None -> None
            | Some SoftDeletionDisabled -> Some false
            | Some SoftDeletionAndPurgeProtectionEnabled -> Some true
            | Some SoftDeletionEnabled -> Some true          
          EnablePurgeProtection =
            match kvc.Access.SoftDelete with
            | None -> None
            | Some SoftDeletionAndPurgeProtectionEnabled -> Some true
            | Some SoftDeletionDisabled | Some SoftDeletionEnabled -> Some false
          CreateMode =
            match kvc.Policies with
            | None -> None
            | Some (Recover _) -> Some "recover"
            | Some (Default _) -> Some "default"
          AccessPolicies =
            let policies =
                match kvc.Policies with                
                | None -> []
                | Some (Recover(policy, secondaryPolicies)) -> policy :: secondaryPolicies
                | Some (Default policies) -> policies
            [| for policy in policies do
                {| ObjectId = policy.ObjectId
                   Permissions =
                    {| Certificates = policy.Permissions.Certificates |> toStringArray
                       Storage = policy.Permissions.Storage |> toStringArray
                       Keys = policy.Permissions.Keys |> toStringArray
                       Secrets = policy.Permissions.Secrets |> toStringArray |}
                |}
            |]
          Uri = kvc.Uri |> Option.map string
          DefaultAction = "AzureServices"
          Bypass = None }