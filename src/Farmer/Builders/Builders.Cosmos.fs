[<AutoOpen>]
module Farmer.Builders.CosmosDb

open System
open Farmer
open Farmer.Arm
open Farmer.CosmosDb
open DatabaseAccounts
open Containers

type KeyType =
    | PrimaryKey
    | SecondaryKey

    member this.ArmValue =
        match this with
        | PrimaryKey -> "primary"
        | SecondaryKey -> "secondary"

type KeyAccess =
    | ReadWrite
    | ReadOnly

    member this.ArmValue =
        match this with
        | ReadWrite -> ""
        | ReadOnly -> "readonly"

type ConnectionStringKind =
    | PrimaryConnectionString
    | SecondaryConnectionString

    member this.KeyIndex =
        match this with
        | PrimaryConnectionString -> 0
        | SecondaryConnectionString -> 1

type CosmosDb =
    static member private providerPath =
        "providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]"

    static member getKey(resourceId: ResourceId, keyType: KeyType, keyAccess: KeyAccess) =
        let expr =
            $"listKeys({resourceId.ArmExpression.Value}, {CosmosDb.providerPath}).{keyType.ArmValue}{keyAccess.ArmValue}MasterKey"

        ArmExpression.create(expr).WithOwner(resourceId)

    static member getKey(name: ResourceName, keyType, keyAccess) =
        CosmosDb.getKey (databaseAccounts.resourceId name, keyType, keyAccess)

    static member getConnectionString(resourceId: ResourceId, connectionStringKind: ConnectionStringKind) =
        let expr =
            $"listConnectionStrings({resourceId.ArmExpression.Value}, {CosmosDb.providerPath}).connectionStrings[{connectionStringKind.KeyIndex}].connectionString"

        ArmExpression.create(expr).WithOwner(resourceId)

    static member getConnectionString(name: ResourceName, connectionStringKind) =
        CosmosDb.getConnectionString (databaseAccounts.resourceId name, connectionStringKind)

type CosmosDbContainerConfig = {
    Name: ResourceName
    PartitionKey: string list * IndexKind
    Indexes: (string * (IndexDataType * IndexKind) list) list
    UniqueKeys: Set<string list>
    ExcludedPaths: string list
    Kind: DatabaseKind
}

type CosmosDbConfig = {
    AccountName: ResourceRef<CosmosDbConfig>
    AccountConsistencyPolicy: ConsistencyPolicy
    AccountFailoverPolicy: FailoverPolicy
    DbName: ResourceName
    DbThroughput: Throughput
    Containers: CosmosDbContainerConfig list
    PublicNetworkAccess: FeatureFlag
    FreeTier: bool
    Tags: Map<string, string>
    Kind: DatabaseKind
} with

    member private this.AccountResourceId = this.AccountName.resourceId this

    member this.PrimaryKey =
        CosmosDb.getKey (this.AccountResourceId, PrimaryKey, ReadWrite)

    member this.SecondaryKey =
        CosmosDb.getKey (this.AccountResourceId, SecondaryKey, ReadWrite)

    member this.PrimaryReadonlyKey =
        CosmosDb.getKey (this.AccountResourceId, PrimaryKey, ReadOnly)

    member this.SecondaryReadonlyKey =
        CosmosDb.getKey (this.AccountResourceId, SecondaryKey, ReadOnly)

    member this.PrimaryConnectionString =
        CosmosDb.getConnectionString (this.AccountResourceId, PrimaryConnectionString)

    member this.SecondaryConnectionString =
        CosmosDb.getConnectionString (this.AccountResourceId, SecondaryConnectionString)

    member this.Endpoint =
        ArmExpression
            .reference(databaseAccounts, this.AccountResourceId)
            .Map(sprintf "%s.documentEndpoint")

    interface IBuilder with
        member this.ResourceId = this.AccountResourceId

        member this.BuildResources location = [
            // Account
            match this.AccountName with
            | DeployableResource this _ -> {
                Name = this.AccountResourceId.Name
                Location = location
                Kind = this.Kind
                ConsistencyPolicy = this.AccountConsistencyPolicy
                Serverless =
                    match this.DbThroughput with
                    | Serverless -> Enabled
                    | Provisioned _ -> Disabled
                PublicNetworkAccess = this.PublicNetworkAccess
                FailoverPolicy = this.AccountFailoverPolicy
                FreeTier = this.FreeTier
                Tags = this.Tags
              }
            | _ -> ()

            // Database
            {
                Name = this.DbName
                Account = this.AccountResourceId.Name
                Throughput = this.DbThroughput
                Kind = this.Kind
            }

            // Containers
            for container in this.Containers do
                {
                    Name = container.Name
                    Account = this.AccountResourceId.Name
                    Database = this.DbName
                    Kind = container.Kind
                    PartitionKey = {|
                        Paths = fst container.PartitionKey
                        Kind = snd container.PartitionKey
                    |}
                    UniqueKeyPolicy = {|
                        UniqueKeys =
                            container.UniqueKeys
                            |> Set.map (fun uniqueKeyPath -> {| Paths = uniqueKeyPath |})
                    |}
                    IndexingPolicy = {|
                        ExcludedPaths = container.ExcludedPaths
                        IncludedPaths = [
                            for (path, indexes) in container.Indexes do
                                {| Path = path; Indexes = indexes |}
                        ]
                    |}
                }
        ]

