[<AutoOpen>]
module Farmer.Builders.KeyVault

open Farmer
open Farmer.KeyVault
open Farmer.Arm.KeyVault
open System
open Vaults

type AccessPolicyConfig = {
    ObjectId: ArmExpression
    ApplicationId: Guid option
    Permissions: {|
        Keys: KeyVault.Key Set
        Secrets: KeyVault.Secret Set
        Certificates: Certificate Set
        Storage: Storage Set
    |}
}

type CreateMode =
    | Recover of NonEmptyList<AccessPolicyConfig>
    | Default of AccessPolicyConfig list
    | Unspecified of AccessPolicyConfig list

type KeyVaultConfigSettings = {
    /// Specifies whether Azure Virtual Machines are permitted to retrieve certificates stored as secrets from the key vault.
    VirtualMachineAccess: FeatureFlag option
    /// Specifies whether Azure Resource Manager is permitted to retrieve secrets from the key vault.
    ResourceManagerAccess: FeatureFlag option
    /// Specifies whether Azure Disk Encryption is permitted to retrieve secrets from the vault and unwrap keys.
    AzureDiskEncryptionAccess: FeatureFlag option
    /// Specifies whether Soft Deletion is enabled for the vault
    SoftDelete: SoftDeletionMode option
    /// Specifies whether Azure role based authorization is used for data retrieval instead of any access policies on the key vault.
    RbacAuthorization: FeatureFlag option
}

type NetworkAcl = {
    IpRules: string list
    VnetRules: string list
    DefaultAction: DefaultAction option
    Bypass: Bypass option
}

type SecretConfig = {
    SecretName: string
    Vault: LinkedResource option
    Value: SecretValue
    ContentType: string option
    Enabled: bool option
    ActivationDate: DateTime option
    ExpirationDate: DateTime option
    Dependencies: ResourceId Set
    Tags: Map<string, string>
} with

    static member internal createUnsafe key = {
        SecretName = key
        Vault = None
        Value = ParameterSecret(SecureParameter key)
        ContentType = None
        Enabled = None
        ActivationDate = None
        ExpirationDate = None
        Dependencies = Set.empty
        Tags = Map.empty
    }

    static member private allowedKeyNameCharRules = [ Char.IsLetterOrDigit; (=) '-' ]

    static member internal sanitizeKeyName settingKey =
        if SecretConfig.isValid settingKey then
            settingKey
        else
            settingKey
            |> Seq.map (fun c ->
                if (SecretConfig.allowedKeyNameCharRules |> Seq.exists (fun r -> r c)) then
                    c
                else
                    '-')
            |> String.Concat

    static member internal isValid key =
        let charRulesPassed =
            key
            |> Seq.forall (fun c -> SecretConfig.allowedKeyNameCharRules |> Seq.exists (fun r -> r c))

        let stringRulesPassed =
            [ (fun l -> String.length l <= 127); String.IsNullOrWhiteSpace >> not ]
            |> Seq.forall (fun r -> r key)

        (charRulesPassed && stringRulesPassed)

    static member internal validate key =
        if not (SecretConfig.isValid key) then
            raiseFarmer
                $"Key Vault key names must be a 1-127 character string, starting with a letter and containing only 0-9, a-z, A-Z, and -. '{key}' is invalid."
        else
            ()

    static member create(secretName: string) =
        SecretConfig.validate secretName
        SecretConfig.createUnsafe secretName

    static member create(secretName, expression) = {
        SecretConfig.create secretName with
            Value = ExpressionSecret expression
            Dependencies =
                match expression.Owner with
                | Some owner -> Set.ofList [ owner ]
                | None ->
                    raiseFarmer
                        $"The supplied ARM expression ('{expression.Value}') has no resource owner. You should explicitly set this using WithOwner(), supplying the Resource Name of the owner."
    }

    member this.ResourceName = this.Vault |> Option.map (fun x -> x.Name / this.SecretName)
    member this.ResourceId = this.ResourceName |> Option.map secrets.resourceId

    member this.CreateExpression field =
        this.ResourceId
        |> Option.map (fun id ->
            let expr = ArmExpression.reference id
            expr.Map(fun e -> $"{e}.%s{field}"))

    member this.SecretUri = this.CreateExpression "secretUri"
    member this.SecretUriWithVersion = this.CreateExpression "secretUriWithVersion"

    static member private HandleNoVault() =
        failwith "Secret must be linked to a vault in order to add it to a deployment"

    interface IBuilder with
        member this.ResourceId =
            match this.ResourceId with
            | None -> SecretConfig.HandleNoVault()
            | Some id -> id

        member this.BuildResources location = [
            match this.ResourceName with
            | None -> SecretConfig.HandleNoVault()
            | Some name -> {
                Name = name
                Value = this.Value
                ContentType = this.ContentType
                Enabled = this.Enabled
                ActivationDate = this.ActivationDate
                ExpirationDate = this.ExpirationDate
                Location = location
                Dependencies =
                    match this.Vault with
                    | Some(Managed id) -> this.Dependencies.Add id
                    | Some(Unmanaged _) -> this.Dependencies
                    | None -> SecretConfig.HandleNoVault()
                Tags = this.Tags
              }
        ]

