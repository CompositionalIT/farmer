[<AutoOpen>]
module Farmer.Arm.KeyVault

open Farmer
open Farmer.KeyVault
open System

let secrets = ResourceType("Microsoft.KeyVault/vaults/secrets", "2022-07-01")

let accessPolicies =
    ResourceType("Microsoft.KeyVault/vaults/accessPolicies", "2022-07-01")

let vaults = ResourceType("Microsoft.KeyVault/vaults", "2022-07-01")
let keys = ResourceType("Microsoft.keyVault/vaults/keys", "2022-07-01")

module Vaults =
    type Secret =
        {
            Name: ResourceName
            Location: Location
            Value: SecretValue
            ContentType: string option
            Enabled: bool option
            ActivationDate: DateTime option
            ExpirationDate: DateTime option
            Dependencies: ResourceId Set
            Tags: Map<string, string>
        }

        static member ``1970`` = DateTime(1970, 1, 1, 0, 0, 0)

        static member TotalSecondsSince1970(d: DateTime) =
            (d.Subtract Secret.``1970``).TotalSeconds |> int

        interface IParameters with
            member this.SecureParameters =
                match this with
                | {
                      Value = ParameterSecret secureParameter
                  } -> [ secureParameter ]
                | _ -> []

        interface IArmResource with
            member this.ResourceId = secrets.resourceId this.Name

            member this.JsonModel =
                {| secrets.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                    properties =
                        {|
                            value = this.Value.Value
                            contentType = this.ContentType |> Option.toObj
                            attributes =
                                {|
                                    enabled = this.Enabled |> Option.toNullable
                                    nbf =
                                        this.ActivationDate
                                        |> Option.map Secret.TotalSecondsSince1970
                                        |> Option.toNullable
                                    exp =
                                        this.ExpirationDate
                                        |> Option.map Secret.TotalSecondsSince1970
                                        |> Option.toNullable
                                |}
                        |}
                |}

    let private armValue armValue (a: 'a option) =
        a |> Option.map armValue |> Option.defaultValue Unchecked.defaultof<_>

    type Key =
        {
            VaultName: ResourceName
            KeyName: ResourceName
            Location: Location
            Enabled: bool option
            ActivationDate: DateTime option
            ExpirationDate: DateTime option
            KeyOps: KeyOperation list
            KTY: KeyType
            Dependencies: ResourceId Set
            Tags: Map<string, string>
        }

        member this.Name = this.VaultName / this.KeyName
        member this.ResourceId = keys.resourceId this.Name

        interface IArmResource with
            member this.ResourceId = this.ResourceId

            member this.JsonModel =
                {| keys.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                    properties =
                        {|
                            attributes =
                                {|
                                    enabled = this.Enabled |> Option.toNullable
                                    exp =
                                        this.ExpirationDate
                                        |> Option.map (fun exp -> DateTimeOffset(exp).ToUnixTimeSeconds())
                                    nbf =
                                        this.ActivationDate
                                        |> Option.map (fun nbf -> DateTimeOffset(nbf).ToUnixTimeSeconds())
                                |}
                            curveName =
                                match this.KTY with
                                | EC curveName
                                | ECHSM curveName -> curveName |> KeyCurveName.ArmValue
                                | _ -> null
                            kty = this.KTY |> KeyType.ArmValue
                            keyOps =
                                if this.KeyOps.IsEmpty then
                                    Unchecked.defaultof<_>
                                else
                                    this.KeyOps |> List.map KeyOperation.ArmValue
                            keySize =
                                match this.KTY with
                                | RSA (RsaKeyLength keySize) -> box keySize
                                | RSAHSM (RsaKeyLength keySize) -> box keySize
                                | _ -> null
                        |}
                |}

type CreateMode =
    | Recover
    | Default

type Vault =
    {
        Name: ResourceName
        Location: Location
        TenantId: string
        Sku: KeyVault.Sku
        Uri: Uri option
        Deployment: FeatureFlag option
        DiskEncryption: FeatureFlag option
        RbacAuthorization: FeatureFlag option
        TemplateDeployment: FeatureFlag option
        SoftDelete: SoftDeletionMode option
        CreateMode: CreateMode option
        AccessPolicies: {| ObjectId: ArmExpression
                           ApplicationId: Guid option
                           Permissions: {| Keys: Key Set
                                           Secrets: Secret Set
                                           Certificates: Certificate Set
                                           Storage: Storage Set |} |} list
        DefaultAction: DefaultAction option
        Bypass: Bypass option
        IpRules: string list
        VnetRules: string list
        DisablePublicNetworkAccess: FeatureFlag option
        Tags: Map<string, string>
    }

    member this.PurgeProtection =
        match this.SoftDelete with
        | None
        | Some SoftDeletionOnly -> None
        | Some SoftDeleteWithPurgeProtection -> Some true

    member private _.ToStringArray s =
        s |> Set.map (fun s -> s.ToString().ToLower()) |> Set.toArray

    member this.Dependencies =
        this.AccessPolicies |> List.choose (fun r -> r.ObjectId.Owner) |> List.distinct

    interface IArmResource with
        member this.ResourceId = vaults.resourceId this.Name

        member this.JsonModel =
            {| vaults.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                properties =
                    {|
                        tenantId = this.TenantId
                        sku =
                            {|
                                name = this.Sku.ArmValue
                                family = "A"
                            |}
                        enabledForDeployment = this.Deployment |> Option.map (fun f -> f.AsBoolean) |> Option.toNullable
                        enabledForDiskEncryption =
                            this.DiskEncryption |> Option.map (fun f -> f.AsBoolean) |> Option.toNullable
                        enabledForTemplateDeployment =
                            this.TemplateDeployment
                            |> Option.map (fun f -> f.AsBoolean)
                            |> Option.toNullable
                        enableRbacAuthorization =
                            this.RbacAuthorization |> Option.map (fun f -> f.AsBoolean) |> Option.toNullable
                        enableSoftDelete =
                            match this.SoftDelete with
                            | None -> Nullable()
                            | Some SoftDeleteWithPurgeProtection
                            | Some SoftDeletionOnly -> Nullable true
                        createMode = this.CreateMode |> Option.map (fun m -> m.ToString().ToLower()) |> Option.toObj
                        enablePurgeProtection = this.PurgeProtection |> Option.toNullable
                        vaultUri = this.Uri |> Option.map string |> Option.toObj
                        accessPolicies =
                            [|
                                for policy in this.AccessPolicies do
                                    {|
                                        objectId = ArmExpression.Eval policy.ObjectId
                                        tenantId = this.TenantId
                                        applicationId = policy.ApplicationId |> Option.map string |> Option.toObj
                                        permissions =
                                            {|
                                                keys = this.ToStringArray policy.Permissions.Keys
                                                storage = this.ToStringArray policy.Permissions.Storage
                                                certificates = this.ToStringArray policy.Permissions.Certificates
                                                secrets = this.ToStringArray policy.Permissions.Secrets
                                            |}
                                    |}
                            |]
                        networkAcls =
                            {|
                                defaultAction = this.DefaultAction |> Option.map string |> Option.toObj
                                bypass = this.Bypass |> Option.map string |> Option.toObj
                                ipRules = this.IpRules
                                virtualNetworkRules = this.VnetRules |> List.map (fun rule -> {| id = rule |})
                            |}
                        publicNetworkAccess =
                            match this.DisablePublicNetworkAccess with
                            | Some FeatureFlag.Enabled -> "Disabled"
                            | Some FeatureFlag.Disabled -> "Enabled"
                            | None -> null
                    |}
            |}

type VaultAddPolicies =
    {
        KeyVault: LinkedResource
        TenantId: string option
        AccessPolicies: {| ObjectId: ArmExpression
                           ApplicationId: Guid option
                           Permissions: {| Keys: Key Set
                                           Secrets: Secret Set
                                           Certificates: Certificate Set
                                           Storage: Storage Set |} |} list
    }

    member private _.ToStringArray s =
        s |> Set.map (fun s -> s.ToString().ToLower()) |> Set.toArray

    interface IArmResource with
        member this.ResourceId =
            accessPolicies.resourceId (this.KeyVault.Name / (ResourceName "add"))

        member this.JsonModel =
            let dependencies =
                match this.KeyVault with
                | Managed kvResId -> [ kvResId ]
                | _ -> []

            {| accessPolicies.Create(this.KeyVault.Name / (ResourceName "add"), dependsOn = dependencies) with
                properties =
                    {|
                        accessPolicies =
                            [|
                                for policy in this.AccessPolicies do
                                    {|
                                        objectId = ArmExpression.Eval policy.ObjectId
                                        tenantId = this.TenantId |> Option.defaultValue "[subscription().tenantId]"
                                        applicationId = policy.ApplicationId |> Option.map string |> Option.toObj
                                        permissions =
                                            {|
                                                keys = this.ToStringArray policy.Permissions.Keys
                                                storage = this.ToStringArray policy.Permissions.Storage
                                                certificates = this.ToStringArray policy.Permissions.Certificates
                                                secrets = this.ToStringArray policy.Permissions.Secrets
                                            |}
                                    |}
                            |]
                    |}
            |}