type CosmosDbContainerBuilder() =
    member _.Yield _ = {
        Name = ResourceName ""
        Kind = DatabaseKind.Document
        PartitionKey = [], Hash
        Indexes = []
        UniqueKeys = Set.empty
        ExcludedPaths = []
    }

    member _.Run state =
        match state.PartitionKey with
        | [], _ -> raiseFarmer $"You must set a partition key on CosmosDB container '{state.Name.Value}'."
        | partitions, indexKind -> {
            state with
                PartitionKey =
                    [
                        for partition in partitions do
                            if partition.StartsWith "/" then
                                partition
                            else
                                "/" + partition
                    ],
                    indexKind
          }

    /// Sets the name of the container.
    [<CustomOperation "name">]
    member _.Name(state: CosmosDbContainerConfig, name) = { state with Name = ResourceName name }

    /// Sets the container kind of the container.
    [<CustomOperation "gremlin_graph">]
    member _.Graph(state: CosmosDbContainerConfig) = {
        state with
            Kind = DatabaseKind.Gremlin
    }

    /// Sets the partition key of the container.
    [<CustomOperation "partition_key">]
    member _.PartitionKey(state: CosmosDbContainerConfig, partitions, indexKind) = {
        state with
            PartitionKey = partitions, indexKind
    }

    /// Adds an index to the container.
    [<CustomOperation "add_index">]
    member _.AddIndex(state: CosmosDbContainerConfig, path, indexes) = {
        state with
            Indexes = (path, indexes) :: state.Indexes
    }

    /// Adds a unique key constraint to the container (ensures uniqueness within the logical partition).
    [<CustomOperation "add_unique_key">]
    member _.AddUniqueKey(state: CosmosDbContainerConfig, uniqueKeyPaths) = {
        state with
            UniqueKeys = state.UniqueKeys.Add(uniqueKeyPaths)
    }

    /// Excludes a path from the container index.
    [<CustomOperation "exclude_path">]
    member _.ExcludePath(state: CosmosDbContainerConfig, path) = {
        state with
            ExcludedPaths = path :: state.ExcludedPaths
    }

