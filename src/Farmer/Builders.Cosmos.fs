[<AutoOpen>]
module Farmer.Resources.CosmosDb

open Farmer
open Farmer.Models

type CosmosDbContainerConfig =
    { Name : ResourceName
      PartitionKey : string list * CosmosDbIndexKind
      Indexes : (string * (CosmosDbIndexDataType * CosmosDbIndexKind) list) list
      ExcludedPaths : string list }
type CosmosDbConfig =
    { ServerName : ResourceRef
      ServerConsistencyPolicy : ConsistencyPolicy
      ServerFailoverPolicy : FailoverPolicy
      DbName : ResourceName
      DbThroughput : string
      Containers : CosmosDbContainerConfig list }

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
        { DbName = ResourceName.Empty
          ServerName = AutomaticPlaceholder
          ServerConsistencyPolicy = Eventual
          ServerFailoverPolicy = NoFailover
          DbThroughput = "400"
          Containers = [] }
    member __.Run state =
        match state.ServerName with
        | AutomaticallyCreated _
        | External _ ->
            state
        | AutomaticPlaceholder ->
            { state with ServerName = sprintf "%s-server" state.DbName.Value |> ResourceName |> AutomaticallyCreated }
    /// Sets the name of the CosmosDB server.
    [<CustomOperation "server_name">]
    member __.ServerName(state:CosmosDbConfig, serverName) = { state with ServerName = AutomaticallyCreated serverName }
    member this.ServerName(state:CosmosDbConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
    /// Links the database to an existing server
    [<CustomOperation "link_to_server">]
    member __.LinkToServer(state:CosmosDbConfig, server:CosmosDbConfig) = { state with ServerName = External server.ServerName.ResourceName }
    /// Sets the name of the database.
    [<CustomOperation "db_name">]
    member __.DbName(state:CosmosDbConfig, name) = { state with DbName = name }
    member this.DbName(state:CosmosDbConfig, name:string) = this.DbName(state, ResourceName name)
    /// Sets the consistency policy of the database.
    [<CustomOperation "consistency_policy">]
    member __.ConsistencyPolicy(state:CosmosDbConfig, consistency:ConsistencyPolicy) = { state with ServerConsistencyPolicy = consistency }
    /// Sets the failover policy of the database.
    [<CustomOperation "failover_policy">]
    member __.FailoverPolicy(state:CosmosDbConfig, failoverPolicy:FailoverPolicy) = { state with ServerFailoverPolicy = failoverPolicy }
    /// Sets the throughput of the server.
    [<CustomOperation "throughput">]
    member __.Throughput(state:CosmosDbConfig, throughput) = { state with DbThroughput = throughput }
    member this.Throughput(state:CosmosDbConfig, throughput:int) = this.Throughput(state, string throughput)
    /// Adds a list of containers to the database.
    [<CustomOperation "add_containers">]
    member __.AddContainers(state:CosmosDbConfig, containers) = { state with Containers = state.Containers @ containers }

open WebApp
type WebAppBuilder with
    member this.DependsOn(state:WebAppConfig, cosmosDbConfig:CosmosDbConfig) =
        this.DependsOn(state, cosmosDbConfig.DbName)
type FunctionsBuilder with
    member this.DependsOn(state:FunctionsConfig, cosmosDbConfig:CosmosDbConfig) =
        this.DependsOn(state, cosmosDbConfig.DbName)

module Converters =
    let cosmosDb location (cosmos:CosmosDbConfig) =
        let account =
            match cosmos.ServerName with
            | AutomaticallyCreated name ->
                { Name = name
                  Location = location
                  ConsistencyPolicy = cosmos.ServerConsistencyPolicy
                  WriteModel = cosmos.ServerFailoverPolicy } |> Some
            | AutomaticPlaceholder ->
                failwith "No CosmosDB server was specified."
            | External _ ->
                None
        let sqlDb =
            { Name = cosmos.DbName
              Account = cosmos.ServerName.ResourceName
              Throughput = cosmos.DbThroughput }
        let containers = [
            for container in cosmos.Containers do
                { Name = container.Name
                  Account = cosmos.ServerName.ResourceName
                  Database = cosmos.DbName
                  PartitionKey =
                    {| Paths = fst container.PartitionKey
                       Kind = snd container.PartitionKey |}
                  IndexingPolicy =
                    {| ExcludedPaths = container.ExcludedPaths
                       IncludedPaths = [
                            for (path, indexes) in container.Indexes do
                                {| Path = path
                                   Indexes = [
                                       for (dataType, kind) in indexes do
                                        {| DataType = dataType
                                           Kind = kind |}
                                   ]
                                |}
                       ]
                    |}
                }
        ]
        {| Account = account
           SqlDb = sqlDb
           Containers = containers |}

open Farmer.Models
type ArmBuilder.ArmBuilder with
    member __.AddResource(state:ArmConfig, config:CosmosDbConfig) =
        let outputs =
            config
            |> Converters.cosmosDb state.Location
        let resources = [
            match outputs.Account with
            | Some account -> CosmosAccount account
            | None -> ()
            CosmosSqlDb outputs.SqlDb
            yield! outputs.Containers |> List.map CosmosContainer
        ]
        { state with Resources = state.Resources @ resources }
    member this.AddResources (state, configs) = addResources<CosmosDbConfig> this.AddResource state configs

let cosmosDb = CosmosDbBuilder()
let cosmosContainer = CosmosDbContainerBuilder()
