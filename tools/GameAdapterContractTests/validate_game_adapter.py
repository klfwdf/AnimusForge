#!/usr/bin/env python3
"""Validate design-only Bannerlord GameAdapter API boundary fixtures."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

EXPECTED_CASES = 14
MAX_CASES = 32
MAX_MESSAGE_LENGTH = 240
ALLOWED_API_LINES = {"1.3", "1.4", "1.3|1.4", "unknown"}


class AdapterFailure(Exception):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AdapterFailure(message)


def load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise AdapterFailure(f"cannot load {path}: {exc}") from exc
    require(isinstance(value, dict), f"root must be object: {path}")
    return value


def index_cases(document: dict[str, Any]) -> dict[str, dict[str, Any]]:
    require(document.get("schemaVersion") == 1, "schemaVersion mismatch")
    require(document.get("mode") == "design-only", "fixture must remain design-only")
    limits = document.get("limits")
    require(isinstance(limits, dict), "limits missing")
    require(limits.get("maxCases") == MAX_CASES, "case bound mismatch")
    require(limits.get("maxMessageLength") == MAX_MESSAGE_LENGTH, "message bound mismatch")
    cases = document.get("cases")
    require(isinstance(cases, list) and len(cases) == EXPECTED_CASES, "case count mismatch")
    indexed: dict[str, dict[str, Any]] = {}
    for case in cases:
        require(isinstance(case, dict), "case must be object")
        case_id = case.get("id")
        require(isinstance(case_id, str) and case_id, "case id missing")
        require(case_id not in indexed, f"duplicate case id: {case_id}")
        require(case.get("apiLine") in ALLOWED_API_LINES, f"invalid API line: {case_id}")
        require(isinstance(case.get("expected"), dict), f"expected missing: {case_id}")
        indexed[case_id] = case
    expected_ids = {
        "api-13-supported", "api-14-supported", "api-13-missing-14-feature",
        "restart-3-args", "restart-4-args", "spawn-signature", "rewards-signature",
        "missing-member", "api-marker-mismatch", "unknown-version", "cached-reflection",
        "main-thread-apply", "package-boundary", "dual-build",
    }
    require(set(indexed) == expected_ids, "case ID set mismatch")
    return indexed


def validate_cases(cases: dict[str, dict[str, Any]]) -> None:
    require(cases["api-13-supported"]["expected"]["status"] == "Supported", "1.3 helper support missing")
    require(cases["api-13-supported"]["expected"]["reflectionCached"] is True, "1.3 reflection cache missing")
    require(cases["api-14-supported"]["expected"]["status"] == "Supported", "1.4 feature support missing")
    missing = cases["api-13-missing-14-feature"]["expected"]
    require(missing["status"] == "Unsupported" and missing["guessApiLine"] is False, "1.3 must not guess 1.4 feature")

    for case_id in ("restart-3-args", "restart-4-args"):
        expected = cases[case_id]["expected"]
        require(expected["status"] == "Supported" and expected["directNativeCall"] is False, f"restart helper boundary invalid: {case_id}")
    require(cases["spawn-signature"]["expected"]["wrongSignatureCall"] is False, "SpawnTroop wrong signature allowed")
    require(cases["rewards-signature"]["expected"]["patchOtherLine"] is False, "battle reward patch crossed API line")

    missing_member = cases["missing-member"]["expected"]
    require(missing_member["status"] == "Unsupported" and missing_member["boundedFailure"] is True, "missing member must fail closed")
    marker = cases["api-marker-mismatch"]["expected"]
    require(marker["status"] == "Blocked" and marker["loadOtherImplementation"] is False, "marker mismatch loaded fallback implementation")
    unknown = cases["unknown-version"]["expected"]
    require(unknown["status"] == "Blocked" and unknown["guessApiLine"] is False, "unknown version guessed")
    cache = cases["cached-reflection"]["expected"]
    require(cache["resolvePerCall"] is False, "reflection resolved per call")
    thread = cases["main-thread-apply"]["expected"]
    require(thread["status"] == "MainThreadApplyRequired" and thread["backgroundLiveMutation"] is False, "background live mutation allowed")
    package = cases["package-boundary"]["expected"]
    require(package["status"] == "Valid" and package["subModuleImplementationDeclarations"] == 0 and package["bootstrapDeclarations"] == 1, "package boundary mismatch")
    dual = cases["dual-build"]["expected"]
    require(dual["status"] == "Valid" and dual["implementations"] == 2 and dual["loadedAtRuntime"] == 1, "dual implementation selection mismatch")


def run(root: Path) -> dict[str, Any]:
    document = load_json(root / "api-boundary-matrix.json")
    cases = index_cases(document)
    validate_cases(cases)
    return {
        "state": "PASS",
        "cases": len(cases),
        "apiLines": 2,
        "helpers": 7,
        "execution": "metadata-only; no Bannerlord/runtime/reflection/network access",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate AF GameAdapter API fixtures")
    parser.add_argument("--fixture-root", type=Path, default=Path(__file__).resolve().parents[2] / "docs" / "fixtures" / "phase3-game-adapter-api")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    try:
        result = run(args.fixture_root)
    except AdapterFailure as exc:
        print(json.dumps({"state": "FAIL", "error": str(exc)[:MAX_MESSAGE_LENGTH]}, ensure_ascii=False), file=sys.stderr)
        return 1
    if args.json:
        print(json.dumps(result, ensure_ascii=False, separators=(",", ":")))
    else:
        print("PASS gameAdapter cases={cases} apiLines={apiLines} helpers={helpers} execution={execution}".format(**result))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())