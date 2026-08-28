# Release Process

AStar.net uses a manual release process. Validation can be run through GitHub Actions, but packages and GitHub releases
are never published automatically.

## 1. Prepare the Release

- Confirm the intended semantic version.
- Update package version metadata and release notes.
- Update public documentation for behavior or API changes.
- Document breaking changes and migration steps when required.
- Ensure the repository and wiki contain no unintended changes.
- Confirm that no package artifact or publication credential is tracked by the repository.

## 2. Validate the Solution

From the repository root, run:

```powershell
dotnet restore AStarNet.sln
dotnet build AStarNet.sln --configuration Release --no-restore
dotnet test AStarNet.sln --configuration Release --no-build --no-restore
```

The build must complete without errors. Warnings and test failures must be reviewed rather than ignored.

Run the console demo manually when its behavior, library integration, or rendering has changed:

```powershell
dotnet run --project samples/AStarNet.ConsoleDemo/AStarNet.ConsoleDemo.csproj --configuration Release
```

## 3. Merge and Validate the Release Commit

Merge `develop` into `master` through a pull request. Run the manual GitHub Validation workflow against the final
commit on `master` when an independent clean-environment check is desired.

The Validation workflow performs restore, build, and test. It intentionally produces no package artifact.

## 4. Generate the NuGet Package

Generate the package explicitly from the final release commit:

```powershell
dotnet pack src/AStarNet/AStarNet.csproj --configuration Release --no-build --no-restore
```

The package is written to:

```text
src/AStarNet/bin/Release/
```

Inspect the `.nupkg` before publication. Confirm that it contains the library assembly, XML documentation, package
README, license, icon, and expected metadata. It must not contain the test project, console demo, development caches,
or unrelated repository assets.

## 5. Publish to NuGet

Upload the inspected package manually through NuGet.org or another explicitly chosen NuGet publication method. Verify
the package ID, version, dependencies, README, icon, and release notes in the preview before confirming publication.

Published NuGet versions are immutable. Never attempt to replace an existing version with a differently generated
package. Correct package content under a new semantic version when a published artifact requires a change.

## 6. Publish Documentation

Commit and push wiki changes after the corresponding source changes are available on `master`. Verify links that refer
to source paths, assets, releases, and the NuGet package.

## 7. Tag and Create the GitHub Release

- Create an annotated or GitHub-managed tag named `vX.Y.Z` on the released commit.
- Create a GitHub Release from that tag.
- Use a concise title matching the version.
- Summarize major changes, breaking changes, and migration requirements.
- Link to the published NuGet package.
- Confirm that GitHub marks the intended release as the latest stable release.

## 8. Verify the Published Release

After publication, verify the public release:

- confirm that the expected version and metadata appear on NuGet.org;
- confirm that the GitHub Release points to the correct tag and NuGet package;
- confirm that public README badges, repository links, and wiki links work.