type KeyConfig = {
    KeyName: ResourceName
    Vault: LinkedResource option
    Enabled: bool option
    ActivationDate: DateTime option
    ExpirationDate: DateTime option
    KeyOps: KeyOperation list
    KTY: KeyType
    Dependencies: ResourceId Set
    Tags: Map<string, string>
} with

    member private this.vault =
        match this.Vault with
        | Some vault -> vault
        | None -> raiseFarmer "Key must be linked to a vault"

    interface IBuilder with
        member this.ResourceId =
            let resId = keys.resourceId (this.vault.Name / this.KeyName)

            {
                resId with
                    Subscription = this.vault.ResourceId.Subscription
                    ResourceGroup = this.vault.ResourceId.ResourceGroup
            }

        member this.BuildResources location = [
            {
                KeyName = this.KeyName
                VaultName = this.vault.Name
                KeyOps = this.KeyOps
                KTY = this.KTY
                Location = location
                Enabled = this.Enabled
                ActivationDate = this.ActivationDate
                ExpirationDate = this.ExpirationDate
                Dependencies =
                    match this.vault with
                    | Managed id -> this.Dependencies.Add id
                    | Unmanaged _ -> this.Dependencies
                Tags = this.Tags
            }
        ]

type KeyVaultConfig = {
    Name: ResourceName
    TenantId: ArmExpression
    Access: KeyVaultConfigSettings
    Sku: Sku
    Policies: CreateMode
    NetworkAcl: NetworkAcl
    Uri: Uri option
    Keys: KeyConfig list
    Secrets: SecretConfig list
    DisablePublicNetworkAccess: FeatureFlag option
    Tags: Map<string, string>
} with

    member this.ResourceId = vaults.resourceId this.Name

    /// References the Key Vault URI after deployment.
    member this.VaultUri =
        let kvUriId = ResourceId.create (vaults, this.Name)
        $"reference({kvUriId.ArmExpression.Value}).vaultUri" |> ArmExpression.create

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location = [
            let keyVault = {
                Name = this.Name
                Location = location
                TenantId = this.TenantId |> ArmExpression.Eval
                Sku = this.Sku
                TemplateDeployment = this.Access.ResourceManagerAccess
                DiskEncryption = this.Access.AzureDiskEncryptionAccess
                Deployment = this.Access.VirtualMachineAccess
                RbacAuthorization = this.Access.RbacAuthorization
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
                        | Recover list -> list.Value
                        | Default policies -> policies

                    [
                        for policy in policies do
                            {|
                                ObjectId = policy.ObjectId
                                ApplicationId = policy.ApplicationId
                                Permissions = {|
                                    Certificates = policy.Permissions.Certificates
                                    Storage = policy.Permissions.Storage
                                    Keys = policy.Permissions.Keys
                                    Secrets = policy.Permissions.Secrets
                                |}
                            |}
                    ]
                Uri = this.Uri
                DefaultAction = this.NetworkAcl.DefaultAction
                Bypass = this.NetworkAcl.Bypass
                IpRules = this.NetworkAcl.IpRules
                VnetRules = this.NetworkAcl.VnetRules
                DisablePublicNetworkAccess = this.DisablePublicNetworkAccess
                Tags = this.Tags
            }

            keyVault

            yield!
                this.Keys
                |> List.map (fun s -> {
                    s with
                        Vault = Some(Managed this.ResourceId)
                })
                |> List.collect (fun s -> (s :> IBuilder).BuildResources location)

            yield!
                this.Secrets
                |> List.map (fun s -> {
                    s with
                        Vault = Some(Managed this.ResourceId)
                })
                |> List.collect (fun s -> (s :> IBuilder).BuildResources location)
        ]

