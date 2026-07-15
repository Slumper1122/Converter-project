# CI Failure Guide

This document explains what each CI failure means and how to fix it.
Use this when a pipeline step goes red.

---

## Step 1 — Checkout source code

**Fails when:** repository access is revoked or the ref does not exist.

```
Error: fatal: repository not found
```

**Fix:** Check that the repository is public or the runner has access.

---

## Step 2 — Setup .NET SDK

**Fails when:** the requested SDK version does not exist or the setup action itself has a bug.

```
Error: Unable to find SDK version 8.0.x
```

**Fix:** Verify the version in the matrix (`8.0.x`, `9.0.x`, `10.0.x`) matches a real release at https://dotnet.microsoft.com/download.

---

## Step 3 — Cache NuGet packages

**Fails when:** the cache service is unavailable (rare). Pipeline continues; packages are re-downloaded.

---

## Step 4 — Restore dependencies

**Fails when:**
- A NuGet package version does not exist
- A `.csproj` `<PackageReference>` has a typo or removed package
- Network is unreliable

```
error NU1101: Unable to find package Newtonsoft.Json. No packages exist with this id in source(s)
```

**Fix:**
```bash
dotnet restore Converter.slnx   # reproduce locally
```
Check `*.csproj` for typos in package names or versions. Verify the package exists on https://www.nuget.org.

**Intentional break test:**
```xml
<!-- Converter.csproj — change to a non-existent version -->
<PackageReference Include="Newtonsoft.Json" Version="999.0.0" />
```
Expected: Step 4 fails with `NU1101`.

---

## Step 5 — Build project

**Fails when:**
- C# syntax errors
- Missing `using` statements
- Ambiguous or removed API references
- `.csproj` has invalid XML

```
error CS0103: The name 'NonExistentClass' does not exist in the current context
```

**Fix:**
```bash
dotnet build Converter.slnx -c Debug   # reproduce locally
```
Read the error line, open the file, fix the syntax.

**Intentional break test:**
```csharp
// CsvLineParser.cs — add a syntax error
public static string[] Parsee(string line)  // typo: extra 'e'
```
Expected: Step 5 fails with `CS0103` or `CS1061`.

**Intentional .csproj break:**
```xml
<!-- remove closing tag -->
<Project Sdk="Microsoft.NET.Sdk"
```
Expected: Step 5 fails with MSBuild XML parse error.

---

## Step 6 — Run tests and collect coverage

**Fails when:**
- One or more xUnit tests fail
- `coverlet.runsettings` is missing
- The test project references a missing type

```
Failed Converter.Tests.FileConverterServiceTests.Convert_ValidCsv_...
```

**Fix:**
```bash
dotnet test Converter.slnx -c Debug   # reproduce locally
```
Read the failing test name and the assertion error. Fix the logic in the converter or the test.

**Intentional test break:**
```csharp
// FileConverterServiceTests.cs
Assert.Equal("WRONG", lines[0]);   // wrong expected value
```
Expected: Step 6 fails with xUnit assertion error listing expected vs actual.

---

## Step 7 — Publish unit test results

**Fails when:** no `.trx` files were produced (Step 6 crashed before writing results).

**Fix:** Fix whatever caused Step 6 to crash (not just fail tests).

---

## Step 8 — Generate HTML coverage report

**Fails when:** `coverage.cobertura.xml` was not produced (Step 6 crashed or `coverlet.runsettings` is wrong/missing).

**Fix:** Ensure `coverlet.runsettings` exists and `coverlet.collector` NuGet is referenced in `Converter.Tests.csproj`.

---

## Step 9 — Enforce minimum 60% branch coverage

**Fails when:** branch coverage drops below 60%.

```
FAIL: 45.2% is below the required 60%.
Hint: Add tests for uncovered branches, then push again.
```

**Fix:** Add unit tests for uncovered branches. Run locally with:
```bash
dotnet test Converter.slnx -c Debug --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```
Then install ReportGenerator to inspect which lines are not covered:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
```
Open `coveragereport/index.html` in a browser.

---

## Step 10 / 11 — Upload artifacts

**Fails when:** the files to upload don't exist (earlier step failed).

`if-no-files-found: warn` — coverage artifact shows a warning but does not fail the pipeline.
`if-no-files-found: error` — exe artifact fails the pipeline if no `.exe` was produced.

---

## publish-exe job — Smoke test

**Fails when:**
- The published `.exe` is not created
- The exe crashes on startup
- `sample.json` is not created after running `Converter.exe sample.csv json`

```
Smoke test FAILED: expected sample.json was not created next to the exe.
```

**Fix:** Run `dotnet publish` locally and test the exe manually.

---

## Common questions

### What if a foreign/unknown branch opens a PR?

Any branch can open a PR. CI will run. Merging to `main` still requires:
- All CI checks green
- 1 approving review from a code owner (CODEOWNERS)

### What if I push directly to main?

With branch protection enabled: the push is **rejected** by GitHub.

```
remote: error: GH006: Protected branch update failed for refs/heads/main.
remote: error: Required status check "Build & Test (.NET 8.0.x)" is expected.
```

### What if a new commit is pushed to an open PR?

The previous approval is **automatically dismissed** (stale review dismissal).
The reviewer must re-approve after seeing the new changes.
