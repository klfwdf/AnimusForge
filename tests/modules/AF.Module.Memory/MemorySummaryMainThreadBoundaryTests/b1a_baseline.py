"""Freeze the pre-B1a record declarations and sanitizer bodies from the product baseline.

The pinned Git revision remains a repeatable old-code oracle after the move. This
script intentionally reads no player data and writes only the specified evidence file.
"""
import hashlib
import json
import re
import subprocess
from pathlib import Path
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import current_source_path

ROOT = Path(__file__).resolve().parents[4]
BASE = "8ae0f831"
NAMES = (
    "DailyMemoryLine", "DailyMemoryDraft", "CompressedMemoryBlock",
    "WeeklyMemoryMaterialTrigger", "MemorySummaryJob", "MemoryOverviewState",
    "MemoryOverviewJob", "MajorActionSummaryState", "MajorActionSummaryJob",
)
SANITIZERS = (
    "SanitizeWeeklyMemoryMaterialTriggers", "SanitizeDailyMemoryDrafts",
    "SanitizeCompressedMemoryBlocks", "SanitizeMemorySummaryQueue",
    "NormalizeMemorySummaryQueue", "SanitizeMemoryOverviewState",
    "SanitizeMemoryOverviewQueue", "SanitizeMajorActionSummaryState",
    "SanitizeMajorActionSummaryQueue",
)


def declaration(source, signature):
    start = source.index(signature)
    opening = source.index("{", start)
    depth = 0
    for pos in range(opening, len(source)):
        if source[pos] == "{":
            depth += 1
        elif source[pos] == "}":
            depth -= 1
            if depth == 0:
                return source[start:pos + 1]
    raise ValueError("unclosed declaration: " + signature)


def fields(block):
    opening = block.index("{")
    depth = 1
    result = []
    for line in block[opening + 1:].splitlines():
        if depth == 1:
            hit = re.fullmatch(r"\s*public\s+([\w<>]+)\s+(\w+)(?:\s*=\s*(.*?))?\s*;\s*", line)
            if hit:
                typ, name, initial = hit.groups()
                result.append({"name": name, "type": typ, "initializer": initial,
                               "default": initial if initial is not None else "default"})
        depth += line.count("{") - line.count("}")
    return result


def sha(value):
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def main(path):
    old = subprocess.check_output(["git", "show", f"{BASE}:MyBehavior.cs"], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
    current = (current_source_path(ROOT, "MyBehavior.cs")).read_text(encoding="utf-8-sig").replace("\r\n", "\n")
    models = {}
    for name in NAMES:
        block = declaration(old, "private sealed class " + name)
        assert block in current, "product moved before baseline: " + name
        items = fields(block)
        models[name] = {"sourceSha256": sha(block), "fields": items,
                        "jsonFieldNamesInDeclarationOrder": [item["name"] for item in items],
                        "copyForSummarySha256": sha(declaration(block, "internal " + name + " CopyForSummary("))}
    assert sum(len(model["fields"]) for model in models.values()) == 95
    rules = {}
    for name in SANITIZERS:
        match = re.search(r"private (?:static )?[^\n]+\b" + name + r"\(", old)
        assert match, name
        block = declaration(old, match.group())
        assert block in current, "product changed before baseline: " + name
        rules[name] = {"sourceSha256": sha(block), "source": block}
    evidence = {"baselineRevision": BASE, "sourcePath": "MyBehavior.cs",
                "recordCount": len(models), "fieldCount": 95,
                "note": "Field list is C# public JSON-field shape; source bodies are the repeatable old sanitizer oracle. Runtime behavior still needs separately executed fixtures.",
                "models": models, "sanitizers": rules}
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(evidence, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"BASELINE_READY records={len(models)} fields=95 sanitizers={len(rules)} file={path}")


# Deliberately no __main__ entry: the repository's aggregate runner discovers
# runnable scripts by that marker. This is a pinned, explicitly invoked helper.
