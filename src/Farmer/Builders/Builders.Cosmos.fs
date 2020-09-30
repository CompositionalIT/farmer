[<AutoOpen>]
module Farmer.Builders.CosmosDb

open Farmer
open Farmer.CoreTypes
open Farmer.CosmosDb
open Farmer.Arm.DocumentDb
open DatabaseAccounts
open SqlDatabases

type KeyType = PrimaryKey | SecondaryKey member this.ArmValue = match this with PrimaryKey -> "primary" | SecondaryKey -> "secondary"
type KeyAccess = ReadWrite | ReadOnly member this.ArmValue = match this with ReadWrite -> "" | ReadOnly -> "readonly"
type ConnectionStringKind = PrimaryConnectionString | SecondaryConnectionString member this.KeyIndex = match this with PrimaryConnectionString -> 0 | SecondaryConnectionString -> 1

type CosmosDb =
    static member private providerPath = "providers('Microsoft.DocumentDb','databaseAccounts').apiVersions[0]"
    static member getKey (resourceId:ResourceId, keyType:KeyType, keyAccess:KeyAccess) =
        let resourceId = resourceId.WithType(databaseAccounts)
        let expr = sprintf "listKeys(%s, %s).%s%sMasterKey" resourceId.ArmExpression.Value CosmosDb.providerPath keyType.ArmValue keyAccess.ArmValue
        ArmExpression.create(expr).WithOwner(resourceId)
    static member getKey (name:ResourceName, keyType, keyAccess) = CosmosDb.getKey(ResourceId.create name, keyType, keyAccess)
    static member getConnectionString (resourceId:ResourceId, connectionStringKind:ConnectionStringKind) =
        let resourceId = resourceId.WithType(databaseAccounts)
        let expr = sprintf "listConnectionStrings(%s, %s).connectionStrings[%i].connectionString" resourceId.ArmExpression.Value CosmosDb.providerPath connectionStringKind.KeyIndex
        ArmExpression.create(expr).WithOwner(resourceId)
    static member getConnectionString (name:ResourceName, connectionStringKind) = CosmosDb.getConnectionString (ResourceId.create name, connectionStringKind)

type CosmosDbContainerConfig =
    { Name : ResourceName
      PartitionKey : string list * IndexKind
      Indexes : (string * (IndexDataType * IndexKind) list) list
      UniqueKeys : Set<string list>
      ExcludedPaths : string list }
type CosmosDbConfig =
    { AccountName : ResourceRef<CosmosDbConfig>
      AccountConsistencyPolicy : ConsistencyPolicy
      AccountFailoverPolicy : FailoverPolicy
      DbName : ResourceName
      DbThroughput : int<RU>
      Containers : CosmosDbContainerConfig list
      PublicNetworkAccess : FeatureFlag
      FreeTier : bool
      Tags: Map<string,string> }
    member private this.AccountResourceId = this.AccountName.CreateResourceId(this).WithType(databaseAccounts)
    member this.PrimaryKey = CosmosDb.getKey(this.AccountResourceId, PrimaryKey, ReadWrite)
    member this.SecondaryKey = CosmosDb.getKey(this.AccountResourceId, SecondaryKey, ReadWrite)
    member this.PrimaryReadonlyKey = CosmosDb.getKey(this.AccountResourceId, PrimaryKey, ReadOnly)
    member this.SecondaryReadonlyKey = CosmosDb.getKey(this.AccountResourceId, SecondaryKey, ReadOnly)
    member this.PrimaryConnectionString = CosmosDb.getConnectionString(this.AccountResourceId, PrimaryConnectionString)
    member this.SecondaryConnectionString = CosmosDb.getConnectionString(this.AccountResourceId, SecondaryConnectionString)
    member this.Endpoint =
        ArmExpression
            .reference(databaseAccounts, this.AccountResourceId)
            .Map(sprintf "%s.documentEndpoint")
    interface IBuilder with
        member this.DependencyName = this.AccountResourceId.Name
        member this.BuildResources location = [
            // Account
            match this.AccountName with
            | DeployableResource this _ ->
                { Name = this.AccountResourceId.Name
                  Location = location
                  ConsistencyPolicy = this.AccountConsistencyPolicy
                  PublicNetworkAccess = this.PublicNetworkAccess
                  FailoverPolicy = this.AccountFailoverPolicy
                  FreeTier = this.FreeTier
                  Tags = this.Tags }
            | _ ->
                ()

            // Database
            { Name = this.DbName
              Account = this.AccountResourceId.Name
              Throughput = this.DbThroughput }

            // Containers
            for container in this.Containers do
                { Name = container.Name
                  Account = this.AccountResourceId.Name
                  Database = this.DbName
                  PartitionKey =
                    {| Paths = fst container.PartitionKey
                       Kind = snd container.PartitionKey |}
                  UniqueKeyPolicy =
                    {| UniqueKeys =
                        container.UniqueKeys
                        |> Set.map (fun uniqueKeyPath -> {| Paths = uniqueKeyPath |})
                    |}
                  IndexingPolicy =
                    {| ExcludedPaths = container.ExcludedPaths
                       IncludedPaths = [
                            for (path, indexes) in container.Indexes do
                                {| Path = path
                                   Indexes = indexes |}
                       ]
                    |}
                }
        ]

