[<AutoOpen>]
module Farmer.Builders.CosmosDb

open Farmer
open Farmer.CoreTypes
open Farmer.CosmosDb
open Farmer.Arm.DocumentDb
open DatabaseAccounts
open SqlDatabases

let internal buildKey (name:ResourceName) key =
    ArmExpression
        .resourceId(databaseAccounts, name)
        .Map(fun db ->
            sprintf
                "listKeys(%s, providers('Microsoft.DocumentDB','databaseAccounts').apiVersions[0]).%s"
                db
                key)

let internal buildConnectionString (name:ResourceName) keyIndex =
    ArmExpression
        .resourceId(databaseAccounts, name)
        .Map(fun db ->
            sprintf
                "listConnectionStrings(%s, providers('Microsoft.DocumentDB','databaseAccounts').apiVersions[0]).connectionStrings[%i].connectionString"
                db
                keyIndex)

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
      FreeTier : bool }
    member private this.AccountResourceName = this.AccountName.CreateResourceName this
    member this.PrimaryKey = buildKey this.AccountResourceName "primaryMasterKey"
    member this.SecondaryKey = buildKey this.AccountResourceName "secondaryMasterKey"
    member this.PrimaryReadonlyKey = buildKey this.AccountResourceName "primaryReadonlyMasterKey"
    member this.SecondaryReadonlyKey = buildKey this.AccountResourceName "secondaryReadonlyMasterKey"
    member this.PrimaryConnectionString = buildConnectionString this.AccountResourceName 0
    member this.SecondaryConnectionString = buildConnectionString this.AccountResourceName 1
    member this.Endpoint =
        sprintf "reference(concat('Microsoft.DocumentDb/databaseAccounts/', '%s')).documentEndpoint" this.AccountResourceName.Value
        |> ArmExpression.create
    interface IBuilder with
        member this.DependencyName = this.AccountResourceName
        member this.BuildResources location = [
            // Account
            match this.AccountName with
            | AutoCreate _ ->
                { Name = this.AccountResourceName
                  Location = location
                  ConsistencyPolicy = this.AccountConsistencyPolicy
                  PublicNetworkAccess = this.PublicNetworkAccess
                  FailoverPolicy = this.AccountFailoverPolicy
                  FreeTier = this.FreeTier }
            | External _ ->
                ()

            // Database
            { Name = this.DbName
              Account = this.AccountResourceName
              Throughput = this.DbThroughput }

            // Containers
            for container in this.Containers do
                { Name = container.Name
                  Account = this.AccountResourceName
                  Database = this.DbName
                  PartitionKey =
                    {| Paths = fst container.PartitionKey
                       Kind = snd container.PartitionKey |}
                  UniqueKeyPolicy =
                    {| UniqueKeys =
                        container.UniqueKeys
                        |> Set.map (fun uniqueKeyPath ->
                            {| Paths = uniqueKeyPath |}
                        )
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
          FreeTier = false }

    /// Sets the name of the CosmosDB server.
    [<CustomOperation "account_name">]
    member __.AccountName(state:CosmosDbConfig, serverName) = { state with AccountName = AutoCreate (Named serverName) }
    member this.AccountName(state:CosmosDbConfig, serverName) = this.AccountName(state, ResourceName serverName)
    /// Links the database to an existing server
    [<CustomOperation "link_to_account">]
    member __.LinkToAccount(state:CosmosDbConfig, server:CosmosDbConfig) = { state with AccountName = External(Managed(server.AccountName.CreateResourceName server)) }
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

let cosmosDb = CosmosDbBuilder()
let cosmosContainer = CosmosDbContainerBuilder()
