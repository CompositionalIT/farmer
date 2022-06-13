Maintainers Guide
=================

This guide is for _maintainers_ who have commit access to the repository and can merge PR's and create release tags.

Release Process
---------------

The release process starts with drafting a new release in GitHub and results in a nuget package published to the public feed.

1. Update the `<Version>` tag in the [Farmer.fsproj](src/Farmer/Farmer.fsproj) file with the version number and commit it to master.
2. Go to [releases](https://github.com/CompositionalIT/farmer/releases) and click "Draft a new Release".
3. Under "Choose a tag", enter the version number for the new package to release to create a tag for the version when it is published. Creating this tag starts the Azure DevOps Pipeline.
4. In "Release Title", name it for the version number as well. This makes it easier to correlate a particular release with the nuget package when viewing the list of releases.
5. In "Describe this release", include the bullet points from the release notes for all the features added in this release. Viewing the "raw" [RELEASE_NOTES.md](https://raw.githubusercontent.com/CompositionalIT/farmer/master/RELEASE_NOTES.md) makes it easier to copy these for the description.
6. Click "Publish Release".
7. Go to the Azure DevOps pipeline and wait for the approval stage. If everything looks good, approve to publish the package to nuget.