type CosmosDbBuilder() =
    member _.Yield _ = {
        DbName = ResourceName.Empty
        AccountName =
            derived (fun config ->
                let dbName = config.DbName.Value.ToLower()
                let maxLength = 36 // 44 less "-account"

                if config.DbName.Value.Length > maxLength then
                    dbName.Substring maxLength
                else
                    dbName
                |> sprintf "%s-account"
                |> ResourceName
                |> databaseAccounts.resourceId)
        AccountConsistencyPolicy = Eventual
        AccountFailoverPolicy = NoFailover
        DbThroughput = Provisioned 400<RU>
        Containers = []
        PublicNetworkAccess = Enabled
        FreeTier = false
        Tags = Map.empty
        Kind = DatabaseKind.Document
    }

    static member ValidateContainers(state: CosmosDbConfig) =
        let validateContainerAndAccountConfig (container: CosmosDbContainerConfig, accountKind: DatabaseKind) =
            if container.Kind = accountKind then
                Ok container
            else
                Error $"Container {container.Name.Value} must be of {state.Kind} kind"

        state.Containers
        |> List.map (fun container -> validateContainerAndAccountConfig (container, state.Kind))

    member _.Run state =
        let errors =
            CosmosDbBuilder.ValidateContainers(state)
            |> List.choose (fun r ->
                match r with
                | Error e -> Some(e)
                | Ok _ -> None)

        if errors.Length > 0 then
            errors |> String.concat Environment.NewLine |> raiseFarmer

        state

    /// Sets the name of the CosmosDB server.
    [<CustomOperation "account_name">]
    member _.AccountName(state: CosmosDbConfig, accountName: ResourceName) = {
        state with
            AccountName =
                AutoGeneratedResource(
                    Named(
                        databaseAccounts.resourceId (
                            CosmosDbValidation.CosmosDbName.Create(accountName).OkValue.ResourceName
                        )
                    )
                )
    }

    member this.AccountName(state: CosmosDbConfig, accountName: string) =
        this.AccountName(state, ResourceName accountName)

    /// Links the database to an existing server
    [<CustomOperation "link_to_account">]
    member _.LinkToAccount(state: CosmosDbConfig, accountConfig: CosmosDbConfig) = {
        state with
            AccountName = LinkedResource(Managed(accountConfig.AccountName.resourceId accountConfig))
    }

    /// Sets the name of the database.
    [<CustomOperation "name">]
    member _.Name(state: CosmosDbConfig, name) = { state with DbName = name }

    member this.Name(state: CosmosDbConfig, name: string) = this.Name(state, ResourceName name)

    /// Sets the consistency policy of the database.
    [<CustomOperation "consistency_policy">]
    member _.ConsistencyPolicy(state: CosmosDbConfig, consistency: ConsistencyPolicy) = {
        state with
            AccountConsistencyPolicy = consistency
    }

    /// Sets the failover policy of the database.
    [<CustomOperation "failover_policy">]
    member _.FailoverPolicy(state: CosmosDbConfig, failoverPolicy: FailoverPolicy) = {
        state with
            AccountFailoverPolicy = failoverPolicy
    }

    /// Sets the throughput of the server.
    [<CustomOperation "throughput">]
    member _.Throughput(state: CosmosDbConfig, throughput) = { state with DbThroughput = throughput }

    member _.Throughput(state: CosmosDbConfig, throughput) = {
        state with
            DbThroughput = Provisioned throughput
    }

    /// Sets the storage kind
    [<CustomOperation "kind">]
    member _.StorageKind(state: CosmosDbConfig, kind) = { state with Kind = kind }

    /// Adds a list of containers to the database.
    [<CustomOperation "add_containers">]
    member _.AddContainers(state: CosmosDbConfig, containers) = {
        state with
            Containers = state.Containers @ containers
    }

    /// Enables public network access
    [<CustomOperation "enable_public_network_access">]
    member _.PublicNetworkAccess(state: CosmosDbConfig) = {
        state with
            PublicNetworkAccess = Enabled
    }

    /// Disables public network access
    [<CustomOperation "disable_public_network_access">]
    member _.PrivateNetworkAccess(state: CosmosDbConfig) = {
        state with
            PublicNetworkAccess = Disabled
    }

    /// Enables the use of CosmosDB free tier (one per subscription).
    [<CustomOperation "free_tier">]
    member _.FreeTier(state: CosmosDbConfig) = { state with FreeTier = true }

    interface ITaggable<CosmosDbConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let cosmosDb = CosmosDbBuilder()
let cosmosContainer = CosmosDbContainerBuilder()