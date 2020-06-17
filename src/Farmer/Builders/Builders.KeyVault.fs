[<AutoOpen>]
module Farmer.Builders.KeyVault

open Farmer
open Farmer.CoreTypes
open Farmer.KeyVault
open Farmer.Arm.KeyVault
open System
open Vaults

type NonEmptyList<'T> = 'T * 'T List
type AccessPolicyConfig =
    { ObjectId : ArmExpression
      ApplicationId : Guid option
      Permissions :
        {| Keys : Key Set
           Secrets : KeyVault.Secret Set
           Certificates : Certificate Set
           Storage : Storage Set |}
    }
type CreateMode =
    | Recover of NonEmptyList<AccessPolicyConfig>
    | Default of AccessPolicyConfig list
    | Unspecified of AccessPolicyConfig list

type KeyVaultConfigSettings =
    { /// Specifies whether Azure Virtual Machines are permitted to retrieve certificates stored as secrets from the key vault.
      VirtualMachineAccess : FeatureFlag option
      /// Specifies whether Azure Resource Manager is permitted to retrieve secrets from the key vault.
      ResourceManagerAccess : FeatureFlag option
      /// Specifies whether Azure Disk Encryption is permitted to retrieve secrets from the vault and unwrap keys.
      AzureDiskEncryptionAccess : FeatureFlag option
      /// Specifies whether Soft Deletion is enabled for the vault
      SoftDelete : SoftDeletionMode option }

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
    static member Create (key:string) =
        let charRulesPassed =
            let charRules = [ Char.IsLetterOrDigit; (=) '-' ]
            key |> Seq.forall(fun c -> charRules |> Seq.exists(fun r -> r c))
        let stringRulesPassed =
            [ (fun l -> String.length l <= 127)
              String.IsNullOrWhiteSpace >> not ]
            |> Seq.forall(fun r -> r key)

        if not (charRulesPassed && stringRulesPassed) then
            failwithf "Key Vault key names must be a 1-127 character string, starting with a letter and containing only 0-9, a-z, A-Z, and -. '%s' is invalid." key

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

type KeyVaultConfig =
    { Name : ResourceName
      TenantId : ArmExpression
      Access : KeyVaultConfigSettings
      Sku : Sku
      Policies : CreateMode
      NetworkAcl : NetworkAcl
      Uri : Uri option
      Secrets : SecretConfig list }
      interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            let keyVault =
                { Name = this.Name
                  Location = location
                  TenantId = this.TenantId |> ArmExpression.Eval
                  Sku = this.Sku
                  TemplateDeployment = this.Access.ResourceManagerAccess
                  DiskEncryption = this.Access.AzureDiskEncryptionAccess
                  Deployment = this.Access.VirtualMachineAccess
                  SoftDelete = this.Access.SoftDelete
                  CreateMode =
                    match this.Policies with
                    | Unspecified _ -> None
                    | Recover _ -> Some Arm.KeyVault.Recover
                    | Default _ -> Some Arm.KeyVault.Default
                  AccessPolicies =
                    let policies =
                        match this.Policies with
                        | Unspecified policies -> policies
                        | Recover(policy, secondaryPolicies) -> policy :: secondaryPolicies
                        | Default policies -> policies
                    [| for policy in policies do
                        {| ObjectId = policy.ObjectId
                           ApplicationId = policy.ApplicationId
                           Permissions =
                            {| Certificates = policy.Permissions.Certificates
                               Storage = policy.Permissions.Storage
                               Keys = policy.Permissions.Keys
                               Secrets = policy.Permissions.Secrets |}
                        |}
                    |]
                  Uri = this.Uri
                  DefaultAction = this.NetworkAcl.DefaultAction
                  Bypass = this.NetworkAcl.Bypass
                  IpRules = this.NetworkAcl.IpRules
                  VnetRules = this.NetworkAcl.VnetRules }

            keyVault
            for secret in this.Secrets do
                { Name = sprintf "%s/%s" this.Name.Value secret.Key |> ResourceName
                  Value = secret.Value
                  ContentType = secret.ContentType
                  Enabled = secret.Enabled
                  ActivationDate = secret.ActivationDate
                  ExpirationDate = secret.ExpirationDate
                  Location = location
                  Dependencies = this.Name :: secret.Dependencies }
        ]

