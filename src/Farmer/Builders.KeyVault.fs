[<AutoOpen>]
module Farmer.Resources.KeyVault

open Farmer
open System
open Arm.KeyVault
open Vaults

type [<RequireQualifiedAccess>] Key = Encrypt | Decrypt | WrapKey | UnwrapKey | Sign | Verify | Get | List | Create | Update | Import | Delete | Backup | Restore | Recover | Purge
type [<RequireQualifiedAccess>] Secret = Get | List | Set | Delete | Backup | Restore | Recover | Purge
type [<RequireQualifiedAccess>] Certificate = Get | List | Delete | Create | Import | Update | ManageContacts | GetIssuers | ListIssuers | SetIssuers | DeleteIssuers | ManageIssuers | Recover | Purge | Backup | Restore
type [<RequireQualifiedAccess>] Storage = Get | List | Delete | Set | Update | RegenerateKey | Recover | Purge | Backup | Restore | SetSas | ListSas | GetSas | DeleteSas

let private makeAll<'TUnion> =
    FSharp.Reflection.FSharpType.GetUnionCases(typeof<'TUnion>)
    |> Array.map(fun t -> FSharp.Reflection.FSharpValue.MakeUnion(t, null) :?> 'TUnion)
    |> Array.toList

module Key =
    let All = makeAll<Key>
module Secret =
    let All = makeAll<Secret>
module Certificate =
    let All = makeAll<Certificate>
module Storage =
    let All = makeAll<Storage>

type AccessPolicy =
    { ObjectId : Guid
      ApplicationId : Guid option
      Permissions :
        {| Keys : Key Set
           Secrets : Secret Set
           Certificates : Certificate Set
           Storage : Storage Set |}
    }
type NonEmptyList<'T> = 'T * 'T List

type CreateMode = Recover of NonEmptyList<AccessPolicy> | Default of AccessPolicy list | Unspecified of AccessPolicy list
type SoftDeletionMode = SoftDeleteWithPurgeProtection | SoftDeletionOnly
[<RequireQualifiedAccess>]
type KeyVaultSku = Standard | Premium
type KeyVaultSettings =
    { /// Specifies whether Azure Virtual Machines are permitted to retrieve certificates stored as secrets from the key vault.
      VirtualMachineAccess : FeatureFlag option
      /// Specifies whether Azure Resource Manager is permitted to retrieve secrets from the key vault.
      ResourceManagerAccess : FeatureFlag option
      /// Specifies whether Azure Disk Encryption is permitted to retrieve secrets from the vault and unwrap keys.
      AzureDiskEncryptionAccess : FeatureFlag option
      /// Specifies whether Soft Deletion is enabled for the vault
      SoftDelete : SoftDeletionMode option }

type Bypass = AzureServices | NoTraffic
type DefaultAction = Allow | Deny
type NetworkAcl =
    { IpRules : string list
      VnetRules : string list
      DefaultAction : DefaultAction option
      Bypass : Bypass option }

type SecretConfig =
    { Key : string
      Value : SecretValue
      ContentType : string option
      Enabled : bool option
      ActivationDate : DateTime option
      ExpirationDate : DateTime option
      Dependencies : ResourceName list }
    static member Create key =
        { Key = key
          Value = ParameterSecret(SecureParameter key)
          ContentType = None
          Enabled = None
          ActivationDate = None
          ExpirationDate = None
          Dependencies = [] }
    static member Create (key, expression, resourceOwner) =
        { SecretConfig.Create key with
            Value = ExpressionSecret expression
            Dependencies = [ resourceOwner ] }

let private ``1970`` = DateTime(1970,1,1,0,0,0)
let private totalSecondsSince1970 (d:DateTime) = (d.Subtract ``1970``).TotalSeconds |> int
let inline private toStringArray theSet = theSet |> Set.map(fun s -> s.ToString().ToLower()) |> Set.toArray
let inline private maybeBoolean (f:FeatureFlag) = f.AsBoolean
type KeyVaultConfig =
    { Name : ResourceName
      TenantId : Guid
      Access : KeyVaultSettings
      Sku : KeyVaultSku
      Policies : CreateMode
      NetworkAcl : NetworkAcl
      Uri : Uri option
      Secrets : SecretConfig list }
    interface IResourceBuilder with
        member kvc.BuildResources location _ = [
            let keyVault =
                { Name = kvc.Name
                  Location = location
                  TenantId = kvc.TenantId.ToString()
                  Sku = kvc.Sku.ToString().ToLower()

                  EnabledForTemplateDeployment = kvc.Access.ResourceManagerAccess |> Option.map maybeBoolean
                  EnabledForDiskEncryption = kvc.Access.AzureDiskEncryptionAccess |> Option.map maybeBoolean
                  EnabledForDeployment = kvc.Access.VirtualMachineAccess |> Option.map maybeBoolean
                  EnableSoftDelete =
                    match kvc.Access.SoftDelete with
                    | None ->
                        None
                    | Some SoftDeleteWithPurgeProtection
                    | Some SoftDeletionOnly ->
                        Some true
                  EnablePurgeProtection =
                    match kvc.Access.SoftDelete with
                    | None
                    | Some SoftDeletionOnly ->
                        None
                    | Some SoftDeleteWithPurgeProtection ->
                        Some true
                  CreateMode =
                    match kvc.Policies with
                    | Unspecified _ -> None
                    | Recover _ -> Some "recover"
                    | Default _ -> Some "default"
                  AccessPolicies =
                    let policies =
                        match kvc.Policies with
                        | Unspecified policies -> policies
                        | Recover(policy, secondaryPolicies) -> policy :: secondaryPolicies
                        | Default policies -> policies
                    [| for policy in policies do
                        {| ObjectId = string policy.ObjectId
                           ApplicationId = policy.ApplicationId |> Option.map string
                           Permissions =
                            {| Certificates = policy.Permissions.Certificates |> toStringArray
                               Storage = policy.Permissions.Storage |> toStringArray
                               Keys = policy.Permissions.Keys |> toStringArray
                               Secrets = policy.Permissions.Secrets |> toStringArray |}
                        |}
                    |]
                  Uri = kvc.Uri |> Option.map string
                  DefaultAction = kvc.NetworkAcl.DefaultAction |> Option.map string
                  Bypass = kvc.NetworkAcl.Bypass |> Option.map string
                  IpRules = kvc.NetworkAcl.IpRules
                  VnetRules = kvc.NetworkAcl.VnetRules }

            keyVault
            for secret in kvc.Secrets do
                { ParentKeyVault = kvc.Name
                  Name = sprintf "%s/%s" kvc.Name.Value secret.Key |> ResourceName
                  Value = secret.Value
                  ContentType = secret.ContentType
                  Enabled = secret.Enabled |> Option.toNullable
                  ActivationDate = secret.ActivationDate |> Option.map totalSecondsSince1970 |> Option.toNullable
                  ExpirationDate = secret.ExpirationDate |> Option.map totalSecondsSince1970 |> Option.toNullable
                  Location = location
                  Dependencies = secret.Dependencies }
        ]

type AccessPolicyBuilder() =
    member __.Yield _ =
        { ObjectId = Guid.Empty
          ApplicationId = None
          Permissions = {| Keys = Set.empty; Secrets = Set.empty; Certificates = Set.empty; Storage = Set.empty |} }
    /// Sets the Object ID of the permission set.
    [<CustomOperation "object_id">]
    member __.ObjectId(state:AccessPolicy, objectId) = { state with ObjectId = objectId }
    member __.ObjectId(state:AccessPolicy, objectId) = { state with ObjectId = Guid.Parse objectId }
    /// Sets the Application ID of the permission set.
    [<CustomOperation "application_id">]
    member __.ApplicationId(state:AccessPolicy, applicationId) = { state with ApplicationId = Some applicationId }
    /// Sets the Key permissions of the permission set.
    [<CustomOperation "key_permissions">]
    member __.SetKeyPermissions(state:AccessPolicy, permissions) = { state with Permissions = {| state.Permissions with Keys = Set permissions |} }
    /// Sets the Storage permissions of the permission set.
    [<CustomOperation "storage_permissions">]
    member __.SetStoragePermissions(state:AccessPolicy, permissions) = { state with Permissions = {| state.Permissions with Storage = Set permissions |} }
    /// Sets the Secret permissions of the permission set.
    [<CustomOperation "secret_permissions">]
    member __.SetSecretPermissions(state:AccessPolicy, permissions) = { state with Permissions = {| state.Permissions with Secrets = Set permissions |} }
    /// Sets the Certificate permissions of the permission set.
    [<CustomOperation "certificate_permissions">]
    member __.SetCertificatePermissions(state:AccessPolicy, permissions) = { state with Permissions = {| state.Permissions with Certificates = Set permissions |} }

[<RequireQualifiedAccess>]
type SimpleCreateMode = Recover | Default
type KeyVaultBuilderState =
    { Name : ResourceName
      Access : KeyVaultSettings
      Sku : KeyVaultSku
      TenantId : Guid
      NetworkAcl : NetworkAcl
      CreateMode : SimpleCreateMode option
      Policies : AccessPolicy list
      Uri : Uri option
      Secrets : SecretConfig list }

type KeyVaultBuilder() =
    member __.Yield (_:unit) =
        { Name = ResourceName.Empty
          TenantId = Guid.Empty
          Access = { VirtualMachineAccess = None; ResourceManagerAccess = None; AzureDiskEncryptionAccess = None; SoftDelete = None }
          Sku = KeyVaultSku.Standard
          NetworkAcl = { IpRules = []; VnetRules = []; Bypass = None; DefaultAction = None }
          Policies = []
          CreateMode = None
          Uri = None
          Secrets = [] }

    member __.Run(state:KeyVaultBuilderState) : KeyVaultConfig =
        { Name = state.Name
          Access = state.Access
          Sku = state.Sku
          NetworkAcl = state.NetworkAcl
          TenantId = state.TenantId
          Policies =
            match state.CreateMode, state.Policies with
            | None, policies -> Unspecified policies
            | Some SimpleCreateMode.Default, policies -> Default policies
            | Some SimpleCreateMode.Recover, primary :: secondary -> Recover(primary, secondary)
            | Some SimpleCreateMode.Recover, [] -> failwith "Setting the creation mode to Recover requires at least one access policy. Use the accessPolicy builder to create a policy, and add it to the vault configuration using add_access_policy."
          Secrets = state.Secrets
          Uri = state.Uri }
    /// Sets the name of the vault.
    [<CustomOperation "name">]
    member __.Name(state:KeyVaultBuilderState, name) = { state with Name = name }
    member this.Name(state:KeyVaultBuilderState, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the vault.
    [<CustomOperation "sku">]
    member __.Sku(state:KeyVaultBuilderState, sku) = { state with Sku = sku }
    /// Sets the Tenant ID of the vault.
    [<CustomOperation "tenant_id">]
    member __.SetTenantId(state:KeyVaultBuilderState, tenantId) = { state with TenantId = tenantId }
    member __.SetTenantId(state:KeyVaultBuilderState, tenantId) = { state with TenantId = Guid.Parse tenantId }
    /// Allows VM access to the vault.
    [<CustomOperation "enable_vm_access">]
    member __.EnableVmAccess(state:KeyVaultBuilderState) = { state with Access = { state.Access with VirtualMachineAccess = Some Enabled } }
    /// Disallows VM access to the vault.
    [<CustomOperation "disable_vm_access">]
    member __.DisableVmAccess(state:KeyVaultBuilderState) = { state with Access = { state.Access with VirtualMachineAccess = Some Disabled } }
    /// Allows Resource Manager access to the vault.
    [<CustomOperation "enable_resource_manager_access">]
    member __.EnableResourceManagerAccess(state:KeyVaultBuilderState) = { state with Access = { state.Access with ResourceManagerAccess = Some Enabled } }
    /// Disallows Resource Manager access to the vault.
    [<CustomOperation "disable_resource_manager_access">]
    member __.DisableResourceManagerAccess(state:KeyVaultBuilderState) = { state with Access = { state.Access with ResourceManagerAccess = Some Disabled } }
    /// Allows Azure Disk Encyption service access to the vault.
    [<CustomOperation "enable_disk_encryption_access">]
    member __.EnableDiskEncryptionAccess(state:KeyVaultBuilderState) = { state with Access = { state.Access with AzureDiskEncryptionAccess = Some Enabled } }
    /// Disallows Azure Disk Encyption service access to the vault.
    [<CustomOperation "disable_disk_encryption_access">]
    member __.DisableDiskEncryptionAccess(state:KeyVaultBuilderState) = { state with Access = { state.Access with AzureDiskEncryptionAccess = Some Disabled } }
    /// Enables VM access to the vault.
    [<CustomOperation "enable_soft_delete">]
    member __.EnableSoftDeletion(state:KeyVaultBuilderState) = { state with Access = { state.Access with SoftDelete = Some SoftDeletionOnly } }
    /// Disables VM access to the vault.
    [<CustomOperation "enable_soft_delete_with_purge_protection">]
    member __.EnableSoftDeletionWithPurgeProtection(state:KeyVaultBuilderState) = { state with Access = { state.Access with SoftDelete = Some SoftDeleteWithPurgeProtection } }
    /// Sets the URI of the vault.
    [<CustomOperation "uri">]
    member __.Uri(state:KeyVaultBuilderState, uri) = { state with Uri = uri }
    /// Sets the Creation Mode to Recovery.
    [<CustomOperation "enable_recovery_mode">]
    member __.EnableRecoveryMode(state:KeyVaultBuilderState) = { state with CreateMode = Some SimpleCreateMode.Recover }
    /// Sets the Creation Mode to Default.
    [<CustomOperation "disable_recovery_mode">]
    member __.DisableRecoveryMode(state:KeyVaultBuilderState) = { state with CreateMode = Some SimpleCreateMode.Default }
    /// Adds an access policy to the vault.
    [<CustomOperation "add_access_policy">]
    member __.AddAccessPolicy(state:KeyVaultBuilderState, accessPolicy) = { state with Policies = accessPolicy :: state.Policies }
    // Allows Azure traffic can bypass network rules.
    [<CustomOperation "enable_azure_services_bypass">]
    member __.EnableBypass(state:KeyVaultBuilderState) = { state with NetworkAcl = { state.NetworkAcl with Bypass = Some AzureServices } }
    // Disallows Azure traffic can bypass network rules.
    [<CustomOperation "disable_azure_services_bypass">]
    member __.DisableBypass(state:KeyVaultBuilderState) = { state with NetworkAcl = { state.NetworkAcl with Bypass = Some NoTraffic } }
    // Allow traffic if no rule from ipRules and virtualNetworkRules match. This is only used after the bypass property has been evaluated.
    [<CustomOperation "allow_default_traffic">]
    member __.AllowDefaultTraffic(state:KeyVaultBuilderState) = { state with NetworkAcl = { state.NetworkAcl with DefaultAction = Some Allow } }
    // Deny traffic when no rule from ipRules and virtualNetworkRules match. This is only used after the bypass property has been evaluated.
    [<CustomOperation "deny_default_traffic">]
    member __.DenyDefaultTraffic(state:KeyVaultBuilderState) = { state with NetworkAcl = { state.NetworkAcl with DefaultAction = Some Deny } }
    // Adds an IP address rule. This can be an IPv4 address range in CIDR notation, such as '124.56.78.91' (simple IP address) or '124.56.78.0/24' (all addresses that start with 124.56.78).
    [<CustomOperation "add_ip_rule">]
    member __.AddIpRule(state:KeyVaultBuilderState, ipRule) = { state with NetworkAcl = { state.NetworkAcl with IpRules = ipRule :: state.NetworkAcl.IpRules } }
    // Adds a virtual network rule. This is the full resource id of a vnet subnet, such as '/subscriptions/subid/resourceGroups/rg1/providers/Microsoft.Network/virtualNetworks/test-vnet/subnets/subnet1'.
    [<CustomOperation "add_vnet_rule">]
    member __.AddVnetRule(state:KeyVaultBuilderState, vnetRule) = { state with NetworkAcl = { state.NetworkAcl with VnetRules = vnetRule :: state.NetworkAcl.VnetRules } }
    [<CustomOperation "add_secret">]
    member __.AddSecret(state:KeyVaultBuilderState, key) = { state with Secrets = SecretConfig.Create key :: state.Secrets }
    member __.AddSecret(state:KeyVaultBuilderState, (key, value, resourceName)) = { state with Secrets = SecretConfig.Create(key, value, resourceName) :: state.Secrets }
    member __.AddSecret(state:KeyVaultBuilderState, key) = { state with Secrets = key :: state.Secrets }

type SecretBuilder() =
    member __.Yield (_:unit) = SecretConfig.Create ""
    [<CustomOperation "name">]
    member __.Name(state:SecretConfig, name) = { state with Key = name; Value = ParameterSecret(SecureParameter name) }
    [<CustomOperation "value">]
    member __.Value(state:SecretConfig, value) = { state with Value = ExpressionSecret value }
    [<CustomOperation "content_type">]
    member __.ContentType(state:SecretConfig, contentType) = { state with ContentType = Some contentType }
    [<CustomOperation "enable_secret">]
    member __.Enabled(state:SecretConfig) = { state with Enabled = Some true }
    [<CustomOperation "disable_secret">]
    member __.Disabled(state:SecretConfig) = { state with Enabled = Some false }
    [<CustomOperation "activation_date">]
    member __.ActivationDate(state:SecretConfig, activationDate) = { state with ActivationDate = Some activationDate }
    [<CustomOperation "expiration_date">]
    member __.ExpirationDate(state:SecretConfig, expirationDate) = { state with ExpirationDate = Some expirationDate }
    [<CustomOperation "depends_on">]
    member __.DependsOn(state:SecretConfig, resourceName) = { state with Dependencies = resourceName :: state.Dependencies }

let secret = SecretBuilder()
let accessPolicy = AccessPolicyBuilder()
let keyVault = KeyVaultBuilder()