type CosmosDbContainerBuilder() =
    member __.Yield _ =
        { Name = ResourceName ""
          PartitionKey = [], Hash
          Indexes = []
          UniqueKeys = Set.empty
          ExcludedPaths = [] }
    member _.Run state =
        match state.PartitionKey with
        | [], _ -> failwithf "You must set a partition key on CosmosDB container '%s'." state.Name.Value
        | partitions, indexKind ->
            { state with
                PartitionKey =
                    [ for partition in partitions do
                        if partition.StartsWith "/" then partition
                        else "/" + partition
                    ], indexKind }

    /// Sets the name of the container.
    [<CustomOperation "name">]
    member __.Name (state:CosmosDbContainerConfig, name) =
        { state with Name = ResourceName name }

    /// Sets the partition key of the container.
    [<CustomOperation "partition_key">]
    member __.PartitionKey (state:CosmosDbContainerConfig, partitions, indexKind) =
        { state with PartitionKey = partitions, indexKind }

    /// Adds an index to the container.
    [<CustomOperation "add_index">]
    member __.AddIndex (state:CosmosDbContainerConfig, path, indexes) =
        { state with Indexes = (path, indexes) :: state.Indexes }

    /// Adds a unique key constraint to the container (ensures uniqueness within the logical partition).
    [<CustomOperation "add_unique_key">]
    member __.AddUniqueKey (state:CosmosDbContainerConfig, uniqueKeyPaths) =
        { state with UniqueKeys = state.UniqueKeys.Add(uniqueKeyPaths) }

    /// Excludes a path from the container index.
    [<CustomOperation "exclude_path">]
    member __.ExcludePath (state:CosmosDbContainerConfig, path) =
        { state with ExcludedPaths = path :: state.ExcludedPaths }
type CosmosDbBuilder() =
    member __.Yield _ =
        { DbName = ResourceName.Empty
          AccountName = derived (fun config ->
            let dbNamePart =
                let maxLength = 36
                let dbName = config.DbName.Value.ToLower()
                if config.DbName.Value.Length > maxLength then dbName.Substring maxLength
                else dbName
            ResourceName (sprintf "%s-account" dbNamePart))
          AccountConsistencyPolicy = Eventual
          AccountFailoverPolicy = NoFailover
          DbThroughput = 400<RU>
          Containers = []
          PublicNetworkAccess = Enabled
          FreeTier = false
          Tags = Map.empty }

    /// Sets the name of the CosmosDB server.
    [<CustomOperation "account_name">]
    member __.AccountName(state:CosmosDbConfig, serverName) = { state with AccountName = AutoCreate (Named serverName) }
    member this.AccountName(state:CosmosDbConfig, serverName) = this.AccountName(state, ResourceName serverName)
    /// Links the database to an existing server
    [<CustomOperation "link_to_account">]
    member __.LinkToAccount(state:CosmosDbConfig, server:CosmosDbConfig) = { state with AccountName = External(Managed(server.AccountName.CreateResourceId(server).Name)) }
    /// Sets the name of the database.
    [<CustomOperation "name">]
    member __.Name(state:CosmosDbConfig, name) = { state with DbName = name }
    member this.Name(state:CosmosDbConfig, name:string) = this.Name(state, ResourceName name)
    /// Sets the consistency policy of the database.
    [<CustomOperation "consistency_policy">]
    member __.ConsistencyPolicy(state:CosmosDbConfig, consistency:ConsistencyPolicy) = { state with AccountConsistencyPolicy = consistency }
    /// Sets the failover policy of the database.
    [<CustomOperation "failover_policy">]
    member __.FailoverPolicy(state:CosmosDbConfig, failoverPolicy:FailoverPolicy) = { state with AccountFailoverPolicy = failoverPolicy }
    /// Sets the throughput of the server.
    [<CustomOperation "throughput">]
    member __.Throughput(state:CosmosDbConfig, throughput) = { state with DbThroughput = throughput }
    /// Adds a list of containers to the database.
    [<CustomOperation "add_containers">]
    member __.AddContainers(state:CosmosDbConfig, containers) = { state with Containers = state.Containers @ containers }
    /// Enables public network access
    [<CustomOperation "enable_public_network_access">]
    member __.PublicNetworkAccess(state:CosmosDbConfig) = { state with PublicNetworkAccess = Enabled }
    /// Disables public network access
    [<CustomOperation "disable_public_network_access">]
    member __.PrivateNetworkAccess(state:CosmosDbConfig) = { state with PublicNetworkAccess = Disabled }
    /// Enables the use of CosmosDB free tier (one per subscription).
    [<CustomOperation "free_tier">]
    member __.FreeTier(state:CosmosDbConfig) = { state with FreeTier = true }
    [<CustomOperation "add_tags">]
    member _.Tags(state:CosmosDbConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:CosmosDbConfig, key, value) = this.Tags(state, [ (key,value) ])

let cosmosDb = CosmosDbBuilder()
let cosmosContainer = CosmosDbContainerBuilder()
