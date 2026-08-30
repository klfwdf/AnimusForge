#!/usr/bin/env python3
"""Validate the design-only AF phase-3 composition matrix."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

EXPECTED_CASE_COUNT = 18
MAX_CASES = 32
MAX_MODULES = 16
MAX_ISSUES = 32
MAX_MESSAGE_LENGTH = 240
EXPECTED_CASES = {
    "foundation-plus-noop",
    "foundation-adapter-noop",
    "foundation-plus-conversation",
    "foundation-plus-policy",
    "foundation-a-plus-b-no-bridge",
    "foundation-a-plus-b-plus-bridge",
    "required-provider-missing",
    "required-failure-cascade",
    "optional-provider-missing",
    "incompatible-contract-version",
    "safe-mode",
    "stale-completion",
    "partial-start-failure",
    "bridge-runtime-failure",
    "bridge-disabled-data-preserved",
    "runtime-toggle-harmony-conflict",
    "health-output-bounded",
    "health-output-overflow",
}
FORBIDDEN_KEY_PARTS = {
    "liveobject",
    "gameobject",
    "apikey",
    "rawprompt",
    "rawresponse",
    "delegate",
    "methodinfo",
    "saveinstance",
}


class MatrixFailure(Exception):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise MatrixFailure(message)


def load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise MatrixFailure(f"cannot load {path}: {exc}") from exc
    require(isinstance(value, dict), f"root must be object: {path}")
    return value


def reject_forbidden_keys(value: Any, path: str = "root") -> None:
    if isinstance(value, dict):
        for key, child in value.items():
            normalized = str(key).replace("-", "_").lower()
            require(not any(part in normalized for part in FORBIDDEN_KEY_PARTS), f"forbidden key at {path}.{key}")
            reject_forbidden_keys(child, f"{path}.{key}")
    elif isinstance(value, list):
        for index, child in enumerate(value):
            reject_forbidden_keys(child, f"{path}[{index}]")


def index_cases(document: dict[str, Any]) -> dict[str, dict[str, Any]]:
    require(document.get("schemaVersion") == 1, "schemaVersion mismatch")
    require(document.get("mode") == "design-only", "matrix must remain design-only")
    limits = document.get("limits")
    require(isinstance(limits, dict), "limits missing")
    require(limits.get("maxCases") == MAX_CASES, "case limit mismatch")
    require(limits.get("maxModules") == MAX_MODULES, "module limit mismatch")
    require(limits.get("maxIssues") == MAX_ISSUES, "issue limit mismatch")
    require(limits.get("maxMessageLength") == MAX_MESSAGE_LENGTH, "message limit mismatch")
    cases = document.get("cases")
    require(isinstance(cases, list) and len(cases) == EXPECTED_CASE_COUNT, "case count mismatch")
    indexed: dict[str, dict[str, Any]] = {}
    for case in cases:
        require(isinstance(case, dict), "case must be object")
        case_id = case.get("id")
        require(isinstance(case_id, str) and case_id, "case id missing")
        require(case_id not in indexed, f"duplicate case id: {case_id}")
        require(isinstance(case.get("profile"), str) and case["profile"], f"profile missing: {case_id}")
        require(isinstance(case.get("enabled"), list) and len(case["enabled"]) <= MAX_MODULES, f"enabled list invalid: {case_id}")
        require(isinstance(case.get("expected"), dict), f"expected missing: {case_id}")
        indexed[case_id] = case
    require(set(indexed) == EXPECTED_CASES, "case ID set mismatch")
    return indexed


def validate_cases(cases: dict[str, dict[str, Any]]) -> None:
    c = cases
    require(c["foundation-plus-noop"]["expected"] == {"foundation": "Active", "noop": "Active", "sideEffects": [], "preserveData": True}, "no-op result mismatch")
    require(c["foundation-adapter-noop"]["expected"]["apiLines"] == ["1.3", "1.4"], "adapter API closure mismatch")
    require(c["foundation-a-plus-b-no-bridge"]["expected"]["bridge"] == "Absent", "no-bridge state mismatch")
    require(c["foundation-a-plus-b-no-bridge"]["expected"]["hiddenIntegration"] is False, "hidden integration not rejected")
    require(c["foundation-a-plus-b-plus-bridge"]["expected"]["declaredIntegration"] is True, "declared bridge integration missing")

    required = c["required-provider-missing"]["expected"]
    require(required["conversation"] == "Blocked" and required["entryInvoked"] is False, "required missing must block entry")
    cascade = c["required-failure-cascade"]["expected"]
    require(cascade["bridge"] == "Blocked" and cascade["foundation"] == "Active" and cascade["adapter"] == "Active", "failure cascade isolation mismatch")
    require(cascade["unrelatedActive"] is True, "unrelated module not preserved")

    optional = c["optional-provider-missing"]["expected"]
    require(optional["conversation"] == "Degraded" and optional["fallback"] == "native-safe-reply", "optional fallback mismatch")
    require(optional["silentActiveClaim"] is False, "optional provider silently marked Active")

    incompatible = c["incompatible-contract-version"]["expected"]
    require(incompatible["policy"] == "Blocked" and incompatible["entryInvoked"] is False, "incompatible contract not blocked")

    safe = c["safe-mode"]["expected"]
    require(safe["foundation"] == "Active" and safe["adapter"] == "Active", "safe-mode foundation/adapter mismatch")
    require(safe["gameplay"] == "Excluded" and safe["bridge"] == "Excluded", "safe-mode gameplay/bridge leak")
    require(safe["dataDeleted"] is False and safe["autoMigration"] is False, "safe-mode data mutation")
    require(len(c["safe-mode"].get("preservedNamespaces", [])) >= 4, "safe-mode preservation metadata missing")

    stale = c["stale-completion"]["expected"]
    require(stale["result"] == "Expired", "stale result not expired")
    for field in ("mainThreadApply", "action", "saveWrite", "historyWrite", "afefWrite", "confirmedEvent"):
        require(stale[field] is False, f"stale side effect: {field}")

    partial = c["partial-start-failure"]["expected"]
    require(partial["policy"] == "Failed" and partial["orphanRegistrations"] is False, "partial failure cleanup mismatch")
    require(set(partial["cleanup"]) == {"service.policy", "listener.policy", "task.policy"}, "partial cleanup set mismatch")

    bridge_failure = c["bridge-runtime-failure"]["expected"]
    require(bridge_failure["bridge"] == "Failed", "bridge failure state mismatch")
    require(bridge_failure["policy"] == "Active" and bridge_failure["worldDiplomacy"] == "Active", "bridge failure damaged peers")
    require(bridge_failure["crossDomainWrites"] is False and bridge_failure["dataDeleted"] is False, "bridge failure side effect")

    bridge_disabled = c["bridge-disabled-data-preserved"]["expected"]
    require(bridge_disabled["bridge"] == "Disabled" and bridge_disabled["crossDomainWrites"] is False, "disabled bridge behavior mismatch")
    require(bridge_disabled["dataDeleted"] is False, "disabled bridge deleted data")

    toggle = c["runtime-toggle-harmony-conflict"]["expected"]
    require(toggle["candidate"] == "RestartRequired" and toggle["entryInvoked"] is False, "lifecycle conflict mismatch")

    health_ok = c["health-output-bounded"]["expected"]
    require(health_ok["issueCount"] == MAX_ISSUES and health_ok["messageLength"] == MAX_MESSAGE_LENGTH, "bounded health output mismatch")
    require(health_ok["originalLogicPreserved"] is True, "diagnostic failure changed original logic")
    health_overflow = c["health-output-overflow"]["expected"]
    require(health_overflow["foundation"] == "Failed" and health_overflow["boundedReport"] is True, "overflow health state mismatch")
    require(health_overflow["originalLogicPreserved"] is True, "overflow changed original logic")


def run(root: Path) -> dict[str, Any]:
    document = load_json(root / "composition-matrix.json")
    reject_forbidden_keys(document)
    cases = index_cases(document)
    validate_cases(cases)
    return {
        "state": "PASS",
        "cases": len(cases),
        "categories": 6,
        "invariants": 24,
        "execution": "composition-fixture-only; no Bannerlord/runtime/network access",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate AF phase-3 composition fixtures")
    parser.add_argument(
        "--fixture-root",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "docs" / "fixtures" / "phase3-composition-matrix",
    )
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    try:
        result = run(args.fixture_root)
    except MatrixFailure as exc:
        print(json.dumps({"state": "FAIL", "error": str(exc)[:MAX_MESSAGE_LENGTH]}, ensure_ascii=False), file=sys.stderr)
        return 1
    if args.json:
        print(json.dumps(result, ensure_ascii=False, separators=(",", ":")))
    else:
        print("PASS compositionMatrix cases={cases} categories={categories} invariants={invariants} execution={execution}".format(**result))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())