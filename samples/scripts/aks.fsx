#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open System
open System.IO
open Farmer
open Farmer.Builders

let homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
let pubKey =
    [homeDir; ".ssh"; "id_rsa.pub"]
    |> String.concat (string Path.DirectorySeparatorChar)
    |> File.ReadAllText

let myAksUser = "AKS SVC PRINCIPAL OBJECT ID"

let aksSubnet = "containernet"

let vnetName = sprintf "env%0i-vnet"
let aksName = sprintf "env%0i-aks"
let aksDns = aksName

let makeVnet (n:int) =
    vnet {
        name (vnetName n)
        add_address_spaces [
            "10.1.0.0/16"
        ]
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
    } :> IBuilder

let makeAks (n:int) =
    aks {
        name (aksName n)
        dns_prefix (aksDns n)
        enable_rbac
        add_agent_pools [
            agentPool {
                name "linuxPool"
                count 1
                vnet (vnetName n)
                subnet aksSubnet
            }
        ]
        network_profile (
            azureCniNetworkProfile {
                service_cidr "10.250.0.0/16"
            }
        )
        linux_profile "aksuser" pubKey
        service_principal_client_id myAksUser
    } :> IBuilder

let vnets = [1..4] |> Seq.map makeVnet |> List.ofSeq
let akses = [1..4] |> Seq.map makeAks |> List.ofSeq
arm {
    location Location.EastUS
    add_resources akses
    add_resources vnets
} |> Writer.quickWrite "aks-on-vnet"
