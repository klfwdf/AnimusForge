"""Inventory every Chinese C# runtime literal, without UI/prompt/log exclusions.

This is a lexical source inventory, not a compiler/dataflow or live-game verdict.
Chinese localization assets and source comments are not runtime hardcoding.
"""
from __future__ import annotations
import argparse
import bisect
import collections
import csv
import hashlib
import json
from pathlib import Path
import re
import subprocess

from extract_localization_inventory import ROOTS, included
from inventory_untranslated import LEX, HAN, decode, module


def run(root: Path) -> dict:
    records, hashes, problems = [], {}, []
    for root_name in ROOTS:
        for path in sorted((root / root_name).rglob("*.cs")):
            if not included(path) or any(part.lower() in {"tests", "design"} for part in path.parts):
                continue
            relative = path.relative_to(root).as_posix()
            data = path.read_bytes()
            hashes[relative] = hashlib.sha256(data).hexdigest()
            try:
                source = data.decode("utf-8-sig")
            except UnicodeDecodeError:
                problems.append({"file": relative, "reason": "not_utf8"})
                continue
            lines = source.splitlines()
            newlines = [m.start() for m in re.finditer("\n", source)]
            for match in LEX.finditer(source):
                if match.lastgroup not in {"string", "char"}:
                    continue
                raw = match[0] if match.lastgroup == "string" else '"' + match[0][1:-1] + '"'
                value = decode(raw)
                if not HAN.search(value):
                    continue
                line = bisect.bisect_left(newlines, match.start()) + 1
                records.append({"file": relative, "line": line,
                                "column": match.start() - source.rfind("\n", 0, match.start()),
                                "module": module(path, root), "kind": match.lastgroup,
                                "text": value, "sourceLine": lines[line - 1].strip()})
    changed = [name for name, digest in hashes.items()
               if hashlib.sha256((root / name).read_bytes()).hexdigest() != digest]
    return {"schemaVersion": 1,
            "sourceRevision": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip(),
            "basis": "working tree including untracked source; no prompt/protocol/log/UI role exclusion",
            "scannedFiles": len(hashes), "literalOccurrences": len(records),
            "filesWithChineseLiterals": len({r["file"] for r in records}),
            "uniqueLiteralValues": len({r["text"] for r in records}),
            "modules": dict(collections.Counter(r["module"] for r in records).most_common()),
            "files": dict(collections.Counter(r["file"] for r in records).most_common()),
            "records": records, "sourceHashes": hashes,
            "problems": problems, "changedDuringScan": changed,
            "limitations": ["C# lexer heuristic; not a full parser (nested interpolation/raw strings need semantic review).",
                            "Includes source not proven part of the current compile item set.",
                            "A literal is an extraction task, not necessarily an untranslated visible string."]}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    report = run(args.root.resolve())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", errors="backslashreplace")
    with args.output.with_suffix(".csv").open("w", encoding="utf-8-sig", errors="backslashreplace", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=["file", "line", "column", "module", "kind", "text", "sourceLine"])
        writer.writeheader()
        writer.writerows(report["records"])
    print(json.dumps({key: report[key] for key in ("sourceRevision", "scannedFiles", "literalOccurrences", "filesWithChineseLiterals", "uniqueLiteralValues", "problems", "changedDuringScan")}, ensure_ascii=False))
    return 1 if report["problems"] or report["changedDuringScan"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
