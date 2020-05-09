[<AutoOpen>]
module Farmer.Resources.PostgreSQLAzure

open Farmer
open Arm.PostgreSQL


type PostgreSQLBuilderState = {
    ServerName : ResourceRef
    AdminUserName : string
    Version : ServerVersion
    Sku : SkuSpec
}


type PostgreSQLBuilder() =
    
    member _this.Yield _ = {
        PostgreSQLBuilderState.ServerName = AutomaticPlaceholder
        AdminUserName = "someadmin"
        Version = VS_11
        Sku = { Family = Gen5; Size = StorageSizeInMBs.ofInt 5120; Tier = Basic; Capacity = VCores_2 }
    }
    
    member _this.Run (state: PostgreSQLBuilderState) =
        { new IResourceBuilder with
            member this.BuildResources location resources = 
                let serverResource =
                    match state.ServerName with
                    | External name ->
                        let resName = Helpers.sanitiseDb name |> ResourceName
                        resources |> Helpers.tryMergeResource resName (fun server -> { server with Databases = [] })
                    | AutomaticallyCreated name ->
                        { ServerName = Helpers.sanitiseDb name |> ResourceName
                          Location = location
                          Credentials = {|  Username = state.AdminUserName; Password = SecureParameter ""  |}
                          Version = state.Version
                          Sku = state.Sku
                          GeoRedundantBackup = Disabled
                          StorageAutoGrow = Enabled
                          BackupRetention = BackupRetentionInDays.ofInt 7
                          Databases = [] }
                    | AutomaticPlaceholder -> failwith "You must specific a server name, or link to an existing server."
                    
                [serverResource] }
        
    /// Sets the name of the PostgreSQL server
    member _this.ServerName(state:PostgreSQLBuilderState, serverName) = { state with ServerName = AutomaticallyCreated serverName }
    member this.ServerName(state:PostgreSQLBuilderState, serverName:string) = this.ServerName(state, ResourceName serverName)


let postgreSQL = PostgreSQLBuilder()
