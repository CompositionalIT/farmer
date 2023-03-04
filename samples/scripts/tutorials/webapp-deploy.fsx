#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let myWebApp = webApp {
    name "<insert_web_app_name_here>"
    zip_deploy @"<path_to_your_webapp_publish_folder>"
}

let template = arm {
    location Location.NorthEurope
    add_resource myWebApp
}

template |> Deploy.execute "mywebapp" Deploy.NoParameters
