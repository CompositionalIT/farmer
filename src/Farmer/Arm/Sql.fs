[<AutoOpen>]
module Farmer.Arm.Sql

open Farmer

type Server =
    { ServerName : ResourceName
      Location : Location
      Credentials : {| Username : string; Password : SecureParameter |}
      Databases :
          {| Name : ResourceName
             Edition : string
             Collation : string
             Objective : string
             TransparentDataEncryption : FeatureFlag |} list
      FirewallRules :
          {| Name : string
             Start : System.Net.IPAddress
             End : System.Net.IPAddress |} list
    }
    interface IParameters with
        member this.SecureParameters = [ this.Credentials.Password ]
    interface IArmResource with
        member this.ResourceName = this.ServerName
        member this.JsonValue =
            {| ``type`` = "Microsoft.Sql/servers"
               name = this.ServerName.Value
               apiVersion = "2014-04-01-preview"
               location = this.Location.ArmValue
               tags = {| displayName = this.ServerName.Value |}
               properties =
                   {| administratorLogin = this.Credentials.Username
                      administratorLoginPassword = this.Credentials.Password.AsArmRef.Eval()
                      version = "12.0" |}
               resources = [
                   for database in this.Databases do
                       box
                           {| ``type`` = "databases"
                              name = database.Name.Value
                              apiVersion = "2015-01-01"
                              location = this.Location.ArmValue
                              tags = {| displayName = database.Name.Value |}
                              properties =
                               {| edition = database.Edition
                                  collation = database.Collation
                                  requestedServiceObjectiveName = database.Objective |}
                              dependsOn = [
                                  this.ServerName.Value
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

