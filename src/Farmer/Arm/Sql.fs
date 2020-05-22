[<AutoOpen>]
module Farmer.Arm.Sql

open Farmer
open Farmer.CoreTypes
open Farmer.Sql
open System.Net

type DbKind = Standalone of DbSku | Pool of ResourceName

type Server =
    { ServerName : ResourceName
      Location : Location
      Credentials : {| Username : string; Password : SecureParameter |}
      Databases :
        {| Name : ResourceName
           Sku : DbKind
           Collation : string
           TransparentDataEncryption : FeatureFlag |} list
      ElasticPool :
        {| Name : ResourceName
           Sku : PoolSku
        |} option
      FirewallRules :
        {| Name : string
           Start : IPAddress
           End : IPAddress |} list
    }
    interface IParameters with
        member this.SecureParameters = [ this.Credentials.Password ]
    interface IArmResource with
        member this.ResourceName = this.ServerName
        member this.JsonModel =
            {| ``type`` = "Microsoft.Sql/servers"
               name = this.ServerName.Value
               apiVersion = "2019-06-01-preview"
               location = this.Location.ArmValue
               tags = {| displayName = this.ServerName.Value |}
               properties =
                   {| administratorLogin = this.Credentials.Username
                      administratorLoginPassword = this.Credentials.Password.AsArmRef.Eval()
                      version = "12.0" |}
               resources = [
                    match this.ElasticPool with
                    | Some pool ->
                        box
                            {| ``type`` = "elasticPools"
                               name = pool.Name.Value
                               apiVersion = "2017-10-01-preview"
                               location = this.Location.ArmValue
                               sku = {| name = pool.Sku.Name; tier = pool.Sku.Edition; size = string pool.Sku.Capacity |}
                               dependsOn = [ this.ServerName.Value ]
                            |}
                    | None ->
                        ()
                    for database in this.Databases do
                        box
                            {| ``type`` = "databases"
                               name = database.Name.Value
                               apiVersion = "2019-06-01-preview"
                               location = this.Location.ArmValue
                               tags = {| displayName = database.Name.Value |}
                               sku =
                                 match database.Sku with
                                 | Standalone sku -> box {| name = sku.Name; tier = sku.Edition |}
                                 | Pool _ -> null
                               properties =
                                 {| collation = database.Collation
                                    elasticPoolId =
                                     match database.Sku with
                                     | Standalone _ -> null
                                     | Pool pool -> sprintf "[resourceId('Microsoft.Sql/servers/elasticPools', '%s', '%s')]" this.ServerName.Value pool.Value |}
                               dependsOn =
                                 [ this.ServerName.Value
                                   match this.ElasticPool with
                                   | Some pool -> pool.Name.Value
                                   | None -> ()
                                 ]
                               resources = [
                                   match database.TransparentDataEncryption with
                                   | Enabled ->
                                       {| ``type`` = "transparentDataEncryption"
                                          comments = "Transparent Data Encryption"
                                          name = "current"
                                          apiVersion = "2014-04-01-preview"
                                          properties = {| status = string database.TransparentDataEncryption |}
                                          dependsOn = [ database.Name.Value ]
                                       |}
                                    | Disabled ->
                                        ()
                               ]
                            |}
                    for rule in this.FirewallRules do
                        box
                            {| ``type`` = "firewallrules"
                               name = rule.Name
                               apiVersion = "2014-04-01"
                               location = this.Location.ArmValue
                               properties =
                                {| endIpAddress = string rule.Start
                                   startIpAddress = string rule.End |}
                               dependsOn = [ this.ServerName.Value ]
                            |}
               ]
            |} :> _