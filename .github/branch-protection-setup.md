# Branch Protection Setup

These settings **cannot be stored in code** — they must be configured once in the GitHub repository settings by an admin.

## Steps (GitHub web UI)

1. Go to your repository → **Settings** → **Branches**
2. Click **Add branch ruleset** (or **Add rule** for classic rules)
3. **Branch name pattern:** `main`
4. Enable the following:

---

### Required settings (core protection)

| Setting | Value | Why |
|---------|-------|-----|
| **Require a pull request before merging** | ✅ ON | No direct pushes to `main` |
| **Required approving reviews** | `1` | At least 1 human approval |
| **Dismiss stale pull request approvals when new commits are pushed** | ✅ ON | Invalidates old approvals when PR is updated |
| **Require status checks to pass before merging** | ✅ ON | CI must be green |
| **Require branches to be up to date before merging** | ✅ ON | No stale branches |
| **Do not allow bypassing the above settings** | ✅ ON | Even admins must follow rules |
| **Restrict who can push to matching branches** | ✅ ON (no one) | Forces PR workflow |

### Status checks to require

Add these as required checks (they appear after the first CI run):

- `Build & Test (.NET 8.0.x)`
- `Build & Test (.NET 9.0.x)`
- `Build & Test (.NET 10.0.x)`
- `Publish and smoke-test executable`

---

## What happens with these settings

| Action | Result |
|--------|--------|
| `git push origin main` | ❌ Rejected — must use PR |
| PR opened | ✅ CI runs automatically |
| New commit pushed to open PR | ✅ Previous approvals invalidated |
| PR approved + CI green | ✅ Merge allowed |
| PR approved but CI red | ❌ Merge blocked |
| CI green but no approval | ❌ Merge blocked |

## Foreign / unknown branches

Any branch (including from forks) that opens a PR against `main` will trigger CI.
The first-time contributor rule in GitHub will require maintainer approval before CI runs on fork PRs.