type AccessPolicyBuilder() =
    member _.Yield _ = {
        ObjectId = ArmExpression.create (string Guid.Empty)
        ApplicationId = None
        Permissions = {|
            Keys = Set.empty
            Secrets = Set.empty
            Certificates = Set.empty
            Storage = Set.empty
        |}
    }

    /// Sets the Object ID of the permission set.
    [<CustomOperation "object_id">]
    member _.ObjectId(state: AccessPolicyConfig, objectId: ArmExpression) = { state with ObjectId = objectId }

    member this.ObjectId(state: AccessPolicyConfig, objectId: Guid) =
        this.ObjectId(state, ArmExpression.create $"string('{objectId}')")

    member this.ObjectId(state: AccessPolicyConfig, (ObjectId objectId)) = this.ObjectId(state, objectId)

    member this.ObjectId(state: AccessPolicyConfig, objectId: string) =
        this.ObjectId(state, Guid.Parse objectId)

    member this.ObjectId(state: AccessPolicyConfig, PrincipalId expression) = this.ObjectId(state, expression)

    /// Sets the Application ID of the permission set.
    [<CustomOperation "application_id">]
    member _.ApplicationId(state: AccessPolicyConfig, applicationId) = {
        state with
            ApplicationId = Some applicationId
    }

    /// Sets the Key permissions of the permission set.
    [<CustomOperation "key_permissions">]
    member _.SetKeyPermissions(state: AccessPolicyConfig, permissions) = {
        state with
            Permissions = {|
                state.Permissions with
                    Keys = set permissions
            |}
    }

    /// Sets the Storage permissions of the permission set.
    [<CustomOperation "storage_permissions">]
    member _.SetStoragePermissions(state: AccessPolicyConfig, permissions) = {
        state with
            Permissions = {|
                state.Permissions with
                    Storage = set permissions
            |}
    }

    /// Sets the Secret permissions of the permission set.
    [<CustomOperation "secret_permissions">]
    member _.SetSecretPermissions(state: AccessPolicyConfig, permissions) = {
        state with
            Permissions = {|
                state.Permissions with
                    Secrets = set permissions
            |}
    }

    /// Sets the Certificate permissions of the permission set.
    [<CustomOperation "certificate_permissions">]
    member _.SetCertificatePermissions(state: AccessPolicyConfig, permissions) = {
        state with
            Permissions = {|
                state.Permissions with
                    Certificates = set permissions
            |}
    }

let accessPolicy = AccessPolicyBuilder()

