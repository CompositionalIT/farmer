#r @"Farmer\bin\Debug\net5.0\Farmer.dll"

open Farmer
open Farmer.Writer
open Farmer.Arm.KeyVault.Keys

let key:KeyVaultKey = {
    VaultName = ResourceName "Hi"
    KeyName = ResourceName "World"
    Attributes = None
    Location = Location.EastUS
    CRV = None
    KeyOps = None
    KeySize = None
    KTY = None
    Tags = Map.empty
}

let deployment = arm {
    add_resource key
}

deployment |> quickWrite "KeyVaultdoodles"