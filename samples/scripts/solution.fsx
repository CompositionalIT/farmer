#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"
open Farmer
open Farmer.Builders

let mysolution = solution {
 name "dqwdqwd"
}
let deployment = arm {
    add_resource mysolution
}

deployment
|> Writer.quickWrite "diagnostics"

