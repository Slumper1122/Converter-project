# Contributing

## Branch workflow

**Never push directly to `main`.** Always work on a feature branch and open a Pull Request.

```bash
git checkout main
git pull origin main
git checkout -b feature/my-change

# ... make changes, commit, push ...
git push -u origin feature/my-change
```

Then open a Pull Request on GitHub. The CI pipeline runs automatically.

### What happens when you push to an open PR

- Previous approvals are **automatically dismissed** (stale review invalidation)
- CI reruns from scratch on the new commit
- A reviewer must re-approve before the PR can be merged

### What if someone pushes directly to main?

With branch protection enabled, GitHub **rejects the push**:

```
remote: error: GH006: Protected branch update failed for refs/heads/main.
```

See [`.github/branch-protection-setup.md`](.github/branch-protection-setup.md) for how to enable this.

---

## Commit messages

Keep them short and descriptive (3–5 words):

```
add json to csv tests
fix coverage threshold step
update ci nuget cache key
resolve README merge conflict
```

---

## Local verification before pushing

```bash
dotnet restore Converter.slnx
dotnet build Converter.slnx -c Debug
dotnet test Converter.slnx -c Debug
```

---

## Manually trigger the CI pipeline

The workflow supports manual dispatch from the GitHub UI:

1. GitHub → your repo → **Actions**
2. Left panel: click **CI**
3. Right side: **Run workflow** button
4. Select branch → click **Run workflow**

---

## CI pipeline steps

Every push triggers these steps (visible in GitHub Actions logs):

| Step | What it does | Fails when |
|------|-------------|------------|
| 1. Checkout source code | Downloads the repository | Repo access issue |
| 2. Setup .NET SDK | Installs the .NET version | Invalid SDK version |
| 3. Cache NuGet packages | Restores package cache | (non-fatal) |
| 4. Restore dependencies | `dotnet restore` | Bad package reference |
| 5. Build project | `dotnet build` | Compiler errors |
| 6. Run tests + coverage | `dotnet test` + Coverlet | Failing tests |
| 7. Publish test results | TRX → PR annotations | No TRX files |
| 8. Generate coverage report | ReportGenerator HTML | No coverage XML |
| 9. Enforce 60% branch coverage | Python script checks threshold | Coverage < 60% |
| 10. Upload coverage artifact | Zips and uploads HTML report | (warn if missing) |
| 11. Upload test results | Uploads TRX files | (warn if missing) |

After all 3 matrix legs (`.NET 8 / 9 / 10`) pass, a second job runs:

| Step | What it does |
|------|-------------|
| Publish exe | `dotnet publish` → single `.exe` |
| Smoke test | Runs `Converter.exe sample.csv json`, checks output exists |
| Upload exe artifact | Downloads the `.exe` from GitHub Actions |

---

## Intentional failure tests (how to verify the pipeline catches errors)

### Break a test
```csharp
// FileConverterServiceTests.cs
Assert.Equal("WRONG_EXPECTED", lines[0]);
```
Expected: **Step 6** fails — test name and assertion shown in log and PR annotation.

### Break the build
```csharp
// Any .cs file
var x = NonExistentClass.DoSomething();
```
Expected: **Step 5** fails — compiler error with file/line number.

### Break a NuGet dependency
```xml
<!-- Converter.csproj -->
<PackageReference Include="Newtonsoft.Json" Version="999.0.0" />
```
Expected: **Step 4** fails — `NU1101: Unable to find package`.

### Break the .csproj XML
```xml
<!-- Converter.csproj — remove closing tag -->
<Project Sdk="Microsoft.NET.Sdk"
```
Expected: **Step 5** fails — MSBuild XML parse error.

For a full breakdown see [`.github/ci-failure-guide.md`](.github/ci-failure-guide.md).

---

## Branch protection setup

See [`.github/branch-protection-setup.md`](.github/branch-protection-setup.md) for step-by-step instructions to configure:

- No direct pushes to `main`
- Required approving review (1)
- Stale review dismissal
- Required CI status checks
