#!/usr/bin/env python3
"""Snapshot conservative UI candidates into non-overlapping review packages."""

from __future__ import annotations

import argparse
import collections
import json
import re
import subprocess
from pathlib import Path


B_MODULES = {"Economy", "Social", "WorldMap", "Issue", "Conversation"}
C_MODULES = {"Kingdom", "Diplomacy", "Weekly", "Memory", "Knowledge"}


def package_for(path: str) -> str:
    if path.startswith("PolicySystem/") or "/PolicySystem/" in path:
        return "C"
    module = re.search(r"AF\.Module\.([A-Za-z]+)/", path)
    if module:
        if module[1] in B_MODULES:
            return "B"
        if module[1] in C_MODULES:
            return "C"
    if path.startswith("src/AF.GameAdapter.Bannerlord/"):
        if re.search(r"/(Economy|Social|WorldMap)/", path):
            return "B"
        if re.search(r"/(Kingdom|Diplomacy|Memory|Knowledge)/", path):
            return "C"
        if re.search(r"/(Memory|Weekly|Knowledge)[^/]*\.cs$", path):
            return "C"
    if path.startswith("extensions/AnimusForge.DialogueUI/"):
        return "B"
    return "A"


def candidate(row: dict) -> bool:
    path = row["file"]
    return row["category"] == "player_visible_ui" and not (
        "/Languages/" in path
        or path.endswith(".zh-CN.json")
        or path == "content/content-map.json"
        or any(part.lower() == "tests" for part in Path(path).parts)
        or "/PlayerExports/" in path
        or re.search(r"\.en(?:-US)?\.json$", path)
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--inventory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--file-lists-dir", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    inventory = json.loads(args.inventory.read_text(encoding="utf-8"))
    rows = [row for row in inventory["records"] if candidate(row)]
    revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    packages = {}
    for name in ("A", "B", "C"):
        selected = [row for row in rows if package_for(row["file"]) == name]
        counts = collections.Counter(row["file"] for row in selected)
        packages[name] = {
            "candidateCount": len(selected),
            "fileCount": len(counts),
            "files": dict(sorted(counts.items())),
            "records": selected,
        }
    assert sum(package["candidateCount"] for package in packages.values()) == len(rows)
    file_sets = [set(package["files"]) for package in packages.values()]
    assert not any(file_sets[i] & file_sets[j] for i in range(3) for j in range(i))
    snapshot = {
        "schemaVersion": 1,
        "sourceRevision": revision,
        "rawChineseRecords": len(inventory["records"]),
        "uiCandidateCount": len(rows),
        "fileCount": len(set(row["file"] for row in rows)),
        "countMeaning": "Conservative audit candidates, including comments and internal strings; not untranslated UI totals.",
        "packages": packages,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    lists_dir = args.file_lists_dir or args.output.parent
    lists_dir.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(snapshot, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    for name, package in packages.items():
        lines = ["# Localization Wave 2 " + name + " Candidate Files", "",
                 "Source: `" + revision + "`. Counts are audit candidates, not confirmed UI omissions.", "",
                 "| File | Candidates |", "| --- | ---: |"]
        lines.extend("| `" + path + "` | " + str(count) + " |" for path, count in package["files"].items())
        (lists_dir / ("localization-wave2-" + name.lower() + "-files.md")).write_text("\n".join(lines) + "\n", encoding="utf-8")
        print(f"{name}: candidates={package['candidateCount']} files={package['fileCount']}")
    print(f"total: candidates={len(rows)} files={snapshot['fileCount']} source={revision}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
