#r @"Farmer\bin\Debug\net5.0\Farmer.dll"

open Farmer
open Farmer.Writer
open Farmer.Arm.KeyVault.Keys

let key:KeyVaultKey = {
    VaultName = ResourceName "Hi"
    KeyName = ResourceName "World"
    Attributes = None
    Location = Location.EastUS
    CurveName = Some JSONWebKeyCurveName.P256
    KeyOps = Some JsonWebKeyOperation.Encrypt
    KeySize = None
    KTY = Some JsonWebKeyType.RSAHSM
    Tags = Map.empty
}

let deployment = arm {
    add_resource key
}

deployment |> quickWrite "KeyVaultdoodles"