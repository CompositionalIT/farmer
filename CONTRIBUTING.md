> This article is not a detailed guide on how to create a pull request (PR). See [here](https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/about-pull-requests) to learn more about how to work with pull requests on GitHub.

The purpose of this article is to illustrate the main checklists you must go through before a PR will be considered for inclusion in Farmer. If you are new to Farmer, F# or GitHub - **don't worry**. The team will be happy to support you getting your feature over the line.

These are the following checks we'll normally put in place:

#### 1. Create an issue first!
Except for small pull requests, create an issue to discuss the feature. The last thing we want is for someone to spend hours of their time on a feature only for someone else to have started work on something similar, or for the admins of the project to reject it for whatever reason e.g. does not fit with the project etc. Creating an issue does not take long and will help save time for everyone.
#### 2. Create Documentation
Every PR to Farmer **must** have some documentation with it. If you modify a resource and add a new keyword, it **must** be added to the appropriate docs page.
#### 3. Write Unit Tests
Every PR to Farmer **should** have at least one test associated with it. If no tests are added, you can expect at least a request for one or explanation as to why one is not necessary.
#### 4. Write Release Notes
Every PR to Farmer **must** include an entry to the `RELEASE_NOTES.md` file under the next release. Briefly explain the feature and ideally link to the PR number e.g.
#### 5. Adhere to Coding Standards
Here are some (very basic!) standards for the project:

* Follow the coding style of the existing source.
* Use 4 spaces for indentation.
* Records are defined as follows:

```fsharp
type MyRecord =
    { Field : Type
      Field : Type } list
```

* List comprehensions should be done as follows:

```fsharp
results = [
  for item in collection do
    item.Foo
]
```
* Put all pattern matching handlers on the same line as the pattern *or* all of them one a new line.
* Do not use `yield` - it is no longer necessary in F#.
* Prefer `[ for x in y do ... ]`  to `[ for x in y -> ... ]`
* Never use `.Value` on `Option` types.
* As a last resort, adhere to [official](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/) style guide as a basis.
