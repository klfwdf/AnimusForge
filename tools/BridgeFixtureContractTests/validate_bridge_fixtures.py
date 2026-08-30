#!/usr/bin/env python3
"""Validate phase-2 Settlement/Siege and Policy/Diplomacy bridge fixtures.

This runner is intentionally independent from AnimusForge.csproj and Bannerlord
assemblies. It validates bounded JSON metadata only; it never loads a game save,
starts a game, calls a network, or executes a bridge.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

MAX_CASES = 32
EXPECTED_SETTLEMENT_CASES = {
    "A-settlement-siege-alone",
    "B-conversation-scene-alone",
    "A-plus-B-without-bridge",
    "A-plus-B-plus-bridge",
    "bridge-failure-stale-generation",
}
EXPECTED_POLICY_CASES = {
    "A-policy-system-alone",
    "B-diplomacy-world-alone",
    "A-plus-B-without-bridge",
    "A-plus-B-plus-bridge",
    "bridge-failure-incompatible-version",
}
FORBIDDEN_KEY_PARTS = {
    "liveobject",
    "liveobjects",
    "gameobject",
    "api_key",
    "apikey",
    "rawprompt",
    "rawresponse",
    "delegate",
    "methodinfo",
    "saveinstance",
}


class FixtureFailure(Exception):
    pass


def load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise FixtureFailure(f"cannot load {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise FixtureFailure(f"root must be an object: {path}")
    return value


def assert_true(condition: bool, message: str) -> None:
    if not condition:
        raise FixtureFailure(message)


def case_map(document: dict[str, Any], label: str) -> dict[str, dict[str, Any]]:
    cases = document.get("cases")
    assert_true(isinstance(cases, list), f"{label}: cases must be an array")
    assert_true(0 < len(cases) <= MAX_CASES, f"{label}: case count must be 1..{MAX_CASES}")
    result: dict[str, dict[str, Any]] = {}
    for case in cases:
        assert_true(isinstance(case, dict), f"{label}: each case must be an object")
        case_id = case.get("id")
        assert_true(isinstance(case_id, str) and case_id, f"{label}: case id missing")
        assert_true(case_id not in result, f"{label}: duplicate case id {case_id}")
        result[case_id] = case
    return result


def assert_no_forbidden_keys(value: Any, path: str = "root") -> None:
    if isinstance(value, dict):
        for key, child in value.items():
            normalized = str(key).replace("-", "_").lower()
            assert_true(
                not any(part in normalized for part in FORBIDDEN_KEY_PARTS),
                f"forbidden runtime payload key at {path}.{key}",
            )
            assert_no_forbidden_keys(child, f"{path}.{key}")
    elif isinstance(value, list):
        for index, child in enumerate(value):
            assert_no_forbidden_keys(child, f"{path}[{index}]")


def assert_expected_case_ids(actual: dict[str, dict[str, Any]], expected: set[str], label: str) -> None:
    assert_true(set(actual) == expected, f"{label}: case ids differ; actual={sorted(actual)}")


def validate_settlement(document: dict[str, Any]) -> int:
    assert_true(document.get("contract") == "AF.Bridge.SettlementSiegeAction", "settlement: contract mismatch")
    assert_true(document.get("contractVersion") == 1, "settlement: contractVersion must be 1")
    cases = case_map(document, "settlement")
    assert_expected_case_ids(cases, EXPECTED_SETTLEMENT_CASES, "settlement")

    alone = cases["A-settlement-siege-alone"]
    assert_true(alone["bridge"] == "absent", "settlement A: bridge must be absent")
    assert_true(alone["expected"] == {"status": "Authorized", "fallback": "Native", "crossDomainAction": False}, "settlement A: unexpected result")

    conversation = cases["B-conversation-scene-alone"]
    assert_true(conversation["bridge"] == "absent", "settlement B: bridge must be absent")
    assert_true(conversation["expected"]["siegeRulesInjected"] is False, "settlement B: siege rules must stay absent")

    without_bridge = cases["A-plus-B-without-bridge"]
    assert_true(without_bridge["bridge"] == "absent", "settlement A+B: bridge must be absent")
    assert_true(without_bridge["expected"]["crossDomainAction"] is False, "settlement A+B: implicit cross-domain action")

    with_bridge = cases["A-plus-B-plus-bridge"]
    request = with_bridge.get("request", {})
    assert_true(with_bridge["bridge"] == "available", "settlement bridge case: bridge must be available")
    assert_true(request.get("targetIdentity", {}).get("agentIndex") == 7, "settlement bridge case: Agent identity missing")
    assert_true("postprocessRuleIds" in request and request["postprocessRuleIds"], "settlement bridge case: postprocess closure missing")
    assert_true(with_bridge["expected"]["mainThreadApply"] is True, "settlement bridge case: apply must be main-thread")
    assert_true(with_bridge["expected"]["confirmedFact"] is True, "settlement bridge case: fact must be confirmed")

    failure = cases["bridge-failure-stale-generation"]
    assert_true(failure["expected"]["status"] == "Expired", "settlement failure: stale result must expire")
    assert_true(failure["expected"]["crossDomainAction"] is False, "settlement failure: cross-domain side effect")
    assert_true(failure["expected"]["confirmedFact"] is False, "settlement failure: stale fact recorded")
    return len(cases)


def validate_policy(document: dict[str, Any]) -> int:
    assert_true(document.get("contract") == "AF.Bridge.PolicyDiplomacy", "policy: contract mismatch")
    assert_true(document.get("contractVersion") == 1, "policy: contractVersion must be 1")
    cases = case_map(document, "policy")
    assert_expected_case_ids(cases, EXPECTED_POLICY_CASES, "policy")

    policy_alone = cases["A-policy-system-alone"]
    assert_true(policy_alone["bridge"] == "absent", "policy A: bridge must be absent")
    assert_true(policy_alone["expected"]["worldNotification"] is False, "policy A: world notification must be absent")

    diplomacy_alone = cases["B-diplomacy-world-alone"]
    assert_true(diplomacy_alone["bridge"] == "absent", "policy B: bridge must be absent")
    assert_true(diplomacy_alone["expected"]["worldNotification"] is True, "policy B: native world notification expected")

    without_bridge = cases["A-plus-B-without-bridge"]
    assert_true(without_bridge["bridge"] == "absent", "policy A+B: bridge must be absent")
    assert_true(without_bridge["expected"]["worldNotification"] is False, "policy A+B: implicit world notification")
    assert_true(without_bridge["expected"]["implicitTrigger"] is False, "policy A+B: implicit trigger")

    with_bridge = cases["A-plus-B-plus-bridge"]
    request = with_bridge.get("request", {})
    assert_true(with_bridge["bridge"] == "available", "policy bridge case: bridge must be available")
    assert_true(request.get("targetPlanHandles"), "policy bridge case: canonical target plan missing")
    assert_true(request.get("policyModuleIds"), "policy bridge case: policy module id missing")
    assert_true(with_bridge["expected"]["receiptRequired"] is True, "policy bridge case: receipt required")
    assert_true(with_bridge["expected"]["confirmedFact"] is True, "policy bridge case: fact must be confirmed")

    failure = cases["bridge-failure-incompatible-version"]
    assert_true(failure["expected"]["status"] == "Incompatible", "policy failure: version mismatch must be incompatible")
    assert_true(failure["expected"]["worldNotification"] is False, "policy failure: world notification side effect")
    assert_true(failure["expected"]["policyReceiptPreserved"] is True, "policy failure: policy receipt must remain owned by PolicySystem")
    return len(cases)


def validate_expected_results(document: dict[str, Any], all_case_ids: set[str]) -> int:
    required = document.get("requiredCases")
    assert_true(isinstance(required, dict), "expected: requiredCases must be an object")
    listed = {case_id for values in required.values() for case_id in values}
    assert_true(listed == all_case_ids, "expected: required case IDs do not match composition fixtures")
    invariants = document.get("invariants")
    assert_true(isinstance(invariants, list) and 6 <= len(invariants) <= 32, "expected: invariant list must be bounded")
    return len(invariants)


def run(root: Path) -> dict[str, Any]:
    settlement_path = root / "settlement-siege-composition.json"
    policy_path = root / "policy-diplomacy-composition.json"
    expected_path = root / "expected-results.json"
    settlement = load_json(settlement_path)
    policy = load_json(policy_path)
    expected = load_json(expected_path)
    assert_no_forbidden_keys(settlement, "settlement")
    assert_no_forbidden_keys(policy, "policy")
    assert_no_forbidden_keys(expected, "expected")
    settlement_count = validate_settlement(settlement)
    policy_count = validate_policy(policy)
    expected_count = validate_expected_results(
        expected,
        EXPECTED_SETTLEMENT_CASES | EXPECTED_POLICY_CASES,
    )
    return {
        "state": "PASS",
        "contractFiles": 2,
        "expectedFiles": 1,
        "cases": settlement_count + policy_count,
        "invariants": expected_count,
        "execution": "fixture-only; no Bannerlord/runtime/network access",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate AF phase-2 Bridge JSON fixtures")
    parser.add_argument(
        "--fixture-root",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "docs" / "fixtures" / "phase2-settlement-policy-bridges",
    )
    parser.add_argument("--json", action="store_true", help="emit one bounded JSON result")
    args = parser.parse_args()
    try:
        result = run(args.fixture_root)
    except FixtureFailure as exc:
        result = {"state": "FAIL", "error": str(exc)[:240]}
        print(json.dumps(result, ensure_ascii=False), file=sys.stderr)
        return 1
    if args.json:
        print(json.dumps(result, ensure_ascii=False, separators=(",", ":")))
    else:
        print(
            "PASS bridgeFixtureCases={cases} invariants={invariants} execution={execution}".format(**result)
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())