type AccessPolicy =
    static let createFilter searchField values =
        let query =
            values
            |> Seq.map (fun value -> $"{searchField} eq '%s{value}'")
            |> String.concat " or "

        $"\"{query}\""

    static let handleSearchResponse =
        Serialization.ofJson<{| DisplayName: string; Id: Guid |} array>
        >> Array.map (fun r -> {| r with Id = ObjectId r.Id |})

    /// Quickly creates an access policy for the supplied Principal. If no permissions are supplied, defaults to GET and LIST.
    static member create(principal: PrincipalId, ?permissions) = accessPolicy {
        object_id principal
        secret_permissions (permissions |> Option.defaultValue Secret.ReadSecrets)
    }

    /// Quickly creates an access policy for the supplied Identity. If no permissions are supplied, defaults to GET and LIST.
    static member create(identity: UserAssignedIdentityConfig, ?permissions) =
        AccessPolicy.create (identity.PrincipalId, ?permissions = permissions)

    /// Quickly creates an access policy for the supplied Identity. If no permissions are supplied, defaults to GET and LIST.
    static member create(identity: Identity.SystemIdentity, ?permissions) =
        AccessPolicy.create (identity.PrincipalId, ?permissions = permissions)

    /// Quickly creates an access policy for the supplied ObjectId. If no permissions are supplied, defaults to GET and LIST.
    static member create(objectId: ObjectId, ?permissions) = accessPolicy {
        object_id objectId
        secret_permissions (permissions |> Option.defaultValue Secret.ReadSecrets)
    }

    /// Locates users in Azure Active Directory based on the supplied email addresses.
    static member findUsers emailAddresses =
        let filter = createFilter "mail" emailAddresses

        Deploy.Az.searchUsers filter
        |> Result.map handleSearchResponse
        |> Result.defaultValue Array.empty

    /// Locates groups in Azure Active Directory based on the supplied group names.
    static member findGroups groupNames =
        let filter = createFilter "displayName" groupNames

        Deploy.Az.searchGroups filter
        |> Result.map handleSearchResponse
        |> Result.defaultValue Array.empty

[<RequireQualifiedAccess>]
type SimpleCreateMode =
    | Recover
    | Default

type KeyVaultBuilderState = {
    Name: ResourceName
    Access: KeyVaultConfigSettings
    Sku: Sku
    TenantId: ArmExpression
    NetworkAcl: NetworkAcl
    CreateMode: SimpleCreateMode option
    Policies: AccessPolicyConfig list
    Uri: Uri option
    Secrets: SecretConfig list
    Keys: KeyConfig list
    DisablePublicNetworkAccess: FeatureFlag option
    Tags: Map<string, string>
}

