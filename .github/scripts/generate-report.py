#!/usr/bin/env python3
"""
Generate a Markdown build report from CI artifacts.

Reads from:
  dl/trx/<version>/*.trx     — xUnit test result files
  dl/coverage/Summary.txt    — ReportGenerator coverage summary

Writes to:
  archive/<timestamp>_<branch>_<sha>.md
"""

import os
import re
import glob
import xml.etree.ElementTree as ET
from datetime import datetime, timezone

DOTNET_VERSIONS = ["8.0.x", "9.0.x", "10.0.x"]
TRX_NS = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
COVERAGE_THRESHOLD = 60.0


# ── Helpers ────────────────────────────────────────────────────────────────────

def env(key: str, default: str = "unknown") -> str:
    return os.environ.get(key, default)


def parse_trx(path: str) -> dict:
    """Parse a .trx XML file and return test counters."""
    try:
        tree = ET.parse(path)
        root = tree.getroot()
        counters = root.find(f".//{{{TRX_NS}}}Counters")
        if counters is None:
            return {"passed": 0, "failed": 0, "total": 0, "error": "No counters found"}

        # Also collect individual failed test names for the report
        failed_tests = []
        for result in root.findall(f".//{{{TRX_NS}}}UnitTestResult"):
            if result.get("outcome") == "Failed":
                failed_tests.append(result.get("testName", "unknown"))

        return {
            "passed":       int(counters.get("passed", 0)),
            "failed":       int(counters.get("failed", 0)),
            "total":        int(counters.get("total", 0)),
            "failed_tests": failed_tests,
            "error":        None,
        }
    except Exception as ex:
        return {"passed": 0, "failed": 0, "total": 0, "failed_tests": [], "error": str(ex)}


def collect_test_results() -> dict:
    """Collect test results for each .NET version from downloaded TRX files."""
    results = {}
    for version in DOTNET_VERSIONS:
        pattern = f"dl/trx/{version}/*.trx"
        files = glob.glob(pattern)
        if not files:
            results[version] = {
                "passed": 0, "failed": 0, "total": 0,
                "failed_tests": [], "error": "No TRX file found (artifact missing or job skipped)"
            }
            continue
        totals = {"passed": 0, "failed": 0, "total": 0, "failed_tests": [], "error": None}
        for f in files:
            r = parse_trx(f)
            totals["passed"] += r["passed"]
            totals["failed"] += r["failed"]
            totals["total"]  += r["total"]
            totals["failed_tests"].extend(r.get("failed_tests", []))
            if r["error"]:
                totals["error"] = r["error"]
        results[version] = totals
    return results


def compare_results(results: dict) -> list[str]:
    """
    Compare results across .NET versions.
    Returns a list of warning/info strings about discrepancies.
    """
    notes = []
    available = {v: r for v, r in results.items() if r["total"] > 0}

    if len(available) < 2:
        return ["⚠️ Not enough data to compare versions."]

    totals = [r["total"] for r in available.values()]
    if len(set(totals)) > 1:
        notes.append(
            "⚠️ **Test count mismatch across versions** — different number of tests ran per .NET version:"
        )
        for v, r in available.items():
            notes.append(f"  - {v}: {r['total']} tests")
    else:
        notes.append(f"✅ All versions ran the same number of tests: **{totals[0]}**")

    failures_by_version = {v: r["failed"] for v, r in available.items() if r["failed"] > 0}
    if failures_by_version:
        failing_versions = list(failures_by_version.keys())
        passing_versions = [v for v in available if v not in failures_by_version]
        if passing_versions:
            notes.append(
                f"⚠️ **Version-specific failure** — "
                f"failing on {', '.join(failing_versions)}, "
                f"passing on {', '.join(passing_versions)}. "
                f"This may indicate a .NET API compatibility issue."
            )
        else:
            notes.append("❌ Tests failed on all versions.")
    else:
        notes.append("✅ All versions passed all tests consistently.")

    return notes


def parse_coverage() -> dict:
    """Parse branch coverage percentage from ReportGenerator Summary.txt."""
    path = "dl/coverage/Summary.txt"
    if not os.path.exists(path):
        return {"value": None, "raw": "Coverage artifact not available"}
    text = open(path).read()
    match = re.search(r"Branch coverage:\s*([\d.]+)%", text)
    if not match:
        return {"value": None, "raw": text[:500]}
    return {"value": float(match.group(1)), "raw": text}


# ── Report generation ──────────────────────────────────────────────────────────

