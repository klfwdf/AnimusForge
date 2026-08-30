#!/usr/bin/env python3
"""Validate design-only AF.Contracts metadata fixtures.

No Bannerlord or AnimusForge production assembly is loaded. The runner reads
bounded JSON metadata only and never starts a game, reads a save, calls a
network, or executes a capability/event.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

EXPECTED_CONTRACTS = 9
EXPECTED_EVENTS = 3
EXPECTED_CAPABILITIES = 6
EXPECTED_INVALID_CASES = 18
MAX_CONTRACTS = 64
MAX_EVENTS = 32
MAX_CAPABILITIES = 64
MAX_FIELDS = 32
MAX_TEXT_LENGTH = 240
ALLOWED_API_LINES = {"1.3", "1.4"}
ALLOWED_STATUS = {"DesignOnly", "Active", "Deprecated"}
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


class ContractFailure(Exception):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ContractFailure(message)


def load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ContractFailure(f"cannot load {path}: {exc}") from exc
    require(isinstance(value, dict), f"root must be object: {path}")
    return value


def unique_ids(items: list[dict[str, Any]], label: str) -> set[str]:
    ids: list[str] = []
    for item in items:
        require(isinstance(item, dict), f"{label}: item must be object")
        item_id = item.get("id")
        require(isinstance(item_id, str) and item_id, f"{label}: missing id")
        ids.append(item_id)
    require(len(ids) == len(set(ids)), f"{label}: duplicate ID")
    return set(ids)


def validate_catalog(catalog: dict[str, Any]) -> dict[str, Any]:
    require(catalog.get("schemaVersion") == 1, "catalog schemaVersion must be 1")
    require(catalog.get("mode") == "design-only", "catalog must remain design-only")
    contracts = catalog.get("contracts")
    events = catalog.get("events")
    capabilities = catalog.get("capabilities")
    require(isinstance(contracts, list) and len(contracts) <= MAX_CONTRACTS, "contract list invalid/bounded")
    require(isinstance(events, list) and len(events) <= MAX_EVENTS, "event list invalid/bounded")
    require(isinstance(capabilities, list) and len(capabilities) <= MAX_CAPABILITIES, "capability list invalid/bounded")
    require(len(contracts) == EXPECTED_CONTRACTS, "contract count mismatch")
    require(len(events) == EXPECTED_EVENTS, "event count mismatch")
    require(len(capabilities) == EXPECTED_CAPABILITIES, "capability count mismatch")
    contract_ids = unique_ids(contracts, "contracts")
    event_ids = unique_ids(events, "events")
    capability_ids = unique_ids(capabilities, "capabilities")
    for contract in contracts:
        require(contract.get("family") in {"Capability", "DTO"}, f"invalid contract family: {contract.get('id')}")
        require(contract.get("version") == 1, f"contract version mismatch: {contract.get('id')}")
        require(contract.get("owner"), f"contract owner missing: {contract.get('id')}")
        require(contract.get("status") in ALLOWED_STATUS, f"contract status invalid: {contract.get('id')}")
        require(set(contract.get("allowedApiLines", [])) == ALLOWED_API_LINES, f"API line closure invalid: {contract.get('id')}")
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
                or normalized.endswith("object")
            )
            require(not (is_forbidden_type or is_forbidden_shape), f"forbidden field {field}: {contract.get('id')}")
        require(contract.get("threadBoundary") in {"NoGameAccess", "BackgroundSnapshotOnly", "MainThreadApply"}, f"thread boundary invalid: {contract.get('id')}")
        require(contract.get("persistenceImpact") in {"None", "ReadsExisting", "LegacyCompatibilityRequired"}, f"persistence impact invalid: {contract.get('id')}")
    for event in events:
        require(event.get("version") == 1, f"event version mismatch: {event.get('id')}")
        require(event.get("owner"), f"event owner missing: {event.get('id')}")
        require(event.get("immutable") is True, f"event must be immutable: {event.get('id')}")
        require(event.get("payloadContract") in contract_ids, f"event payload not found: {event.get('id')}")
        if event.get("confirmedOnly"):
            require(event.get("payloadContract") in {"af.contracts.memory.exchange", "af.contracts.diplomacy.resolution"}, f"confirmed event payload mismatch: {event.get('id')}")
    for capability in capabilities:
        require(capability.get("provider"), f"capability provider missing: {capability.get('id')}")
        require(capability.get("version") == 1 and capability.get("contractVersion") == 1, f"capability version mismatch: {capability.get('id')}")
    rules = catalog.get("rules")
    require(isinstance(rules, dict), "catalog rules missing")
    require(rules.get("maxContracts") == MAX_CONTRACTS, "maxContracts mismatch")
    require(rules.get("maxEvents") == MAX_EVENTS, "maxEvents mismatch")
    require(rules.get("maxFieldsPerContract") == MAX_FIELDS, "maxFields mismatch")
    require(rules.get("maxTextLength") == MAX_TEXT_LENGTH, "max text bound mismatch")
    require(rules.get("publicPayloadAllowsLiveObjects") is False, "live objects allowed")
    require(rules.get("publicPayloadAllowsRawDictionary") is False, "raw dictionary allowed")
    require(rules.get("runtimeFrequency") == "0", "contract catalog must not run in tick")
    return {"contracts": contract_ids, "events": event_ids, "capabilities": capability_ids}


def validate_invalid_cases(document: dict[str, Any]) -> int:
    cases = document.get("cases")
    require(isinstance(cases, list) and len(cases) == EXPECTED_INVALID_CASES, "invalid case count mismatch")
    ids = unique_ids(cases, "invalid cases")
    require(len(ids) == EXPECTED_INVALID_CASES, "invalid case ID count mismatch")
    for case in cases:
        require(isinstance(case.get("mutation"), str) and case["mutation"], f"mutation missing: {case.get('id')}")
        expected = case.get("expected")
        require(isinstance(expected, dict) and expected.get("state") and expected.get("code"), f"expected result missing: {case.get('id')}")
    required_states = {
        "optional-provider-missing": "Degraded",
        "required-version-incompatible": "Blocked",
        "stale-event": "Expired",
        "safe-mode-gameplay-event": "Blocked",
    }
    by_id = {case["id"]: case for case in cases}
    for case_id, state in required_states.items():
        require(by_id[case_id]["expected"]["state"] == state, f"state mismatch: {case_id}")
    return len(cases)


def validate_expected(document: dict[str, Any], counts: dict[str, Any], invalid_count: int) -> None:
    valid = document.get("valid")
    require(isinstance(valid, dict) and valid.get("state") == "Valid", "valid expected state mismatch")
    require(valid.get("contracts") == len(counts["contracts"]), "expected contract count mismatch")
    require(valid.get("events") == len(counts["events"]), "expected event count mismatch")
    require(valid.get("capabilities") == len(counts["capabilities"]), "expected capability count mismatch")
    require(document.get("invalidCaseCount") == invalid_count, "expected invalid count mismatch")
    invariants = document.get("invariants")
    require(isinstance(invariants, list) and 1 <= len(invariants) <= MAX_EVENTS * 2, "invariant output must be bounded")


def run(root: Path) -> dict[str, Any]:
    catalog = load_json(root / "valid-contract-catalog.json")
    invalid = load_json(root / "invalid-contract-cases.json")
    expected = load_json(root / "expected-results.json")
    counts = validate_catalog(catalog)
    invalid_count = validate_invalid_cases(invalid)
    validate_expected(expected, counts, invalid_count)
    return {
        "state": "PASS",
        "contracts": len(counts["contracts"]),
        "events": len(counts["events"]),
        "capabilities": len(counts["capabilities"]),
        "invalidCases": invalid_count,
        "execution": "metadata-only; no Bannerlord/runtime/network access",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate AF.Contracts design fixtures")
    parser.add_argument(
        "--fixture-root",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "docs" / "fixtures" / "phase3-af-contracts",
    )
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    try:
        result = run(args.fixture_root)
    except ContractFailure as exc:
        print(json.dumps({"state": "FAIL", "error": str(exc)[:MAX_TEXT_LENGTH]}, ensure_ascii=False), file=sys.stderr)
        return 1
    if args.json:
        print(json.dumps(result, ensure_ascii=False, separators=(",", ":")))
    else:
        print("PASS afContracts contracts={contracts} events={events} capabilities={capabilities} invalidCases={invalidCases} execution={execution}".format(**result))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())