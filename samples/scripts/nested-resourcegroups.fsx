#r "nuget: Farmer"

open Farmer
open Farmer.Builders

let myStorage = storageAccount { name "myfarmerstorage" }

let myVm = vm {
    name "farmer-test-vm"
    username "codat"
}

let nested = resourceGroup {
    name "farmer-test-inner"
    add_resource myStorage
    add_resource myVm
    output "foo" "bax"
}

let template = arm {
    location Location.UKSouth
    add_resource nested
}

template |> Writer.quickWrite "template"

template
|> Deploy.execute "farmer-test-rg" [ ("password-for-farmer-test-vm", "Codat121!") ]