type KeyVaultBuilder() =
    member _.Yield(_: unit) = {
        Name = ResourceName.Empty
        TenantId = Subscription.TenantId
        Access = {
            VirtualMachineAccess = None
            RbacAuthorization = None
            ResourceManagerAccess = Some Enabled
            AzureDiskEncryptionAccess = None
            SoftDelete = None
        }
        Sku = Standard
        NetworkAcl = {
            IpRules = []
            VnetRules = []
            Bypass = None
            DefaultAction = None
        }
        Policies = []
        CreateMode = None
        Uri = None
        Secrets = []
        Keys = []
        DisablePublicNetworkAccess = None
        Tags = Map.empty
    }

    member _.Run(state: KeyVaultBuilderState) : KeyVaultConfig = {
        Name = state.Name
        Access = state.Access
        Sku = state.Sku
        NetworkAcl = state.NetworkAcl
        TenantId = state.TenantId
        Policies =
            match state.CreateMode, state.Policies with
            | None, policies -> Unspecified policies
            | Some SimpleCreateMode.Default, policies -> Default policies
            | Some SimpleCreateMode.Recover, [] ->
                raiseFarmer
                    "Setting the creation mode to Recover requires at least one access policy. Use the accessPolicy builder to create a policy, and add it to the vault configuration using add_access_policy."
            | Some SimpleCreateMode.Recover, policies -> Recover(NonEmptyList.create policies)
        Keys = state.Keys
        Secrets = state.Secrets
        Uri = state.Uri
        DisablePublicNetworkAccess = state.DisablePublicNetworkAccess
        Tags = state.Tags
    }

    /// Sets the name of the vault.
    [<CustomOperation "name">]
    member _.Name(state: KeyVaultBuilderState, name) = { state with Name = name }

    member this.Name(state: KeyVaultBuilderState, name) = this.Name(state, ResourceName name)

    /// Sets the sku of the vault.
    [<CustomOperation "sku">]
    member _.Sku(state: KeyVaultBuilderState, sku) = { state with Sku = sku }

    /// Sets the Tenant ID of the vault.
    [<CustomOperation "tenant_id">]
    member _.SetTenantId(state: KeyVaultBuilderState, tenantId) = { state with TenantId = tenantId }

    member this.SetTenantId(state: KeyVaultBuilderState, tenantId) =
        this.SetTenantId(state, ArmExpression.create $"string('{tenantId}')")

    member this.SetTenantId(state: KeyVaultBuilderState, tenantId: string) =
        this.SetTenantId(state, Guid.Parse tenantId)

    /// Allows VM access to the vault.
    [<CustomOperation "enable_vm_access">]
    member _.EnableVmAccess(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    VirtualMachineAccess = Some Enabled
            }
    }

    /// Disallows VM access to the vault.
    [<CustomOperation "disable_vm_access">]
    member _.DisableVmAccess(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    VirtualMachineAccess = Some Disabled
            }
    }

    /// Allows Resource Manager access to the vault so that you can deploy secrets during ARM deployments.
    [<CustomOperation "enable_resource_manager_access">]
    member _.EnableResourceManagerAccess(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    ResourceManagerAccess = Some Enabled
            }
    }

    /// Disallows Resource Manager access to the vault.
    [<CustomOperation "disable_resource_manager_access">]
    member _.DisableResourceManagerAccess(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    ResourceManagerAccess = Some Disabled
            }
    }

    /// Allows Azure Disk Encyption service access to the vault.
    [<CustomOperation "enable_disk_encryption_access">]
    member _.EnableDiskEncryptionAccess(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    AzureDiskEncryptionAccess = Some Enabled
            }
    }

    /// Disallows Azure Disk Encyption service access to the vault.
    [<CustomOperation "disable_disk_encryption_access">]
    member _.DisableDiskEncryptionAccess(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    AzureDiskEncryptionAccess = Some Disabled
            }
    }

    /// Enables Azure role based authentication for access to key vault data.
    [<CustomOperation "enable_rbac">]
    member _.EnableRbacAuthorization(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    RbacAuthorization = Some Enabled
            }
    }

    /// Disables Azure role based authentication for access to key vault data.
    [<CustomOperation "disable_rbac">]
    member _.DisableRbacAuthorization(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    RbacAuthorization = Some Disabled
            }
    }

    /// Enables VM access to the vault.
    [<CustomOperation "enable_soft_delete">]
    member _.EnableSoftDeletion(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    SoftDelete = Some SoftDeletionOnly
            }
    }

    /// Disables VM access to the vault.
    [<CustomOperation "enable_soft_delete_with_purge_protection">]
    member _.EnableSoftDeletionWithPurgeProtection(state: KeyVaultBuilderState) = {
        state with
            Access = {
                state.Access with
                    SoftDelete = Some SoftDeleteWithPurgeProtection
            }
    }

    /// Sets the URI of the vault.
    [<CustomOperation "uri">]
    member _.Uri(state: KeyVaultBuilderState, uri) = { state with Uri = uri }

    /// Sets the Creation Mode to Recovery.
    [<CustomOperation "enable_recovery_mode">]
    member _.EnableRecoveryMode(state: KeyVaultBuilderState) = {
        state with
            CreateMode = Some SimpleCreateMode.Recover
    }

    /// Sets the Creation Mode to Default.
    [<CustomOperation "disable_recovery_mode">]
    member _.DisableRecoveryMode(state: KeyVaultBuilderState) = {
        state with
            CreateMode = Some SimpleCreateMode.Default
    }

    /// Adds an access policy to the vault.
    [<CustomOperation "add_access_policy">]
    member _.AddAccessPolicy(state: KeyVaultBuilderState, accessPolicy) = {
        state with
            Policies = accessPolicy :: state.Policies
    }

    /// Adds access policies to the vault.
    [<CustomOperation "add_access_policies">]
    member _.AddAccessPolicies(state: KeyVaultBuilderState, accessPolicies) = {
        state with
            Policies = List.append accessPolicies state.Policies
    }

    /// Allows Azure traffic can bypass network rules.
    [<CustomOperation "enable_azure_services_bypass">]
    member _.EnableBypass(state: KeyVaultBuilderState) = {
        state with
            NetworkAcl = {
                state.NetworkAcl with
                    Bypass = Some AzureServices
            }
    }

    /// Disallows Azure traffic can bypass network rules.
    [<CustomOperation "disable_azure_services_bypass">]
    member _.DisableBypass(state: KeyVaultBuilderState) = {
        state with
            NetworkAcl = {
                state.NetworkAcl with
                    Bypass = Some NoTraffic
            }
    }

    /// Allow traffic if no rule from ipRules and virtualNetworkRules match. This is only used after the bypass property has been evaluated.
    [<CustomOperation "allow_default_traffic">]
    member _.AllowDefaultTraffic(state: KeyVaultBuilderState) = {
        state with
            NetworkAcl = {
                state.NetworkAcl with
                    DefaultAction = Some Allow
            }
    }

    /// Deny traffic when no rule from ipRules and virtualNetworkRules match. This is only used after the bypass property has been evaluated.
    [<CustomOperation "deny_default_traffic">]
    member _.DenyDefaultTraffic(state: KeyVaultBuilderState) = {
        state with
            NetworkAcl = {
                state.NetworkAcl with
                    DefaultAction = Some Deny
            }
    }

    /// Adds an IP address rule. This can be an IPv4 address range in CIDR notation, such as '124.56.78.91' (simple IP address) or '124.56.78.0/24' (all addresses that start with 124.56.78).
    [<CustomOperation "add_ip_rule">]
    member _.AddIpRule(state: KeyVaultBuilderState, ipRule) = {
        state with
            NetworkAcl = {
                state.NetworkAcl with
                    IpRules = ipRule :: state.NetworkAcl.IpRules
            }
    }

    /// Adds a virtual network rule. This is the full resource id of a vnet subnet, such as '/subscriptions/subid/resourceGroups/rg1/providers/Microsoft.Network/virtualNetworks/test-vnet/subnets/subnet1'.
    [<CustomOperation "add_vnet_rule">]
    member _.AddVnetRule(state: KeyVaultBuilderState, vnetRule) = {
        state with
            NetworkAcl = {
                state.NetworkAcl with
                    VnetRules = vnetRule :: state.NetworkAcl.VnetRules
            }
    }

    /// Allows adding a key to the vault.
    [<CustomOperation "add_key">]
    member _.AddKey(state: KeyVaultBuilderState, key: KeyConfig) = { state with Keys = key :: state.Keys }

    /// Allows adding multiple keys to the vault.
    [<CustomOperation "add_keys">]
    member _.AddKeys(state: KeyVaultBuilderState, keys: KeyConfig list) = { state with Keys = state.Keys @ keys }

    /// Allows to add a secret to the vault.
    [<CustomOperation "add_secret">]
    member _.AddSecret(state: KeyVaultBuilderState, key: SecretConfig) = {
        state with
            Secrets = key :: state.Secrets
    }

    member this.AddSecret(state: KeyVaultBuilderState, key: string) =
        this.AddSecret(state, SecretConfig.create key)

    member this.AddSecret(state: KeyVaultBuilderState, (key, expression: ArmExpression)) =
        this.AddSecret(state, SecretConfig.create (key, expression))

    /// Allows to add multiple secrets to the vault.
    [<CustomOperation "add_secrets">]
    member this.AddSecrets(state: KeyVaultBuilderState, keys) =
        keys
        |> Seq.fold (fun state (key: SecretConfig) -> this.AddSecret(state, key)) state

    member this.AddSecrets(state: KeyVaultBuilderState, keys) =
        this.AddSecrets(state, keys |> Seq.map SecretConfig.create)

    member this.AddSecrets(state: KeyVaultBuilderState, items) =
        this.AddSecrets(state, items |> Seq.map (fun (key, value) -> SecretConfig.create (key, value)))

    /// Disable public network access
    [<CustomOperation "disable_public_network_access">]
    member _.DisablePublicNetworkAccess(state: KeyVaultBuilderState, ?flag: FeatureFlag) =
        let flag = defaultArg flag FeatureFlag.Enabled

        {
            state with
                DisablePublicNetworkAccess = Some flag
        }

    interface ITaggable<KeyVaultBuilderState> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