def build_report(now: datetime) -> str:
    build_test_result  = env("BUILD_TEST_RESULT")
    publish_exe_result = env("PUBLISH_EXE_RESULT")
    branch  = env("BRANCH")
    sha     = env("SHA")[:8]
    run_id  = env("RUN_ID")
    run_num = env("RUN_NUMBER")
    repo    = env("REPO")

    overall_ok   = (build_test_result == "success" and publish_exe_result == "success")
    overall_icon = "✅ SUCCESS" if overall_ok else "❌ FAILED"

    test_results = collect_test_results()
    coverage     = parse_coverage()
    comparison   = compare_results(test_results)

    total_passed = sum(r["passed"] for r in test_results.values())
    total_failed = sum(r["failed"] for r in test_results.values())
    total_tests  = sum(r["total"]  for r in test_results.values())

    cov_value = coverage["value"]
    if cov_value is None:
        cov_display = "⚠️ N/A — coverage artifact missing"
        cov_alert   = ""
    elif cov_value < COVERAGE_THRESHOLD:
        cov_display = f"🔴 **{cov_value:.1f}%** — BELOW {COVERAGE_THRESHOLD:.0f}% threshold!"
        cov_alert   = (
            f"\n> 🚨 **Coverage alert:** branch coverage is {cov_value:.1f}%, "
            f"below the required {COVERAGE_THRESHOLD:.0f}%.\n"
            f"> Run `dotnet test` locally with coverage and open `coveragereport/index.html` "
            f"to find uncovered branches.\n"
        )
    else:
        cov_display = f"🟢 {cov_value:.1f}%"
        cov_alert   = ""

    actions_url = f"https://github.com/{repo}/actions/runs/{run_id}"
    ts_iso      = now.strftime("%Y-%m-%d %H:%M:%S UTC")

    lines = [
        f"# Build Report — {ts_iso}",
        "",
        "## Summary",
        "",
        "| Field | Value |",
        "|-------|-------|",
        f"| **Status** | {overall_icon} |",
        f"| Branch | `{branch}` |",
        f"| Commit | `{sha}` |",
        f"| Run | [#{run_num}]({actions_url}) |",
        f"| Timestamp | {ts_iso} |",
        "",
    ]

    # ── Failed jobs ────────────────────────────────────────────────────────────
    if not overall_ok:
        lines += [
            "## ❌ Failed Jobs",
            "",
        ]
        if build_test_result != "success":
            lines.append(
                f"- **Build & Test** — result: `{build_test_result}`  "
                f"→ [Open Actions log]({actions_url}) and look for the first red step "
                f"(restore → build → test → coverage)"
            )
        if publish_exe_result != "success":
            lines.append(
                f"- **Publish & Smoke Test** — result: `{publish_exe_result}`  "
                f"→ [Open Actions log]({actions_url})"
            )
        lines.append("")

    # ── Test results ───────────────────────────────────────────────────────────
    lines += [
        "## Test Results",
        "",
        "| .NET Version | ✅ Passed | ❌ Failed | Total | Status |",
        "|---|---|---|---|---|",
    ]
    for version in DOTNET_VERSIONS:
        r = test_results[version]
        if r["error"] and r["total"] == 0:
            row_icon = "⚠️ N/A"
        elif r["failed"] > 0:
            row_icon = "❌"
        else:
            row_icon = "✅"
        lines.append(
            f"| {version} | {r['passed']} | {r['failed']} | {r['total']} | {row_icon} |"
        )

    lines += [
        "",
        f"**Total across all versions: {total_passed} passed, {total_failed} failed, {total_tests} total**",
        "",
    ]

    # ── Failed test names ──────────────────────────────────────────────────────
    all_failed: dict[str, list[str]] = {
        v: r["failed_tests"] for v, r in test_results.items() if r.get("failed_tests")
    }
    if all_failed:
        lines += ["### ❌ Failing Tests", ""]
        for version, names in all_failed.items():
            lines.append(f"**{version}:**")
            for name in names:
                lines.append(f"- `{name}`")
        lines += [
            "",
            "> Run `dotnet test Converter.slnx -c Debug` locally to reproduce.",
            "",
        ]

    # ── Cross-version comparison ───────────────────────────────────────────────
    lines += [
        "## Cross-Version Comparison",
        "",
    ]
    for note in comparison:
        lines.append(note)
    lines.append("")

    # ── Coverage ───────────────────────────────────────────────────────────────
    lines += [
        "## Branch Coverage (.NET 8)",
        "",
        "| Metric | Value |",
        "|--------|-------|",
        f"| Branch coverage | {cov_display} |",
        f"| Threshold | {COVERAGE_THRESHOLD:.0f}% |",
        "",
    ]
    if cov_alert:
        lines.append(cov_alert)

    # ── Artifacts ──────────────────────────────────────────────────────────────
    lines += [
        "## Artifacts",
        "",
        f"Download from [Actions run #{run_num}]({actions_url}):",
        "",
        "| Artifact | Contents |",
        "|----------|----------|",
        "| `coverage-report` | HTML coverage report + Cobertura XML |",
        "| `test-results-8.0.x` | TRX test result file (.NET 8) |",
        "| `test-results-9.0.x` | TRX test result file (.NET 9) |",
        "| `test-results-10.0.x` | TRX test result file (.NET 10) |",
        "| `converter-exe` | Published single-file `Converter.exe` |",
        "",
    ]

    # ── Footer ─────────────────────────────────────────────────────────────────
    lines += [
        "---",
        f"*Generated automatically by CI on {ts_iso}*",
    ]

    return "\n".join(lines)


# ── Entry point ────────────────────────────────────────────────────────────────

def main():
    now = datetime.now(timezone.utc)
    ts  = now.strftime("%Y-%m-%dT%H-%M-%SZ")
    branch = env("BRANCH").replace("/", "-")
    sha    = env("SHA")[:8]

    os.makedirs("archive", exist_ok=True)
    filename = f"archive/{ts}_{branch}_{sha}.md"

    report = build_report(now)
    with open(filename, "w", encoding="utf-8") as f:
        f.write(report)

    print(f"Report written to: {filename}")
    print()
    print(report)

    output_file = os.environ.get("GITHUB_OUTPUT", "")
    if output_file:
        with open(output_file, "a") as f:
            f.write(f"report_file={filename}\n")


if __name__ == "__main__":
    main()
