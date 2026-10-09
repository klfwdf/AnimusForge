#!/usr/bin/env python3
"""Extract Chinese literals and classify likely player-facing localization work.

The scanner is intentionally conservative: it reports every CJK occurrence, but
uses source context only to classify a record. It never rewrites source files.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


HAN = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]")
STRING = re.compile(r'"(?:\\.|[^"\\])*"')
EXCLUDED_PARTS = {"bin", "obj", ".git", "NuGet", "原版游戏本体代码1.3.x", "原版游戏本体代码1.4.5", "原版游戏本体代码1.5.4"}
ROOTS = (
    "src",
    "extensions",
    "PolicySystem",
    "Vengeance",
    "WarStats",
    "AnimusForge.SiegeAftermathIntervention",
    "WorldEvents",
    "content",
)


def category(path: str, line: str) -> str:
    lower = line.lower()
    if "//" in line and line.lstrip().startswith("//"):
        return "comment"
    if any(token in lower for token in ("prompt", "system", "contract", "instruction")):
        return "prompt_or_internal"
    if any(token in lower for token in ("logger", "log.", "debug", "trace")):
        return "log_or_diagnostic"
    if path.endswith((".xml", ".xaml")) or any(
        token in lower
        for token in ("textobject", "settingproperty", "title", "subtitle", "hint", "tooltip", "button", "notification", "message", "status")
    ):
        return "player_visible_ui"
    return "player_visible_or_review"


def included(path: Path) -> bool:
    return not any(part in EXCLUDED_PARTS for part in path.parts)


def scan(root: Path) -> list[dict[str, object]]:
    records: list[dict[str, object]] = []
    for root_name in ROOTS:
        base = root / root_name
        if not base.exists():
            continue
        for path in base.rglob("*"):
            if not path.is_file() or not included(path) or path.suffix.lower() not in {".cs", ".xml", ".json", ".xaml"}:
                continue
            try:
                lines = path.read_text(encoding="utf-8").splitlines()
            except UnicodeDecodeError:
                continue
            for line_number, line in enumerate(lines, 1):
                if not HAN.search(line):
                    continue
                literals = [m.group(0) for m in STRING.finditer(line) if HAN.search(m.group(0))]
                if not literals:
                    literals = [line.strip()]
                for literal in literals:
                    records.append(
                        {
                            "file": path.relative_to(root).as_posix(),
                            "line": line_number,
                            "category": category(path.as_posix(), line),
                            "literal": literal,
                            "has_localization_id": "{=" in literal,
                            "status": "existing_id" if "{=" in literal else "needs_review",
                        }
                    )
    return records


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    records = scan(args.root.resolve())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps({"schemaVersion": 1, "records": records}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"records={len(records)} output={args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
