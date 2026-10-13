#r "nuget:Farmer"

open System
open System.IO
open Farmer
open Farmer.Builders
open Farmer.ContainerService
open Farmer.Vm    

let homeDir = Environment.GetFolderPath Environment.SpecialFolder.UserProfile

let pubKey =
    [ homeDir; ".ssh"; "id_rsa.pub" ]
    |> String.concat (string Path.DirectorySeparatorChar)
    |> File.ReadAllText

let aksSubnet = "containernet"

let vnetName = sprintf "env%0i-vnet"
let aksName = sprintf "env%0i-aks"
let aksDns = aksName

let makeVnet (n: int) =
    vnet {
        name (vnetName n)
        add_address_spaces [ "10.1.0.0/16" ]

        add_subnets [
            subnet {
                name "default"
                prefix "10.1.0.0/24"
            }
            subnet {
                name aksSubnet
                prefix "10.1.30.0/25"
            }
        ]
    }
    :> IBuilder

let msi = userAssignedIdentity { name "aks-user" }

let makeAks (n: int) =
    aks {
        name (aksName n)
        tier Tier.Standard
        dns_prefix (aksDns n)
        enable_rbac
        add_identity msi

        add_agent_pools [
            agentPool {
                name "linuxPool"
                vm_size VMSize.Standard_D2_v5
                count 3
                vnet (vnetName n)
                subnet aksSubnet
            }
        ]

        network_profile (azureCniNetworkProfile { service_cidr "10.250.0.0/16" })
        linux_profile "aksuser" pubKey
        service_principal_use_msi
    }
    :> IBuilder

let vnets = [ 1..4 ] |> Seq.map makeVnet |> List.ofSeq
let akses = [ 1..4 ] |> Seq.map makeAks |> List.ofSeq

arm {
    add_resource msi
    add_resources akses
    add_resources vnets
}
|> Writer.quickWrite "aks-on-vnet"
