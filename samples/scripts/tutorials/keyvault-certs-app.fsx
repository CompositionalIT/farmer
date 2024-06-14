#r "nuget: Suave, Version=2.6.0"

open Suave
open System.Security.Cryptography.X509Certificates

let certWithKey = new X509Certificate2("/certs/key.pfx", "")
let store = new X509Store(StoreName.Root, StoreLocation.CurrentUser)
store.Open(OpenFlags.ReadWrite)
store.Add(certWithKey)
store.Close()

let config = {
    defaultConfig with
        bindings = [ HttpBinding.createSimple (HTTPS certWithKey) "0.0.0.0" 443 ]
}

startWebServer config (Successful.OK "Hello Secure Farmers!")