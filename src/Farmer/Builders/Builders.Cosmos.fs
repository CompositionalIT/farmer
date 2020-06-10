[<AutoOpen>]
module Farmer.Builders.CosmosDb

open Farmer
open Farmer.CoreTypes
open Farmer.CosmosDb
open Farmer.Arm.DocumentDb
open DatabaseAccounts
open SqlDatabases

type CosmosDbContainerConfig =
    { Name : ResourceName
      PartitionKey : string list * IndexKind
      Indexes : (string * (IndexDataType * IndexKind) list) list
      UniqueKeys : Set<string list>
      ExcludedPaths : string list }
type CosmosDbConfig =
    { AccountName : ResourceRef
      AccountConsistencyPolicy : ConsistencyPolicy
      AccountFailoverPolicy : FailoverPolicy
      Name : ResourceName
      DbThroughput : int<RU>
      Containers : CosmosDbContainerConfig list
      PublicNetworkAccess : FeatureFlag
      FreeTier : bool }
    member this.PrimaryKey =
        sprintf "[listKeys(resourceId('Microsoft.DocumentDB/databaseAccounts', '%s'), providers('Microsoft.DocumentDB','databaseAccounts').apiVersions[0]).primaryMasterKey]"
            this.AccountName.ResourceName.Value
    member this.Endpoint =
        sprintf "[reference(concat('Microsoft.DocumentDb/databaseAccounts/', '%s')).documentEndpoint]" this.AccountName.ResourceName.Value
    interface IBuilder with
        member this.DependencyName = this.AccountName.ResourceName
        member this.BuildResources location _ = [
            // Account
            match this.AccountName with
            | AutomaticallyCreated name ->
                { Name = name
                  Location = location
                  ConsistencyPolicy = this.AccountConsistencyPolicy
                  PublicNetworkAccess = this.PublicNetworkAccess
                  FailoverPolicy = this.AccountFailoverPolicy
                  FreeTier = this.FreeTier }
            | AutomaticPlaceholder ->
                failwith "No CosmosDB server was specified."
            | External _ ->
                ()

            // Database
            { Name = this.Name
              Account = this.AccountName.ResourceName
              Throughput = this.DbThroughput }

            // Containers
            for container in this.Containers do
                { Name = container.Name
                  Account = this.AccountName.ResourceName
                  Database = this.Name
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
        { Name = ResourceName.Empty
          AccountName = AutomaticPlaceholder
          AccountConsistencyPolicy = Eventual
          AccountFailoverPolicy = NoFailover
          DbThroughput = 400<RU>
          Containers = []
          PublicNetworkAccess = Enabled
          FreeTier = false }
    member __.Run state =
        match state.AccountName with
        | AutomaticallyCreated _
        | External _ ->
            state
        | AutomaticPlaceholder ->
            { state with AccountName = sprintf "%s-server" state.Name.Value |> ResourceName |> AutomaticallyCreated }
    /// Sets the name of the CosmosDB server.
    [<CustomOperation "account_name">]
    member __.AccountName(state:CosmosDbConfig, serverName) = { state with AccountName = AutomaticallyCreated serverName }
    member this.AccountName(state:CosmosDbConfig, serverName:string) = this.AccountName(state, ResourceName serverName)
    /// Links the database to an existing server
    [<CustomOperation "link_to_account">]
    member __.LinkToAccount(state:CosmosDbConfig, server:CosmosDbConfig) = { state with AccountName = External server.AccountName.ResourceName }
    /// Sets the name of the database.
    [<CustomOperation "name">]
    member __.Name(state:CosmosDbConfig, name) = { state with Name = name }
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
