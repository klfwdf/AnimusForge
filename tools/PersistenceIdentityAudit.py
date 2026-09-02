#!/usr/bin/env python3
"""Read-only save/identity audit against the AF refactor baseline commit."""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_BASELINE = "d4cb1467376c6e923f4295dcefc7878c11dbc7c1"
SYNC = re.compile(r'SyncData\s*(?:<[^>\r\n]+>)?\s*\(\s*"([^"\r\n]+)"\s*,\s*ref\s+([A-Za-z_][A-Za-z0-9_]*)')
DECL = re.compile(
    r'(?m)^\s*(?:(?:public|private|protected|internal|static|readonly|volatile|const)\s+)+'
    r'([A-Za-z_][A-Za-z0-9_]*(?:\s*<[^\n;=]+>)?(?:\[\])?)\s+'
    r'([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;)'
)
BEHAVIOR = re.compile(
    r'\bclass\s+([A-Za-z_][A-Za-z0-9_]*)[^\{]{0,500}\bCampaignBehaviorBase\b',
    re.DOTALL,
)


def git_show(commit: str, relative: str) -> str | None:
    try:
        return subprocess.check_output(
            ["git", "show", f"{commit}:{relative}"],
            cwd=ROOT,
            encoding="utf-8",
            errors="replace",
            stderr=subprocess.DEVNULL,
        )
    except subprocess.CalledProcessError:
        return None


def sync_bindings(source: str) -> set[tuple[str, str]]:
    declarations = list(DECL.finditer(source))
    result: set[tuple[str, str]] = set()
    for match in SYNC.finditer(source):
        ref_name = match.group(2)
        candidates = [
            (declaration.start(), declaration.group(1).strip())
            for declaration in declarations
            if declaration.group(2) == ref_name and declaration.start() < match.start()
        ]
        result.add((match.group(1), candidates[-1][1] if candidates else "UNRESOLVED"))
    return result


def production_sources() -> list[Path]:
    paths = []
    for path in ROOT.rglob("*.cs"):
        if any(part in {"tools", "bin", "obj", ".tmp", "tmp", ".codex_tmp", "artifacts", "_deps_auto", ".dotnet", ".dotnet_cli"} for part in path.parts):
            continue
        if any("原版游戏本体代码" in part for part in path.parts):
            continue
        paths.append(path)
    return sorted(paths)


def current_sync() -> set[tuple[str, str]]:
    values: set[tuple[str, str]] = set()
    for path in production_sources():
        values |= sync_bindings(path.read_text(encoding="utf-8", errors="replace"))
    return values


def baseline_sync(commit: str) -> set[tuple[str, str]]:
    values: set[tuple[str, str]] = set()
    files = subprocess.check_output(
        ["git", "ls-tree", "-r", "--name-only", commit],
        cwd=ROOT,
        encoding="utf-8",
        errors="replace",
    ).splitlines()
    for relative in files:
        if not relative.endswith(".cs"):
            continue
        if relative.startswith("tools/") or relative.startswith("原版游戏本体代码"):
            continue
        source = git_show(commit, relative)
        if source and "SyncData" in source:
            values |= sync_bindings(source)
    return values


def behavior_names(source: str) -> set[str]:
    return {match.group(1) for match in BEHAVIOR.finditer(source)}


def current_behaviors() -> set[str]:
    result: set[str] = set()
    for path in production_sources():
        result |= behavior_names(path.read_text(encoding="utf-8", errors="replace"))
    return result


def baseline_behaviors(commit: str) -> set[str]:
    result: set[str] = set()
    files = subprocess.check_output(
        ["git", "ls-tree", "-r", "--name-only", commit],
        cwd=ROOT,
        encoding="utf-8",
        errors="replace",
    ).splitlines()
    for relative in files:
        if not relative.endswith(".cs"):
            continue
        if relative.startswith("tools/") or relative.startswith("原版游戏本体代码"):
            continue
        source = git_show(commit, relative)
        if source:
            result |= behavior_names(source)
    return result


def module_identity(relative: str) -> tuple[str, str, list[str]]:
    root = ET.parse(ROOT / relative).getroot()
    name = root.find("./Name").attrib.get("value", "")
    module_id = root.find("./Id").attrib.get("value", "")
    assemblies = [node.attrib.get("value", "") for node in root.findall(".//Assembly")]
    return name, module_id, assemblies


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--baseline", default=DEFAULT_BASELINE)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()

    current = current_sync()
    baseline = baseline_sync(args.baseline)
    current_beh = current_behaviors()
    baseline_beh = baseline_behaviors(args.baseline)
    name, module_id, assemblies = module_identity("AnimusForge/SubModule.xml")
    result = {
        "status": "PASS",
        "baseline": args.baseline,
        "syncCurrent": len(current),
        "syncBaseline": len(baseline),
        "syncAdded": sorted(current - baseline),
        "syncRemoved": sorted(baseline - current),
        "behaviorCurrent": len(current_beh),
        "behaviorBaseline": len(baseline_beh),
        "behaviorAdded": sorted(current_beh - baseline_beh),
        "behaviorRemoved": sorted(baseline_beh - current_beh),
        "moduleName": name,
        "moduleId": module_id,
        "moduleAssemblies": assemblies,
    }
    if current != baseline:
        result["status"] = "FAIL"
    if current_beh != baseline_beh:
        result["status"] = "FAIL"
    if name != "AnimusForge" or module_id != "AnimusForge" or assemblies != ["AnimusForge.Bootstrap.dll"]:
        result["status"] = "FAIL"
    if args.json:
        import json
        print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    else:
        if result["status"] == "PASS":
            print(f"PASS persistenceIdentity sync={len(current)} behavior={len(current_beh)} module=AnimusForge bootstrap=1")
        else:
            print(f"FAIL persistenceIdentity {result}")
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
