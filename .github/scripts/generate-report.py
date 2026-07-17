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


def status_icon(result: str) -> str:
    return "✅" if result == "success" else "❌"


def parse_trx(path: str) -> dict:
    """Parse a .trx XML file and return test counters."""
    try:
        tree = ET.parse(path)
        root = tree.getroot()
        counters = root.find(f".//{{{TRX_NS}}}Counters")
        if counters is None:
            return {"passed": 0, "failed": 0, "total": 0, "error": "No counters found"}
        return {
            "passed": int(counters.get("passed", 0)),
            "failed": int(counters.get("failed", 0)),
            "total":  int(counters.get("total", 0)),
            "error":  None,
        }
    except Exception as ex:
        return {"passed": 0, "failed": 0, "total": 0, "error": str(ex)}


def collect_test_results() -> dict:
    """Collect test results for each .NET version from downloaded TRX files."""
    results = {}
    for version in DOTNET_VERSIONS:
        pattern = f"dl/trx/{version}/*.trx"
        files = glob.glob(pattern)
        if not files:
            results[version] = {"passed": 0, "failed": 0, "total": 0, "error": "No TRX file found"}
            continue
        totals = {"passed": 0, "failed": 0, "total": 0, "error": None}
        for f in files:
            r = parse_trx(f)
            totals["passed"] += r["passed"]
            totals["failed"] += r["failed"]
            totals["total"]  += r["total"]
            if r["error"]:
                totals["error"] = r["error"]
        results[version] = totals
    return results


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

    overall_ok = (build_test_result == "success" and publish_exe_result == "success")
    overall_icon = "✅ SUCCESS" if overall_ok else "❌ FAILED"

    test_results = collect_test_results()
    coverage     = parse_coverage()

    all_tests_passed = all(
        r["failed"] == 0 and r["total"] > 0
        for r in test_results.values()
    )
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
            f"\n> 🚨 **Coverage alert:** branch coverage {cov_value:.1f}% is below the required "
            f"{COVERAGE_THRESHOLD:.0f}%. Add tests for uncovered branches.\n"
        )
    else:
        cov_display = f"🟢 {cov_value:.1f}%"
        cov_alert   = ""

    actions_url = f"https://github.com/{repo}/actions/runs/{run_id}"
    ts_iso = now.strftime("%Y-%m-%d %H:%M:%S UTC")

    lines = [
        f"# Build Report — {ts_iso}",
        "",
        "## Summary",
        "",
        f"| Field | Value |",
        f"|-------|-------|",
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
                f"- **Build & Test** — `{build_test_result}` "
                f"— check [Actions log]({actions_url}) for the exact failing step "
                f"(restore / build / test / coverage)"
            )
        if publish_exe_result != "success":
            lines.append(
                f"- **Publish & Smoke Test** — `{publish_exe_result}` "
                f"— check [Actions log]({actions_url})"
            )
        lines.append("")

    # ── Test results ───────────────────────────────────────────────────────────
    lines += [
        "## Test Results",
        "",
        f"| .NET Version | Passed | Failed | Total | Status |",
        f"|---|---|---|---|---|",
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
        f"**Total: {total_passed} passed, {total_failed} failed, {total_tests} total**",
        "",
    ]

    if total_failed > 0:
        lines += [
            "> ❌ **Some tests failed.** Run `dotnet test Converter.slnx -c Debug` locally to reproduce.",
            "",
        ]

    # ── Coverage ───────────────────────────────────────────────────────────────
    lines += [
        "## Branch Coverage",
        "",
        f"| Metric | Value |",
        f"|--------|-------|",
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

    # Signal the filename for the CI step to pick up
    output_file = os.environ.get("GITHUB_OUTPUT", "")
    if output_file:
        with open(output_file, "a") as f:
            f.write(f"report_file={filename}\n")


if __name__ == "__main__":
    main()