type AccessPolicyBuilder() =
    member __.Yield _ =
        { ObjectId = ArmExpression (string Guid.Empty)
          ApplicationId = None
          Permissions = {| Keys = Set.empty; Secrets = Set.empty; Certificates = Set.empty; Storage = Set.empty |} }
    /// Sets the Object ID of the permission set.
    [<CustomOperation "object_id">]
    member __.ObjectId(state:AccessPolicyConfig, objectId:ArmExpression) = { state with ObjectId = objectId }
    member this.ObjectId(state:AccessPolicyConfig, objectId:Guid) = this.ObjectId(state, ArmExpression (sprintf "string('%O')" objectId))
    member this.ObjectId(state:AccessPolicyConfig, (ObjectId objectId)) = this.ObjectId(state, objectId)
    member this.ObjectId(state:AccessPolicyConfig, objectId:string) = this.ObjectId(state, Guid.Parse objectId)
    member this.ObjectId(state:AccessPolicyConfig, PrincipalId principalId) = this.ObjectId(state, principalId)
    /// Sets the Application ID of the permission set.
    [<CustomOperation "application_id">]
    member __.ApplicationId(state:AccessPolicyConfig, applicationId) = { state with ApplicationId = Some applicationId }
    /// Sets the Key permissions of the permission set.
    [<CustomOperation "key_permissions">]
    member __.SetKeyPermissions(state:AccessPolicyConfig, permissions) = { state with Permissions = {| state.Permissions with Keys = set permissions |} }
    /// Sets the Storage permissions of the permission set.
    [<CustomOperation "storage_permissions">]
    member __.SetStoragePermissions(state:AccessPolicyConfig, permissions) = { state with Permissions = {| state.Permissions with Storage = set permissions |} }
    /// Sets the Secret permissions of the permission set.
    [<CustomOperation "secret_permissions">]
    member __.SetSecretPermissions(state:AccessPolicyConfig, permissions) = { state with Permissions = {| state.Permissions with Secrets = set permissions |} }
    /// Sets the Certificate permissions of the permission set.
    [<CustomOperation "certificate_permissions">]
    member __.SetCertificatePermissions(state:AccessPolicyConfig, permissions) = { state with Permissions = {| state.Permissions with Certificates = set permissions |} }

let accessPolicy = AccessPolicyBuilder()
type AccessPolicy =
    /// Quickly creates an access policy for the supplied Principal that can GET secrets.
    static member create (principal:PrincipalId) = accessPolicy { object_id principal; secret_permissions [ Secret.Get ] }
    /// Quickly creates an access policy for the supplied ObjectId that can GET secrets.
    static member create (objectId:ObjectId) = accessPolicy { object_id objectId; secret_permissions [ Secret.Get ] }
    static member private findEntity (searchField, values, searcher) =
        values
        |> Seq.map (sprintf "%s eq '%s'" searchField)
        |> String.concat " or "
        |> sprintf "\"%s\""
        |> searcher
        |> Result.map (Newtonsoft.Json.JsonConvert.DeserializeObject<{| DisplayName : string; ObjectId : Guid|} array>)
        |> Result.toOption
        |> Option.map(Array.map(fun r -> {| r with ObjectId = ObjectId r.ObjectId |}))
        |> Option.defaultValue Array.empty
    /// Locates users in Azure Active Directory based on the supplied email addresses.
    static member findUsers emailAddresses = AccessPolicy.findEntity ("mail", emailAddresses, Deploy.Az.searchUsers)
    /// Locates groups in Azure Active Directory based on the supplied group names.
    static member findGroups groupNames = AccessPolicy.findEntity ("displayName", groupNames, Deploy.Az.searchGroups)



[<RequireQualifiedAccess>]
type SimpleCreateMode = Recover | Default
type KeyVaultBuilderState =
    { Name : ResourceName
      Access : KeyVaultConfigSettings
      Sku : Sku
      TenantId : ArmExpression
      NetworkAcl : NetworkAcl
      CreateMode : SimpleCreateMode option
      Policies : AccessPolicyConfig list
      Uri : Uri option
      Secrets : SecretConfig list }

