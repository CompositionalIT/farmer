open Farmer
open Farmer.Writer
open Farmer.Arm.KeyVault.Keys


let key:KeyVaultKey = {
    VaultName = ResourceName "Hi"
    KeyName = ResourceName "World"
    Attributes = None
    CRV = None
    KeyOps = None
    KeySize = None
    KTY = None
    Tags = None
}

let deployment = arm {
    add_resource key
}

deployment |> quickWrite "KeyVaultdoodles"