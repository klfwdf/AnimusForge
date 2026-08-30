#!/usr/bin/env python3
"""Validate design-only AF.Foundation.Runtime contract fixtures."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

EXPECTED_CONTRACTS = 6
EXPECTED_STATES = 8
EXPECTED_INVALID_CASES = 16
MAX_CONTRACTS = 64
MAX_FIELDS = 32
MAX_TEXT_LENGTH = 240
MAX_ISSUES = 32
ALLOWED_API_LINES = {"1.3", "1.4"}
ALLOWED_THREAD_BOUNDARIES = {"NoGameAccess", "BackgroundSnapshotOnly", "MainThreadApply"}
ALLOWED_PERSISTENCE = {"None", "ReadsExisting", "LegacyCompatibilityRequired"}
FORBIDDEN_FIELD_NAMES = {
    "game",
    "mission",
    "agent",
    "hero",
    "settlement",
    "kingdom",
    "idatastore",
    "delegate",
    "methodinfo",
    "jtoken",
    "jobject",
    "dynamic",
    "object",
}


class FoundationFailure(Exception):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise FoundationFailure(message)


def load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise FoundationFailure(f"cannot load {path}: {exc}") from exc
    require(isinstance(value, dict), f"root must be object: {path}")
    return value


def unique_ids(items: list[dict[str, Any]], label: str) -> set[str]:
    ids = []
    for item in items:
        require(isinstance(item, dict), f"{label}: item must be object")
        item_id = item.get("id")
        require(isinstance(item_id, str) and item_id, f"{label}: id missing")
        ids.append(item_id)
    require(len(ids) == len(set(ids)), f"{label}: duplicate id")
    return set(ids)


def validate_catalog(catalog: dict[str, Any]) -> dict[str, Any]:
    require(catalog.get("schemaVersion") == 1, "catalog schemaVersion mismatch")
    require(catalog.get("mode") == "design-only", "catalog must remain design-only")
    contracts = catalog.get("contracts")
    require(isinstance(contracts, list) and len(contracts) == EXPECTED_CONTRACTS, "contract count mismatch")
    ids = unique_ids(contracts, "contracts")
    for contract in contracts:
        require(contract.get("family") == "DTO", f"contract family invalid: {contract.get('id')}")
        require(contract.get("version") == 1, f"contract version invalid: {contract.get('id')}")
        require(contract.get("owner"), f"contract owner missing: {contract.get('id')}")
        require(set(contract.get("allowedApiLines", [])) == ALLOWED_API_LINES, f"API line closure invalid: {contract.get('id')}")
        require(contract.get("threadBoundary") in ALLOWED_THREAD_BOUNDARIES, f"thread boundary invalid: {contract.get('id')}")
        require(contract.get("persistenceImpact") in ALLOWED_PERSISTENCE, f"persistence impact invalid: {contract.get('id')}")
        fields = contract.get("fields")
        require(isinstance(fields, list) and 0 < len(fields) <= MAX_FIELDS, f"fields invalid/bounded: {contract.get('id')}")
        require(len(fields) == len(set(fields)), f"duplicate fields: {contract.get('id')}")
        for field in fields:
            normalized = str(field).replace("-", "").replace("_", "").replace(" ", "").lower()
            is_forbidden_type = normalized in FORBIDDEN_FIELD_NAMES
            is_forbidden_shape = (
                "dictionary<string,object>" in normalized
                or "dictionary<string,dynamic>" in normalized
                or normalized.endswith("instance")
                or normalized.endswith("delegate")
            )
            require(not (is_forbidden_type or is_forbidden_shape), f"forbidden field {field}: {contract.get('id')}")
    rules = catalog.get("rules")
    require(isinstance(rules, dict), "catalog rules missing")
    require(rules.get("maxContracts") == MAX_CONTRACTS, "maxContracts mismatch")
    require(rules.get("maxFieldsPerContract") == MAX_FIELDS, "maxFields mismatch")
    require(rules.get("maxTextLength") == MAX_TEXT_LENGTH, "maxTextLength mismatch")
    require(rules.get("maxIssues") == MAX_ISSUES, "maxIssues mismatch")
    require(rules.get("runtimeFrequency") == "0", "runtime frequency must be 0")
    require(rules.get("publicPayloadAllowsLiveObjects") is False, "live objects allowed")
    require(rules.get("publicPayloadAllowsDelegates") is False, "delegates allowed")
    require(rules.get("publicPayloadAllowsRawDictionary") is False, "raw dictionary allowed")
    states = catalog.get("states")
    require(isinstance(states, list) and len(states) == EXPECTED_STATES, "health state count mismatch")
    require(len(states) == len(set(states)), "health states not unique")
    require(set(states) == {"Discovered", "Disabled", "Blocked", "Starting", "Active", "Degraded", "Failed", "RestartRequired"}, "health states mismatch")
    return {"ids": ids, "states": set(states)}


def validate_invalid_cases(document: dict[str, Any]) -> int:
    cases = document.get("cases")
    require(isinstance(cases, list) and len(cases) == EXPECTED_INVALID_CASES, "invalid case count mismatch")
    unique_ids(cases, "invalid cases")
    by_id = {case["id"]: case for case in cases}
    for case in cases:
        require(isinstance(case.get("mutation"), str) and case["mutation"], f"mutation missing: {case.get('id')}")
        expected = case.get("expected")
        require(isinstance(expected, dict) and expected.get("state") and expected.get("code"), f"expected result missing: {case.get('id')}")
    required_states = {
        "stale-success": "Expired",
        "safe-mode-gameplay": "Blocked",
        "toggle-harmony": "RestartRequired",
    }
    for case_id, state in required_states.items():
        require(by_id[case_id]["expected"]["state"] == state, f"invalid state mismatch: {case_id}")
    return len(cases)


def validate_expected(document: dict[str, Any], catalog_counts: dict[str, Any], invalid_count: int) -> None:
    valid = document.get("valid")
    require(isinstance(valid, dict) and valid.get("state") == "Valid", "valid expected state mismatch")
    require(valid.get("contracts") == len(catalog_counts["ids"]), "expected contract count mismatch")
    require(valid.get("states") == len(catalog_counts["states"]), "expected health state count mismatch")
    require(valid.get("runtimeFrequency") == "0", "expected runtime frequency mismatch")
    require(document.get("invalidCaseCount") == invalid_count, "expected invalid count mismatch")
    invariants = document.get("invariants")
    require(isinstance(invariants, list) and 1 <= len(invariants) <= MAX_ISSUES, "invariants must be bounded")


def run(root: Path) -> dict[str, Any]:
    catalog = load_json(root / "valid-foundation-contract-catalog.json")
    invalid = load_json(root / "invalid-cases.json")
    expected = load_json(root / "expected-results.json")
    counts = validate_catalog(catalog)
    invalid_count = validate_invalid_cases(invalid)
    validate_expected(expected, counts, invalid_count)
    return {
        "state": "PASS",
        "contracts": len(counts["ids"]),
        "healthStates": len(counts["states"]),
        "invalidCases": invalid_count,
        "execution": "metadata-only; no Bannerlord/runtime/network access",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate AF Foundation runtime contract fixtures")
    parser.add_argument(
        "--fixture-root",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "docs" / "fixtures" / "phase3-foundation-runtime",
    )
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    try:
        result = run(args.fixture_root)
    except FoundationFailure as exc:
        print(json.dumps({"state": "FAIL", "error": str(exc)[:MAX_TEXT_LENGTH]}, ensure_ascii=False), file=sys.stderr)
        return 1
    if args.json:
        print(json.dumps(result, ensure_ascii=False, separators=(",", ":")))
    else:
        print("PASS foundationContracts contracts={contracts} healthStates={healthStates} invalidCases={invalidCases} execution={execution}".format(**result))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())