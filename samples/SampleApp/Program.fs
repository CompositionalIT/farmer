open System
open Farmer
open Farmer.Builders.Cdn
//open Farmer.Arm.ContainerRegistry
open Farmer.Arm.Cdn
open Farmer.Builders
open Farmer.Cdn
open DeliveryPolicy

let ruleConfig =
    let test = None
    rule {
        order 1
        name "testrule"
        when_url_path Equals [ "/test"; "/test2" ] ToLowercase
//        when_device_type EquityOperator.Equals Mobile
//        when_device_type NotEquals Desktop
//        when_http_version Equals [Version20]
//        when_http_version EquityOperator.NotEquals [Version20; Version11]
//        when_request_cookies "test" StringComparisonOperator.Equals ["value1"; "value2"] CaseTransform.NoTransform
//        when_post_argument "argumentName" StringComparisonOperator.Contains ["argumentValue"] ToLowercase
//        when_query_string Equals ["2"] ToUppercase
//        when_remote_address Any []
//        when_request_body Contains ["test"] NoTransform
//        when_request_header "head" Contains ["toes"] NoTransform
//        when_request_method EquityOperator.Equals Post
//        when_request_protocol EquityOperator.NotEquals Http
//        when_request_url Contains ["test"] ToLowercase
//        when_url_file_extension Contains ["svg"] ToLowercase
//        when_url_file_name NotContains ["test"] ToLowercase
//        when_url_path Contains ["TEST"] ToUppercase      
//        cache_expiration BypassCache (TimeSpan(60 ,2,  0,  0))
//        cache_key_query_string Include "test"
//        modify_request_header Append "name" "value"
//        modify_response_header Append "name" "value"
//        url_rewrite "/pattern" "/destination" false
//        url_redirect Found UrlRedirectProtocol.Http null "/path" "queryString" "fragment"
    }

let storage = storageAccount { name "farmerteststorage" }

let endpointConfig =
    endpoint {
        name "farmer-test-endpoint2"
        origin storage
        add_rule ruleConfig
    }

let cdnConfig =
    cdn {
        name "farmer-test-cdn"
        sku Sku.Standard_Microsoft
        add_endpoints [ endpointConfig ]
    }

let deployment =
    arm {
        location Location.UKSouth
        add_resource cdnConfig
        add_resource storage
    }

// Generate the ARM template here...
deployment |> Writer.quickWrite "/test-output"
//Or deploy it directly to Azure here... (required Azure CLI installed!)
deployment |> Deploy.execute "codat-redwood-testing" Deploy.NoParameters
