# Contributing

## Branch workflow

1. Create a **feature branch** from `main` — never commit directly to `main`.
2. Make focused changes with descriptive commits (3–5 words).
3. Open a **Pull Request** into `main`.
4. Wait for the CI pipeline to pass (build, tests, coverage ≥ 60% branch).
5. Address review feedback; stale approvals are dismissed automatically when you push new commits (once branch protection is enabled).

```bash
git checkout main
git pull origin main
git checkout -b feature/my-change
# ... edit, commit, push ...
git push -u origin feature/my-change
```

## Commit messages

Keep them short and descriptive:

```
add json to csv tests
fix coverage threshold step
update ci cache key
```

## Local verification before pushing

```bash
dotnet restore Converter.slnx
dotnet build Converter.slnx -c Release
dotnet test Converter.slnx -c Release
```

## CI failure guide

| Failed step | What to check |
|-------------|---------------|
| Restore dependencies | `.csproj` syntax, NuGet package versions, network |
| Build project | Compiler errors in C# source |
| Run tests | Failing unit tests — run locally with `dotnet test` |
| Enforce branch coverage | Add tests for uncovered branches |
| Publish executable | Publish settings or smoke-test script |

## Branch protection setup (maintainers)

In GitHub repository settings for `main`:

- ✅ Require a pull request before merging
- ✅ Require status checks to pass (CI workflow)
- ✅ Do not allow bypassing the above settings
- ✅ Dismiss stale pull request approvals when new commits are pushed
- ✅ Restrict who can push to matching branches (optional: no direct pushes)
