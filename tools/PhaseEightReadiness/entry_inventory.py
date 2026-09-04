#!/usr/bin/env python3
"""Build the reviewed Phase 8 domain entry candidate inventory."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

RULES = {
    "economy-reward-debt": ("RewardSystemBehavior*.cs",),
    "policy-political": ("PolicySystem/Core/CustomPolicyBehavior*.cs",),
    "settlement-siege-gccz-sets": ("SiegeAiInterventionBehavior*.cs",),
    "duel": ("DuelBehavior*.cs",),
    "courier-proactive-issue": ("CourierDeliveryBehavior*.cs", "Refactor/Runtime/CourierInboundCompletion*.cs"),
    "social-progression-reports": ("PlayerNotorietyBehavior*.cs",),
    "game-adapter-compatibility": ("PlayerEncounterCompat.cs",),
    "action-commit": ("Refactor/Runtime/DetachedInteractionHost.cs",),
}
CATALOG_PATH = Path("docs/phase8/full-domain-readiness-catalog.json")
EXCLUDED_PARTS = {
    ".tmp", "_deps_auto", "artifacts", "bin", "modules", "obj", "terminal", "tools",
}


def _excluded(path: Path) -> bool:
    lowered = {part.lower() for part in path.parts}
    if lowered & EXCLUDED_PARTS:
        return True
    if any("terminal" in part.lower() for part in path.parts):
        return True
    if any("原版游戏" in part for part in path.parts):
        return True
    if path.name.lower().endswith((".g.cs", ".designer.cs")):
        return True
    return False


def _matches(project: Path, pattern: str) -> list[str]:
    paths: set[str] = set()
    for path in project.glob(pattern):
        if path.is_file() and not _excluded(path.relative_to(project)):
            paths.add(path.relative_to(project).as_posix())
    return sorted(paths)


def build_explained_inventory(project: Path) -> dict[str, list[dict[str, object]]]:
    project = project.resolve(strict=True)
    result: dict[str, list[dict[str, object]]] = {}
    for domain_id, patterns in RULES.items():
        candidates: dict[str, set[str]] = {}
        for pattern in patterns:
            for path in _matches(project, pattern):
                candidates.setdefault(path, set()).add("reviewed-pattern:" + pattern)
        if not candidates:
            raise ValueError(f"no candidate entry paths found for {domain_id}")
        result[domain_id] = [
            {"path": path, "sourceReasons": sorted(candidates[path])}
            for path in sorted(candidates)
        ]
    return result


def build_inventory(project: Path) -> dict[str, list[str]]:
    return {
        domain_id: [item["path"] for item in candidates]
        for domain_id, candidates in build_explained_inventory(project).items()
    }


def check_catalog(project: Path, *, require_preparation_state: bool = False) -> list[str]:
    inventory = build_inventory(project)
    document = json.loads((project / CATALOG_PATH).read_text(encoding="utf-8"))
    domains = {item["id"]: item for item in document.get("domains", [])}
    errors: list[str] = []
    for domain_id, candidates in inventory.items():
        if domain_id not in domains:
            errors.append(f"missing domain: {domain_id}")
            continue
        actual = domains[domain_id].get("entryPaths", [])
        missing = sorted(set(candidates) - set(actual))
        if missing:
            errors.append(f"{domain_id} missing: {', '.join(missing)}")
        if require_preparation_state:
            if domains[domain_id].get("entryCoverage") != "REPRESENTATIVE":
                errors.append(f"{domain_id} entryCoverage changed")
            if domains[domain_id].get("ownerAssignmentState") != "ROLE_PLACEHOLDER":
                errors.append(f"{domain_id} ownerAssignmentState changed")
    return errors


def update_catalog(project: Path) -> None:
    inventory = build_inventory(project)
    path = project / CATALOG_PATH
    document = json.loads(path.read_text(encoding="utf-8"))
    for domain in document["domains"]:
        if domain["id"] in inventory:
            domain["entryPaths"] = sorted(set(domain["entryPaths"]) | set(inventory[domain["id"]]))
    path.write_text(json.dumps(document, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--update", action="store_true")
    parser.add_argument("--require-preparation-state", action="store_true")
    args = parser.parse_args()
    try:
        if args.update:
            update_catalog(args.project_root)
        errors = check_catalog(args.project_root, require_preparation_state=args.require_preparation_state)
        if args.check:
            if errors:
                for error in errors:
                    print("FAIL " + error, file=sys.stderr)
                return 1
            print("PASS Phase8 entry inventory")
            return 0
        print(json.dumps(build_explained_inventory(args.project_root), ensure_ascii=False, indent=2))
        return 0
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        print("FAIL " + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
