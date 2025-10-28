# GitHub Copilot Instructions for Farmer Repository

## Pull Request Requirements

When creating or updating pull requests for this repository, you **MUST** follow these guidelines:

### 1. Follow the PR Template

Every PR must follow the template in `pull_request_template.md`:

- Link to the issue being closed: "This PR closes #" (add issue number)
- List all changes in bullet points under "The changes in this PR are as follows:"
- State: "I have read the [contributing guidelines](CONTRIBUTING.md) and have completed the following:"
- Complete the checklist items:
  - [ ] **Tested my code** end-to-end against a live Azure subscription.
  - [ ] **Updated the documentation** in the docs folder for the affected changes.
  - [ ] **Written unit tests** against the modified code that I have made.
  - [ ] **Updated the [release notes](RELEASE_NOTES.md)** with a new entry for this PR.
  - [ ] **Checked the coding standards** outlined in the [contributions guide](CONTRIBUTING.md) and ensured my code adheres to them.
- If you haven't completed any of the tasks above, include the reasons why here
- Include this section: "Below is a minimal example configuration that includes the new features, which can be used to deploy to Azure:" followed by an F# code block with the example

### 2. Format Code with Fantomas

**CRITICAL**: All F# code MUST be formatted with Fantomas before committing. This is automatically checked by CI and PRs will be rejected if formatting is not applied.

**How to format code:**

1. Restore tools first (only needed once):
   ```bash
   dotnet tool restore
   ```

2. Format all F# code in the src directory:
   ```bash
   dotnet fantomas src -r
   ```

3. Verify formatting is correct:
   ```bash
   dotnet fantomas src --check
   ```

**ALWAYS run `dotnet fantomas src -r` after making any changes to F# files and before committing.**

### 3. Coding Standards

Follow these F# coding standards:

- **Never** use `yield` - it is no longer necessary in F#
- Prefer `[ for x in y do ... ]` to `[ for x in y -> ... ]`
- **Never** use `.Value` on `Option` types
- Follow the .editorconfig settings for indentation and formatting

### 4. Documentation

Every PR that modifies a resource or adds a new keyword **MUST** have corresponding documentation updates in the `docs/` folder.

### 5. Unit Tests

Every PR **SHOULD** have at least one test associated with it. If no tests are added, provide an explanation in the PR description.

### 6. Release Notes

Every PR **MUST** include an entry in the `RELEASE_NOTES.md` file under the next release section. Format:

```markdown
* [Feature/Fix description] - [link to PR]
```

## Workflow Summary

For every PR:

1. Create an issue first (for non-trivial changes)
2. Make code changes following coding standards
3. **RUN FANTOMAS**: `dotnet fantomas src -r`
4. Update documentation in `docs/`
5. Write unit tests
6. Update `RELEASE_NOTES.md`
7. Verify fantomas formatting: `dotnet fantomas src --check`
8. Follow the PR template when creating the PR
9. Include a minimal F# example in the PR description

## Testing

Run tests with:
```bash
dotnet test -v n -c release
```

## Build

Build the project with:
```bash
dotnet build -c release
```

---

**Remember**: Fantomas formatting is enforced by CI. Always run `dotnet fantomas src -r` before committing!