type KeyBuilder() =
    member _.Yield _ : KeyConfig = {
        KeyName = ResourceName.Empty
        Vault = None
        Enabled = None
        ActivationDate = None
        ExpirationDate = None
        KeyOps = []
        KTY = KeyType.RSA_2048
        Dependencies = Set.empty
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: KeyConfig, name) = {
        state with
            KeyName = ResourceName name
    }

    [<CustomOperation "status">]
    member _.Enabled(state: KeyConfig, featureFlag: FeatureFlag) = {
        state with
            Enabled = Some featureFlag.AsBoolean
    }

    [<CustomOperation "activation_date">]
    member _.ActivationDate(state: KeyConfig, activationDate) = {
        state with
            ActivationDate = Some activationDate
    }

    [<CustomOperation "expiration_date">]
    member _.ExpirationDate(state: KeyConfig, expirationDate) = {
        state with
            ExpirationDate = Some expirationDate
    }

    [<CustomOperation "key_operations">]
    member _.KeyOperations(state: KeyConfig, keyOperations: KeyOperation list) = { state with KeyOps = keyOperations }

    [<CustomOperation "key_type">]
    member _.KeyType(state: KeyConfig, keyType: KeyType) = { state with KTY = keyType }

    [<CustomOperation "link_to_unmanaged_keyvault">]
    member _.LinkToKeyVault(state: KeyConfig, keyVault: ResourceId) = {
        state with
            Vault = Some(Unmanaged keyVault)
    }

    member _.LinkToKeyVault(state: KeyConfig, keyVault: KeyVaultConfig) = {
        state with
            Vault = Some(Unmanaged keyVault.ResourceId)
    }

    member _.LinkToKeyVault(state: KeyConfig, keyVault: IArmResource) = {
        state with
            Vault = Some(Unmanaged keyVault.ResourceId)
    }

    interface ITaggable<KeyConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<KeyConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }


type SecretBuilder() =
    member _.Run(state: SecretConfig) =
        SecretConfig.validate state.SecretName
        state

    member _.Yield(_: unit) = SecretConfig.createUnsafe ""

    [<CustomOperation "name">]
    member _.Name(state: SecretConfig, name) = {
        state with
            SecretName = name
            Value = ParameterSecret(SecureParameter name)
    }

    [<CustomOperation "value">]
    member _.Value(state: SecretConfig, value) = {
        state with
            Value = ExpressionSecret value
    }

    [<CustomOperation "content_type">]
    member _.ContentType(state: SecretConfig, contentType) = {
        state with
            ContentType = Some contentType
    }

    [<CustomOperation "enable_secret">] // Leaving in for compatibility - should use FeatureFlag
    member _.EnableSecret(state: SecretConfig) = { state with Enabled = Some true }

    [<CustomOperation "disable_secret">] // Leaving in for compatibility - should use FeatureFlag
    member _.DisableSecret(state: SecretConfig) = { state with Enabled = Some false }

    [<CustomOperation "status">]
    member _.Enabled(state: SecretConfig, featureFlag: FeatureFlag) = {
        state with
            Enabled = Some featureFlag.AsBoolean
    }

    [<CustomOperation "activation_date">]
    member _.ActivationDate(state: SecretConfig, activationDate) = {
        state with
            ActivationDate = Some activationDate
    }

    [<CustomOperation "expiration_date">]
    member _.ExpirationDate(state: SecretConfig, expirationDate) = {
        state with
            ExpirationDate = Some expirationDate
    }

    [<CustomOperation "link_to_unmanaged_keyvault">]
    member _.LinkToKeyVault(state: SecretConfig, keyVault) = {
        state with
            Vault = Some(Unmanaged keyVault)
    }

    interface ITaggable<SecretConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<SecretConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

