module Common

open Expecto
open Farmer

let tests = testList "Common" [
    test "IPAddressCidr creates correct range" {
        let cidr = IPAddressCidr.parse "192.168.0.0/24"
        let first, last = cidr |> IPAddressCidr.ipRange
        Expect.equal (string first) "192.168.0.0" "First address incorrect"
        Expect.equal (string last) "192.168.0.255" "Last address incorrect"
    }

    test "Can carve /22 into 4 /24 subnets" {
        let cidr = IPAddressCidr.parse "192.168.0.0/22"
        let subnets = [24; 24; 24; 24] |> IPAddressCidr.carveAddressSpace cidr |> Seq.map IPAddressCidr.format |> Array.ofSeq
        Expect.equal subnets.Length 4 "Incorrect number of subnets"
        Expect.equal subnets.[0] "192.168.0.0/24" "First subnet incorrect"
        Expect.equal subnets.[1] "192.168.1.0/24" "Second subnet incorrect"
        Expect.equal subnets.[2] "192.168.2.0/24" "Third subnet incorrect"
        Expect.equal subnets.[3] "192.168.3.0/24" "Fourth subnet incorrect"
    }

    test "Can carve /22 into 7 different subnets preventing overlap" {
        let cidr = IPAddressCidr.parse "192.168.0.0/22"
        let subnets = [24; 24; 24; 30; 30; 28; 26] |> IPAddressCidr.carveAddressSpace cidr |> Seq.map IPAddressCidr.format |> Array.ofSeq
        Expect.equal subnets.Length 7 "Incorrect number of subnets"
        Expect.equal subnets.[0] "192.168.0.0/24" "First subnet incorrect"
        Expect.equal subnets.[1] "192.168.1.0/24" "Second subnet incorrect"
        Expect.equal subnets.[2] "192.168.2.0/24" "Third subnet incorrect"
        Expect.equal subnets.[3] "192.168.3.0/30" "Fourth subnet incorrect"
        Expect.equal subnets.[4] "192.168.3.4/30" "Fifth subnet incorrect"
        Expect.equal subnets.[5] "192.168.3.16/28" "Sixth subnet incorrect"
        Expect.equal subnets.[6] "192.168.3.64/26" "Seventh subnet incorrect"
    }
    test "Fails to carve /22 into 3 /24 and 1 /23 subnets" {
        Expect.throws (
            fun _ ->
                let cidr = IPAddressCidr.parse "192.168.0.0/22"
                [24; 24; 24; 23] |> IPAddressCidr.carveAddressSpace cidr |> List.ofSeq |> ignore
        ) "Should have failed to carve /22 into subnets"
    }

    test "10.0.5.0/24 is contained within 10.0.0.0/16" {
        let innerCidr = IPAddressCidr.parse "10.0.5.0/24"
        let outerCidr = IPAddressCidr.parse "10.0.0.0/16"
        Expect.isTrue (outerCidr |> IPAddressCidr.contains innerCidr) ""
    }

    test "192.168.1.0/24 is not contained within 10.0.0.0/16" {
        let innerCidr = IPAddressCidr.parse "192.168.1.0/24"
        let outerCidr = IPAddressCidr.parse "10.0.0.0/16"
        Expect.isFalse (outerCidr |> IPAddressCidr.contains innerCidr) ""
    }

    test "Docker image tag generation" {
        let officialNginx = Containers.DockerImage.OfficialImage ("nginx")
        Expect.equal officialNginx.Tag "nginx" "Official image generated with incorrect tag"
        let officialNginxVersion = Containers.DockerImage.OfficialImage ("nginx", "1.21.4")
        Expect.equal officialNginxVersion.Tag "nginx:1.21.4" "Official versioned image generated with incorrect tag"
        let privateRepo = Containers.DockerImage.PrivateImage ("my.azurecr.io", "foo")
        Expect.equal privateRepo.Tag "my.azurecr.io/foo" "Private image generated with incorrect tag"
        let privateRepoVersion = Containers.DockerImage.PrivateImage ("my.azurecr.io", "foo", version="1.2.3")
        Expect.equal privateRepoVersion.Tag "my.azurecr.io/foo:1.2.3" "Private versioned image generated with incorrect tag"
        let privateRepoNamedContainer = Containers.DockerImage.PrivateImage ("my.azurecr.io", "foo", containerName="bar")
        Expect.equal privateRepoNamedContainer.Tag "my.azurecr.io/foo/bar" "Private named container image generated with incorrect tag"
        let privateRepoNamedContainerVersion = Containers.DockerImage.PrivateImage ("my.azurecr.io", "foo", "bar", "1.2.3")
        Expect.equal privateRepoNamedContainerVersion.Tag "my.azurecr.io/foo/bar:1.2.3" "Private named and versioned container image generated with incorrect tag"
    }

    test "Docker image tag parsing" {
        let officialNginx = Containers.DockerImage.Parse "nginx"
        Expect.equal officialNginx.Tag "nginx" "Official image generated with incorrect tag"
        let officialNginxVersion = Containers.DockerImage.Parse "nginx:1.21.4"
        Expect.equal officialNginxVersion.Tag "nginx:1.21.4" "Official versioned image generated with incorrect tag"
        let privateRepo = Containers.DockerImage.Parse "my.azurecr.io/foo"
        Expect.equal privateRepo.Tag "my.azurecr.io/foo" "Private image generated with incorrect tag"
        let privateRepoVersion = Containers.DockerImage.Parse "my.azurecr.io/foo:1.2.3"
        Expect.equal privateRepoVersion.Tag "my.azurecr.io/foo:1.2.3" "Private versioned image generated with incorrect tag"
        let privateRepoNamedContainer = Containers.DockerImage.Parse "my.azurecr.io/foo/bar"
        Expect.equal privateRepoNamedContainer.Tag "my.azurecr.io/foo/bar" "Private named container image generated with incorrect tag"
        let privateRepoNamedContainerVersion = Containers.DockerImage.Parse "my.azurecr.io/foo/bar:1.2.3"
        Expect.equal privateRepoNamedContainerVersion.Tag "my.azurecr.io/foo/bar:1.2.3" "Private named and versioned container image generated with incorrect tag"
    }
]
