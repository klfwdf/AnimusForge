#!/usr/bin/env python3
"""Validate the design-only AF module/profile/health catalog.

This runner is independent from AnimusForge.csproj and Bannerlord. It validates
JSON metadata and never loads modules, assemblies, saves, or network resources.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

MAX_MODULES = 64
MAX_PROFILES = 16
MAX_HEALTH_ISSUES = 32
MAX_HEALTH_MESSAGE = 240
EXPECTED_MODULE_COUNT = 8
EXPECTED_PROFILE_COUNT = 3
EXPECTED_INVALID_COUNT = 16
ALLOWED_KINDS = {"foundation", "adapter", "module", "bridge"}
ALLOWED_ACTIVATION = {"boot-only", "save-load-boundary", "runtime-toggle-safe"}
ALLOWED_HEALTH_STATES = {
    "Discovered",
    "Disabled",
    "Blocked",
    "Starting",
    "Active",
    "Degraded",
    "Failed",
    "RestartRequired",
}
FORBIDDEN_KEY_PARTS = {
    "liveobject",
    "liveobjects",
    "gameobject",
    "apikey",
    "rawprompt",
    "rawresponse",
    "delegate",
    "methodinfo",
    "saveinstance",
}


class CatalogFailure(Exception):
    pass


def fail(message: str) -> None:
    raise CatalogFailure(message)


def require(condition: bool, message: str) -> None:
    if not condition:
        fail(message)


def load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        fail(f"cannot load {path}: {exc}")
    require(isinstance(value, dict), f"root must be object: {path}")
    return value


def reject_runtime_keys(value: Any, path: str = "root") -> None:
    if isinstance(value, dict):
        for key, child in value.items():
            normalized = str(key).replace("-", "_").lower()
            require(
                not any(part in normalized for part in FORBIDDEN_KEY_PARTS),
                f"runtime object payload key at {path}.{key}",
            )
            reject_runtime_keys(child, f"{path}.{key}")
    elif isinstance(value, list):
        for index, child in enumerate(value):
            reject_runtime_keys(child, f"{path}[{index}]")


def index_modules(catalog: dict[str, Any]) -> dict[str, dict[str, Any]]:
    modules = catalog.get("modules")
    require(isinstance(modules, list), "modules must be an array")
    require(0 < len(modules) <= MAX_MODULES, "module count is out of bounds")
    indexed: dict[str, dict[str, Any]] = {}
    namespaces: dict[str, str] = {}
    for module in modules:
        require(isinstance(module, dict), "module descriptor must be an object")
        module_id = module.get("id")
        require(isinstance(module_id, str) and module_id, "module id missing")
        require(module_id not in indexed, f"duplicate module id: {module_id}")
        require(module.get("kind") in ALLOWED_KINDS, f"invalid kind: {module_id}")
        require(isinstance(module.get("version"), str) and module["version"], f"version missing: {module_id}")
        require(module.get("contractVersion") == 1, f"contractVersion mismatch: {module_id}")
        require(isinstance(module.get("owner"), str) and module["owner"], f"owner missing: {module_id}")
        maintainers = module.get("maintainers")
        require(isinstance(maintainers, list) and maintainers, f"maintainers missing: {module_id}")
        profiles = module.get("profiles")
        require(isinstance(profiles, list) and profiles, f"profiles missing: {module_id}")
        require(module.get("entryTypeStatus") in {"Pending", "Bound"}, f"entryTypeStatus invalid: {module_id}")
        require(set(module.get("compatibility", {}).get("bannerlord", [])) == {"1.3", "1.4"}, f"API line closure invalid: {module_id}")
        lifecycle = module.get("lifecycle")
        require(isinstance(lifecycle, dict), f"lifecycle missing: {module_id}")
        require(lifecycle.get("activation") in ALLOWED_ACTIVATION, f"activation invalid: {module_id}")
        persistence = module.get("persistence")
        require(isinstance(persistence, dict), f"persistence missing: {module_id}")
        namespace = persistence.get("namespace")
        require(isinstance(namespace, str) and namespace, f"persistence namespace missing: {module_id}")
        if namespace != "none":
            require(namespace not in namespaces, f"persistence namespace conflict: {namespace}")
            namespaces[namespace] = module_id
        required = module.get("requiredModules")
        optional = module.get("optionalModules")
        require(isinstance(required, list) and isinstance(optional, list), f"dependency arrays missing: {module_id}")
        required_ids = [entry.get("id") for entry in required if isinstance(entry, dict)]
        require(len(required_ids) == len(required), f"required dependency shape invalid: {module_id}")
        require(len(required_ids) == len(set(required_ids)), f"duplicate required dependency: {module_id}")
        if module.get("kind") == "bridge":
            require(len(required_ids) >= 2, f"bridge must have at least two peers: {module_id}")
            require(namespace.startswith("bridge."), f"bridge namespace must be separate: {module_id}")
        if lifecycle.get("activation") == "runtime-toggle-safe":
            for field in ("harmonyPatches", "campaignBehavior", "missionBehavior", "applicationTick", "engineTick", "backgroundWork"):
                require(not lifecycle.get(field, False), f"runtime-toggle-safe contradiction {module_id}.{field}")
            require(namespace == "none", f"runtime-toggle-safe persistence contradiction: {module_id}")
        indexed[module_id] = module
    return indexed


def validate_dependency_graph(modules: dict[str, dict[str, Any]]) -> None:
    edges: dict[str, list[str]] = {}
    for module_id, module in modules.items():
        dependencies = []
        for entry in module["requiredModules"]:
            dependency_id = entry.get("id")
            require(dependency_id in modules, f"required dependency missing: {module_id} -> {dependency_id}")
            dependencies.append(dependency_id)
        edges[module_id] = dependencies
    visiting: set[str] = set()
    visited: set[str] = set()

    def visit(module_id: str) -> None:
        if module_id in visiting:
            fail(f"dependency cycle detected at {module_id}")
        if module_id in visited:
            return
        visiting.add(module_id)
        for dependency_id in edges[module_id]:
            visit(dependency_id)
        visiting.remove(module_id)
        visited.add(module_id)

    for module_id in modules:
        visit(module_id)


def validate_profiles(catalog: dict[str, Any], modules: dict[str, dict[str, Any]]) -> int:
    profiles = catalog.get("profiles")
    require(isinstance(profiles, dict), "profiles must be an object")
    require(0 < len(profiles) <= MAX_PROFILES, "profile count is out of bounds")
    for profile_id, profile in profiles.items():
        require(isinstance(profile, dict), f"profile must be object: {profile_id}")
        includes = profile.get("includes")
        excludes = profile.get("excludes")
        require(isinstance(includes, list) and isinstance(excludes, list), f"profile lists missing: {profile_id}")
        require(len(includes) == len(set(includes)), f"duplicate profile include: {profile_id}")
        require(not set(includes) & set(excludes), f"profile include/exclude conflict: {profile_id}")
        for module_id in includes + excludes:
            require(module_id in modules, f"profile references unknown module: {profile_id} -> {module_id}")
        for module_id in includes:
            for dependency in modules[module_id]["requiredModules"]:
                require(dependency["id"] in includes, f"profile closure violation: {profile_id} -> {module_id} requires {dependency['id']}")
        if profile_id == "safe-mode":
            require(set(includes) == {"af.foundation.runtime", "af.game-adapter"}, "safe-mode must contain only foundation and adapter")
            require(not any(modules[module_id]["kind"] in {"module", "bridge"} for module_id in includes), "safe-mode includes gameplay/bridge")
    require(set(profiles) == {"single-player", "safe-mode", "developer"}, "unexpected profile set")
    return len(profiles)


def validate_health(catalog: dict[str, Any]) -> None:
    health = catalog.get("healthCatalog")
    require(isinstance(health, dict), "healthCatalog missing")
    require(set(health.get("states", [])) == ALLOWED_HEALTH_STATES, "health states mismatch")
    require(health.get("maxIssues") == MAX_HEALTH_ISSUES, "health issue bound mismatch")
    require(health.get("maxMessageLength") == MAX_HEALTH_MESSAGE, "health message bound mismatch")


def validate_invalid_fixture(document: dict[str, Any]) -> int:
    cases = document.get("cases")
    require(isinstance(cases, list) and len(cases) == EXPECTED_INVALID_COUNT, "invalid case count mismatch")
    ids = [case.get("id") for case in cases if isinstance(case, dict)]
    require(len(ids) == EXPECTED_INVALID_COUNT and len(set(ids)) == EXPECTED_INVALID_COUNT, "invalid case IDs must be unique")
    for case in cases:
        require(isinstance(case.get("mutation"), str) and case["mutation"], f"invalid mutation missing: {case.get('id')}")
        expected = case.get("expected")
        require(isinstance(expected, dict) and expected.get("state") and expected.get("code"), f"invalid expected result missing: {case.get('id')}")
    required_states = {
        "optional-provider-missing": "Degraded",
        "required-provider-missing": "Blocked",
        "runtime-toggle-harmony": "RestartRequired",
        "bridge-failure": "Failed",
    }
    by_id = {case["id"]: case for case in cases}
    for case_id, state in required_states.items():
        require(by_id[case_id]["expected"]["state"] == state, f"invalid state mismatch: {case_id}")
    return len(cases)


def validate_expected(document: dict[str, Any], modules: dict[str, dict[str, Any]], profile_count: int, invalid_count: int) -> None:
    require(document.get("validCatalog", {}).get("state") == "Valid", "expected valid state mismatch")
    require(document["validCatalog"].get("modules") == len(modules), "expected module count mismatch")
    require(document["validCatalog"].get("profiles") == profile_count, "expected profile count mismatch")
    require(document.get("requiredInvalidCount") == invalid_count, "expected invalid count mismatch")
    invariants = document.get("invariants")
    require(isinstance(invariants, list) and 1 <= len(invariants) <= MAX_HEALTH_ISSUES, "expected invariants must be bounded")


def run(root: Path) -> dict[str, Any]:
    catalog = load_json(root / "module-catalog.json")
    invalid = load_json(root / "invalid-cases.json")
    expected = load_json(root / "expected-results.json")
    reject_runtime_keys(catalog, "catalog")
    reject_runtime_keys(invalid, "invalid")
    reject_runtime_keys(expected, "expected")
    modules = index_modules(catalog)
    require(len(modules) == EXPECTED_MODULE_COUNT, "design catalog module count mismatch")
    validate_dependency_graph(modules)
    profile_count = validate_profiles(catalog, modules)
    validate_health(catalog)
    invalid_count = validate_invalid_fixture(invalid)
    validate_expected(expected, modules, profile_count, invalid_count)
    return {
        "state": "PASS",
        "modules": len(modules),
        "profiles": profile_count,
        "invalidCases": invalid_count,
        "healthStates": len(catalog["healthCatalog"]["states"]),
        "execution": "catalog-only; no Bannerlord/runtime/network access",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate AF phase-3 module catalog fixtures")
    parser.add_argument(
        "--fixture-root",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "docs" / "fixtures" / "phase3-module-catalog",
    )
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    try:
        result = run(args.fixture_root)
    except CatalogFailure as exc:
        result = {"state": "FAIL", "error": str(exc)[:MAX_HEALTH_MESSAGE]}
        print(json.dumps(result, ensure_ascii=False), file=sys.stderr)
        return 1
    if args.json:
        print(json.dumps(result, ensure_ascii=False, separators=(",", ":")))
    else:
        print("PASS moduleCatalog modules={modules} profiles={profiles} invalidCases={invalidCases} healthStates={healthStates} execution={execution}".format(**result))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())