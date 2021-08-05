open Farmer
open Farmer.Builders

let template =
    let myWebApp = webApp {
        name "codat-devopstest"
        sku WebApp.Sku.B1
        custom_domain (DomainConfig.AppServiceDomain "devops-test.codat.io")
        app_insights_off
        runtime_stack Runtime.DotNetCore31
    }

    arm {
        location Location.UKSouth
        add_resource myWebApp
    }

template
//|> Writer.quickWrite "army"
|> Deploy.execute "my-resource-group-name-2" Deploy.NoParameters

