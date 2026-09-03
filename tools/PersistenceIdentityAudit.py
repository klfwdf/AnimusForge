#!/usr/bin/env python3
"""Read-only save/identity audit against the AF refactor baseline commit."""
from __future__ import annotations

import argparse
import json
import os
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


def progress(message: str, quiet: bool) -> None:
    if not quiet:
        print(message, file=sys.stderr)


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


def current_source_snapshot() -> list[tuple[Path, str]]:
    paths = production_sources()
    snapshot: list[tuple[Path, str]] = []
    for path in paths:
        snapshot.append((path, path.read_text(encoding="utf-8", errors="replace")))
    return snapshot


def current_sync(snapshot: list[tuple[Path, str]] | None = None) -> set[tuple[str, str]]:
    values: set[tuple[str, str]] = set()
    for _path, source in snapshot if snapshot is not None else current_source_snapshot():
        values |= sync_bindings(source)
    return values


def parse_batch_cat_file(data: bytes, expected_objects: int | None = None) -> dict[str, str]:
    """Parse git cat-file --batch output without spawning one process/object."""
    result: dict[str, str] = {}
    offset = 0
    records = 0
    while offset < len(data):
        header_end = data.find(b"\n", offset)
        if header_end < 0:
            raise ValueError("truncated git cat-file batch header")
        header = data[offset:header_end].decode("ascii", errors="strict").split()
        offset = header_end + 1
        records += 1
        if len(header) >= 2 and header[1] == "missing":
            continue
        if len(header) != 3 or header[1] != "blob":
            raise ValueError("unexpected git cat-file batch response")
        object_id = header[0]
        try:
            size = int(header[2])
        except ValueError as exc:
            raise ValueError("invalid git cat-file blob size") from exc
        if size < 0 or offset + size >= len(data):
            raise ValueError("truncated git cat-file batch blob")
        payload = data[offset:offset + size]
        offset += size
        if offset >= len(data) or data[offset:offset + 1] != b"\n":
            raise ValueError("missing git cat-file batch separator")
        offset += 1
        result[object_id] = payload.decode("utf-8", errors="replace")
    if expected_objects is not None and records != expected_objects:
        raise ValueError("git cat-file batch object count mismatch")
    return result


def baseline_source_snapshot(commit: str) -> list[tuple[str, str]]:
    git_env = os.environ.copy()
    git_env["GIT_NO_LAZY_FETCH"] = "1"
    tree = subprocess.run(
        ["git", "--no-optional-locks", "-c", "core.fsmonitor=false", "ls-tree", "-r", "--format=%(objectname)\t%(path)", commit],
        cwd=ROOT, capture_output=True, check=False, env=git_env,
    )
    if tree.returncode != 0:
        raise RuntimeError("cannot load baseline tree")
    objects: list[tuple[str, str]] = []
    for raw in tree.stdout.splitlines():
        try:
            object_id, relative_bytes = raw.split(b"\t", 1)
        except ValueError as exc:
            raise ValueError("malformed baseline tree entry") from exc
        relative = relative_bytes.decode("utf-8", errors="strict")
        if (not relative.endswith(".cs")
                or relative.startswith("tools/")
                or relative.startswith("原版游戏本体代码")):
            continue
        objects.append((object_id.decode("ascii"), relative))
    if not objects:
        return []
    process = subprocess.Popen(
        ["git", "--no-optional-locks", "-c", "core.fsmonitor=false", "cat-file", "--batch"],
        cwd=ROOT, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=git_env,
    )
    request = ("".join(object_id + "\n" for object_id, _ in objects)).encode("ascii")
    stdout, stderr = process.communicate(request, timeout=120)
    if process.returncode != 0:
        raise RuntimeError("baseline source batch read failed: " + stderr.decode(errors="replace")[:160])
    blobs = parse_batch_cat_file(stdout, expected_objects=len(objects))
    missing = [object_id for object_id, _relative in objects if object_id not in blobs]
    if missing:
        raise RuntimeError("baseline source blob unavailable (" + str(len(missing)) + " missing)")
    return [(relative, blobs[object_id]) for object_id, relative in objects]


def behavior_names(source: str) -> set[str]:
    return {match.group(1) for match in BEHAVIOR.finditer(source)}


def current_behaviors(snapshot: list[tuple[Path, str]] | None = None) -> set[str]:
    result: set[str] = set()
    for _path, source in snapshot if snapshot is not None else current_source_snapshot():
        result |= behavior_names(source)
    return result


def baseline_behaviors(commit: str, snapshot: list[tuple[str, str]] | None = None) -> set[str]:
    result: set[str] = set()
    for _relative, source in snapshot if snapshot is not None else baseline_source_snapshot(commit):
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
    parser.add_argument("--quiet", action="store_true")
    args = parser.parse_args()
    try:
        progress("current source enumeration", args.quiet)
        current_snapshot = current_source_snapshot()
        progress(f"current source loading ({len(current_snapshot)} files)", args.quiet)
        current = current_sync(current_snapshot)
        current_beh = current_behaviors(current_snapshot)
        progress("baseline tree loading", args.quiet)
        progress("baseline source batch reading", args.quiet)
        baseline_snapshot = baseline_source_snapshot(args.baseline)
        baseline = current_sync([(Path(relative), source) for relative, source in baseline_snapshot])
        baseline_beh = baseline_behaviors(args.baseline, baseline_snapshot)
        progress("comparison complete", args.quiet)
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
        if current != baseline or current_beh != baseline_beh or name != "AnimusForge" or module_id != "AnimusForge" or assemblies != ["AnimusForge.Bootstrap.dll"]:
            result["status"] = "FAIL"
    except Exception as exc:
        result = {"status": "FAIL", "error": str(exc)[:240]}
        if not args.quiet:
            print("persistence identity audit failed: " + result["error"], file=sys.stderr)
        if args.json:
            print(json.dumps(result, ensure_ascii=False, sort_keys=True))
        else:
            print("FAIL persistenceIdentity " + result["error"])
        return 1
    if args.json:
        print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    else:
        if result["status"] == "PASS":
            print(f"PASS persistenceIdentity sync={len(current)} behavior={len(current_beh)} module=AnimusForge bootstrap=1")
        else:
            print(f"FAIL persistenceIdentity {result}")
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
