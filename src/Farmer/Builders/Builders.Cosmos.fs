[<AutoOpen>]
module Farmer.Builders.CosmosDb

open Farmer
open Farmer.Arm.DocumentDb
open DatabaseAccounts
open SqlDatabases

/// The consistency policy of a CosmosDB database.
type ConsistencyPolicy = Eventual | ConsistentPrefix | Session | BoundedStaleness of maxStaleness:int * maxIntervalSeconds : int | Strong
/// The failover policy of a CosmosDB database.
type FailoverPolicy = NoFailover | AutoFailover of secondaryLocation:Location | MultiMaster of secondaryLocation:Location
/// The kind of index to use on a CosmoDB container.
type CosmosDbIndexKind = Hash | Range
/// The datatype for the key of index to use on a CosmoDB container.
type CosmosDbIndexDataType = Number | String

type CosmosDbContainerConfig =
    { Name : ResourceName
      PartitionKey : string list * CosmosDbIndexKind
      Indexes : (string * (CosmosDbIndexDataType * CosmosDbIndexKind) list) list
      ExcludedPaths : string list }
type CosmosDbConfig =
    { ServerName : ResourceRef
      ServerConsistencyPolicy : ConsistencyPolicy
      ServerFailoverPolicy : FailoverPolicy
      Name : ResourceName
      DbThroughput : int
      Containers : CosmosDbContainerConfig list
      PublicNetworkAccess : FeatureFlag
      FreeTier : bool }
    interface IBuilder with
        member this.BuildResources location _ = [
            match this.ServerName with
            | AutomaticallyCreated name ->
                { Name = name
                  Location = location
                  ConsistencyPolicy =
                    match this.ServerConsistencyPolicy with
                    | BoundedStaleness _ -> "BoundedStaleness"
                    | Session | Eventual | ConsistentPrefix | Strong -> string this.ServerConsistencyPolicy
                  MaxStaleness =
                    match this.ServerConsistencyPolicy with
                    | BoundedStaleness (staleness, _) -> Some staleness
                    | Session | Eventual | ConsistentPrefix | Strong -> None
                  MaxInterval =
                    match this.ServerConsistencyPolicy with
                    | BoundedStaleness (_, interval) -> Some interval
                    | Session | Eventual | ConsistentPrefix | Strong -> None
                  EnableAutomaticFailure = match this.ServerFailoverPolicy with AutoFailover _ -> Some true | _ -> None
                  PublicNetworkAccess = this.PublicNetworkAccess
                  FreeTier = this.FreeTier
                  EnableMultipleWriteLocations = match this.ServerFailoverPolicy with MultiMaster _ -> Some true | _ -> None
                  FailoverLocations = [
                        match this.ServerFailoverPolicy with
                        | AutoFailover secondary
                        | MultiMaster secondary ->
                            {| Location = location; Priority = 0 |}
                            {| Location = secondary; Priority = 1 |}
                        | NoFailover ->
                            ()
                  ] }
            | AutomaticPlaceholder ->
                failwith "No CosmosDB server was specified."
            | External _ ->
                ()
            { Name = this.Name
              Account = this.ServerName.ResourceName
              Throughput = string this.DbThroughput }
            for container in this.Containers do
                { Name = container.Name
                  Account = this.ServerName.ResourceName
                  Database = this.Name
                  PartitionKey =
                    {| Paths = fst container.PartitionKey
                       Kind = snd container.PartitionKey |> string |}
                  IndexingPolicy =
                    {| ExcludedPaths = container.ExcludedPaths
                       IncludedPaths = [
                            for (path, indexes) in container.Indexes do
                                {| Path = path
                                   Indexes = [
                                       for (dataType, kind) in indexes do
                                        {| DataType = (string dataType).ToLower()
                                           Kind = string kind |}
                                   ]
                                |}
                       ]
                    |}
                }
        ]


type CosmosDbContainerBuilder() =
    member __.Yield _ =
        { Name = ResourceName ""
          PartitionKey = [], Hash
          Indexes = []
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

    /// Excludes a path from the container index.
    [<CustomOperation "exclude_path">]
    member __.ExcludePath (state:CosmosDbContainerConfig, path) =
        { state with ExcludedPaths = path :: state.ExcludedPaths }
type CosmosDbBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          ServerName = AutomaticPlaceholder
          ServerConsistencyPolicy = Eventual
          ServerFailoverPolicy = NoFailover
          DbThroughput = 400
          Containers = []
          PublicNetworkAccess = Enabled
          FreeTier = false }
    member __.Run state =
        match state.ServerName with
        | AutomaticallyCreated _
        | External _ ->
            state
        | AutomaticPlaceholder ->
            { state with ServerName = sprintf "%s-server" state.Name.Value |> ResourceName |> AutomaticallyCreated }
    /// Sets the name of the CosmosDB server.
    [<CustomOperation "server_name">]
    member __.ServerName(state:CosmosDbConfig, serverName) = { state with ServerName = AutomaticallyCreated serverName }
    member this.ServerName(state:CosmosDbConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
    /// Links the database to an existing server
    [<CustomOperation "link_to_server">]
    member __.LinkToServer(state:CosmosDbConfig, server:CosmosDbConfig) = { state with ServerName = External server.ServerName.ResourceName }
    /// Sets the name of the database.
    [<CustomOperation "name">]
    member __.Name(state:CosmosDbConfig, name) = { state with Name = name }
    member this.Name(state:CosmosDbConfig, name:string) = this.Name(state, ResourceName name)
    /// Sets the consistency policy of the database.
    [<CustomOperation "consistency_policy">]
    member __.ConsistencyPolicy(state:CosmosDbConfig, consistency:ConsistencyPolicy) = { state with ServerConsistencyPolicy = consistency }
    /// Sets the failover policy of the database.
    [<CustomOperation "failover_policy">]
    member __.FailoverPolicy(state:CosmosDbConfig, failoverPolicy:FailoverPolicy) = { state with ServerFailoverPolicy = failoverPolicy }
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

open WebApp
type WebAppBuilder with
    member this.DependsOn(state:WebAppConfig, cosmosDbConfig:CosmosDbConfig) =
        this.DependsOn(state, cosmosDbConfig.Name)
type FunctionsBuilder with
    member this.DependsOn(state:FunctionsConfig, cosmosDbConfig:CosmosDbConfig) =
        this.DependsOn(state, cosmosDbConfig.Name)

let cosmosDb = CosmosDbBuilder()
let cosmosContainer = CosmosDbContainerBuilder()
