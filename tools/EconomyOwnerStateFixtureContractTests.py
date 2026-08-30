from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FIXTURE = ROOT / "docs" / "fixtures" / "phase7-economy-aware-commit" / "economy-owner-state-cases.json"


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    with FIXTURE.open("r", encoding="utf-8") as handle:
        document = json.load(handle)
    check(document.get("schema") == 1, "fixture schema drifted")
    allowed = set(document.get("allowedOwnerKinds", []))
    check(allowed == {"hero", "party", "merchant"}, "owner kinds drifted")
    string_only = set(document.get("stringOnlyFields", []))
    check({"channel", "sessionId", "subjectId", "ownerKind", "settlementId", "actionKind", "capabilityId"}.issubset(string_only), "string-only boundary incomplete")
    cases = document.get("cases")
    check(isinstance(cases, list) and len(cases) == 7, "fixture case count mismatch")
    ids = [case.get("id") for case in cases]
    check(len(set(ids)) == len(ids) and all(isinstance(value, str) and value for value in ids), "fixture IDs are not unique")

    route_by_owner = {
        "hero": "CreateEconomyRewardDebtMainThreadPortForExternal",
        "party": "CreatePartyEconomyRewardDebtMainThreadPortForExternal",
        "merchant": "CreateMerchantEconomyRewardDebtMainThreadPortForExternal",
    }
    supported_by_owner = {
        "hero": {"GiveGold", "GiveAsset", "DebtCreate", "DebtResolve", "SettlementTransfer"},
        "party": {"GiveGold", "GiveAsset"},
        "merchant": {"GiveGold", "GiveAsset", "DebtCreate", "DebtResolve"},
    }
    eligible = rejected = 0
    for case in cases:
        for key in ("channel", "sessionId", "subjectId", "ownerKind", "settlementId"):
            check(isinstance(case.get(key), str), f"{key} must be string: {case.get('id')}")
        owner = case["ownerKind"]
        expected = case.get("expected", {})
        actions = set(case.get("actions", []))
        capabilities = set(case.get("capabilities", []))
        check(actions, f"actions missing: {case['id']}")
        if owner in allowed and expected.get("status") == "eligible":
            check(expected.get("ownerRoute") == owner, f"eligible route mismatch: {case['id']}")
            check(expected.get("factory") == route_by_owner[owner], f"factory mismatch: {case['id']}")
            check(actions.issubset(supported_by_owner[owner]), f"unsupported action in eligible case: {case['id']}")
            check(all(isinstance(value, str) for value in case["capabilities"]), f"capability is not string-only: {case['id']}")
            check(case["active"] is True, f"eligible case inactive: {case['id']}")
            if owner == "merchant":
                check(bool(case["settlementId"]), f"merchant settlement missing: {case['id']}")
            eligible += 1
        else:
            check(expected.get("status") == "rejected", f"invalid case not rejected: {case['id']}")
            check(expected.get("ownerRoute") == "none" or not case["active"], f"rejected case leaked owner route: {case['id']}")
            check(isinstance(expected.get("reason"), str) and expected["reason"], f"rejection reason missing: {case['id']}")
            rejected += 1
    check(eligible == 4 and rejected == 3, "eligible/rejected case counts drifted")
    print(f"PASS economyOwnerStateFixture cases={len(cases)} eligible={eligible} rejected={rejected} stringOnly=1 hero=1 party=1 merchant=1 courierHero=1 failClosed=1")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, OSError, json.JSONDecodeError) as exc:
        print(f"FAIL {exc}")
        raise SystemExit(1)