type KeyVaultBuilder() =
    member __.Yield (_:unit) =
        { Name = ResourceName.Empty
          TenantId = Subscription.TenantId
          Access = { VirtualMachineAccess = None; ResourceManagerAccess = Some Enabled; AzureDiskEncryptionAccess = None; SoftDelete = None }
          Sku = Sku.Standard
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
    member this.SetTenantId(state:KeyVaultBuilderState, tenantId) = this.SetTenantId(state, ArmExpression (sprintf "string('%O')" tenantId))
    member this.SetTenantId(state:KeyVaultBuilderState, tenantId) = this.SetTenantId(state, Guid.Parse tenantId)
    /// Allows VM access to the vault.
    [<CustomOperation "enable_vm_access">]
    member __.EnableVmAccess(state:KeyVaultBuilderState) = { state with Access = { state.Access with VirtualMachineAccess = Some Enabled } }
    /// Disallows VM access to the vault.
    [<CustomOperation "disable_vm_access">]
    member __.DisableVmAccess(state:KeyVaultBuilderState) = { state with Access = { state.Access with VirtualMachineAccess = Some Disabled } }
    /// Allows Resource Manager access to the vault so that you can deploy secrets during ARM deployments.
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
    member __.AddAccessPolicy(state:KeyVaultBuilderState, accessPolicy) =
      { state with Policies = accessPolicy :: state.Policies }
    /// Adds access policies to the vault.
    [<CustomOperation "add_access_policies">]
    member __.AddAccessPolicies(state:KeyVaultBuilderState, accessPolicies) =
      { state with Policies = List.append accessPolicies state.Policies }
    /// Allows Azure traffic can bypass network rules.
    [<CustomOperation "enable_azure_services_bypass">]
    member __.EnableBypass(state:KeyVaultBuilderState) = { state with NetworkAcl = { state.NetworkAcl with Bypass = Some AzureServices } }
    /// Disallows Azure traffic can bypass network rules.
    [<CustomOperation "disable_azure_services_bypass">]
    member __.DisableBypass(state:KeyVaultBuilderState) = { state with NetworkAcl = { state.NetworkAcl with Bypass = Some NoTraffic } }
    /// Allow traffic if no rule from ipRules and virtualNetworkRules match. This is only used after the bypass property has been evaluated.
    [<CustomOperation "allow_default_traffic">]
    member __.AllowDefaultTraffic(state:KeyVaultBuilderState) = { state with NetworkAcl = { state.NetworkAcl with DefaultAction = Some Allow } }
    /// Deny traffic when no rule from ipRules and virtualNetworkRules match. This is only used after the bypass property has been evaluated.
    [<CustomOperation "deny_default_traffic">]
    member __.DenyDefaultTraffic(state:KeyVaultBuilderState) = { state with NetworkAcl = { state.NetworkAcl with DefaultAction = Some Deny } }
    /// Adds an IP address rule. This can be an IPv4 address range in CIDR notation, such as '124.56.78.91' (simple IP address) or '124.56.78.0/24' (all addresses that start with 124.56.78).
    [<CustomOperation "add_ip_rule">]
    member __.AddIpRule(state:KeyVaultBuilderState, ipRule) = { state with NetworkAcl = { state.NetworkAcl with IpRules = ipRule :: state.NetworkAcl.IpRules } }
    /// Adds a virtual network rule. This is the full resource id of a vnet subnet, such as '/subscriptions/subid/resourceGroups/rg1/providers/Microsoft.Network/virtualNetworks/test-vnet/subnets/subnet1'.
    [<CustomOperation "add_vnet_rule">]
    member __.AddVnetRule(state:KeyVaultBuilderState, vnetRule) = { state with NetworkAcl = { state.NetworkAcl with VnetRules = vnetRule :: state.NetworkAcl.VnetRules } }
    /// Allows to add a secret to the vault.
    [<CustomOperation "add_secret">]
    member __.AddSecret(state:KeyVaultBuilderState, key:SecretConfig) = { state with Secrets = key :: state.Secrets }
    member this.AddSecret(state:KeyVaultBuilderState, key:string) = this.AddSecret(state, SecretConfig.Create key)
    member this.AddSecret(state:KeyVaultBuilderState, (key, builder:#IBuilder, value)) = this.AddSecret(state, SecretConfig.Create(key, value, builder.DependencyName))
    member this.AddSecret(state:KeyVaultBuilderState, (key, resourceName, value)) = this.AddSecret(state, SecretConfig.Create(key, value, resourceName))

    /// Allows to add multiple secrets to the vault.
    [<CustomOperation "add_secrets">]
    member this.AddSecrets(state:KeyVaultBuilderState, keys) = keys |> Seq.fold(fun state (key:SecretConfig) -> this.AddSecret(state, key)) state
    member this.AddSecrets(state:KeyVaultBuilderState, keys) = this.AddSecrets(state, keys |> Seq.map SecretConfig.Create)
    member this.AddSecrets(state:KeyVaultBuilderState, items) = this.AddSecrets(state, items |> Seq.map(fun (key, builder:#IBuilder, value) -> SecretConfig.Create (key, value, builder.DependencyName)))
    member this.AddSecrets(state:KeyVaultBuilderState, items) = this.AddSecrets(state, items |> Seq.map(fun (key, resourceName:ResourceName, value) -> SecretConfig.Create (key, value, resourceName)))

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
    member __.DependsOn(state:SecretConfig, builder:IBuilder) = { state with Dependencies = builder.DependencyName :: state.Dependencies }
    member __.DependsOn(state:SecretConfig, resource:IArmResource) = { state with Dependencies = resource.ResourceName :: state.Dependencies }

let secret = SecretBuilder()
let keyVault = KeyVaultBuilder()