let key = KeyBuilder()
let secret = SecretBuilder()
let keyVault = KeyVaultBuilder()

/// Configuration for adding access policies to an existing key vault.
type KeyVaultAddPoliciesConfig = {
    KeyVault: LinkedResource option
    TenantId: string option
    AccessPolicies: AccessPolicyConfig list
} with

    interface IBuilder with
        member this.BuildResources _ =
            match this.KeyVault with
            | None ->
                raiseFarmer "Key vault policy addition must be linked to a key vault to properly assign the resourceId."
            | Some kv -> [
                {
                    VaultAddPolicies.KeyVault = kv
                    TenantId = this.TenantId
                    AccessPolicies =
                        this.AccessPolicies
                        |> List.map (fun policy -> {|
                            ObjectId = policy.ObjectId
                            ApplicationId = policy.ApplicationId
                            Permissions = {|
                                Certificates = policy.Permissions.Certificates
                                Storage = policy.Permissions.Storage
                                Keys = policy.Permissions.Keys
                                Secrets = policy.Permissions.Secrets
                            |}
                        |})
                }
              ]

        member this.ResourceId =
            match this.KeyVault with
            | None ->
                raiseFarmer "Key vault policy addition must be linked to a key vault to properly assign the resourceId."
            | Some kv -> accessPolicies.resourceId (kv.Name / (ResourceName "add"))

/// Builder for adding policies to an existing key vault.
type KeyVaultAddPoliciesBuilder() =
    member _.Yield _ = {
        KeyVault = None
        TenantId = None
        AccessPolicies = []
    }

    /// The key vault where the policies should be added.
    [<CustomOperation "key_vault">]
    member _.KeyVault(state: KeyVaultAddPoliciesConfig, kv: Vault) = {
        state with
            KeyVault = Some(Unmanaged (kv :> IArmResource).ResourceId)
    }

    member _.KeyVault(state: KeyVaultAddPoliciesConfig, kv: KeyVaultConfig) = {
        state with
            KeyVault = Some(Unmanaged (kv :> IBuilder).ResourceId)
    }

    member _.KeyVault(state: KeyVaultAddPoliciesConfig, kv: ResourceId) = {
        state with
            KeyVault = Some(Unmanaged kv)
    }

    /// Specify the tenant ID for the users or service principals being granted access.
    [<CustomOperation "tenant_id">]
    member _.TenantId(state: KeyVaultAddPoliciesConfig, tenantId: string) = { state with TenantId = Some tenantId }

    /// Access polices to add to the key vault.
    [<CustomOperation "add_access_policies">]
    member _.AddAccessPolicies(state: KeyVaultAddPoliciesConfig, accessPolicies: AccessPolicyConfig list) = {
        state with
            AccessPolicies = state.AccessPolicies @ accessPolicies
    }

let keyVaultAddPolicies = KeyVaultAddPoliciesBuilder()