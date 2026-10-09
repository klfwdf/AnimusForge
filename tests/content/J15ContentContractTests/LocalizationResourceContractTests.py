#!/usr/bin/env python3
"""Validate paired English/zh-CN XML localization resources."""

from __future__ import annotations

import re
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


ID = re.compile(r"^[A-Za-z0-9_]+$")
VAR = re.compile(r"\{([A-Za-z][A-Za-z0-9_]*)\}")


def strings(path: Path) -> dict[str, str]:
    root = ET.parse(path).getroot()
    result: dict[str, str] = {}
    if root.tag != "base" or root.attrib.get("type") != "string":
        raise AssertionError(f"not a Bannerlord localization document: {path}")
    for item in root.findall("strings/string"):
        key = item.attrib.get("id", "")
        if not ID.match(key) or key in result:
            raise AssertionError(f"invalid or duplicate localization id: {path}:{key}")
        result[key] = item.attrib.get("text", "")
    if not result:
        raise AssertionError(f"no loader-visible strings: {path}")
    return result


def main() -> int:
    root = Path(__file__).resolve().parents[3]
    sys.path.insert(0, str(root / "tools/localization"))
    from resource_catalog import registered_pairs
    pairs = registered_pairs(root)
    all_ids: set[str] = set()
    for english, chinese in pairs:
        en = strings(english)
        zh = strings(chinese)
        if set(en) != set(zh):
            raise AssertionError(f"key mismatch: {english} vs {chinese}: EN-only={set(en)-set(zh)}, zh-only={set(zh)-set(en)}")
        duplicate = all_ids & set(en)
        if duplicate:
            raise AssertionError(f"duplicate IDs across loader resources: {sorted(duplicate)}")
        all_ids.update(en)
        for key in en:
            if sorted(VAR.findall(en[key])) != sorted(VAR.findall(zh[key])):
                raise AssertionError(f"placeholder mismatch: {key}")
    # Vengeance source code must never introduce a player-facing REX_ key without both language entries.
    vengeance_source = root / "Vengeance/Source"
    source_text = "\n".join(path.read_text(encoding="utf-8") for path in vengeance_source.rglob("*.cs"))
    referenced = set(re.findall(r"REX_[A-Za-z0-9_]+", source_text))
    vengeance_en = strings(root / "AnimusForge/ModuleData/Languages/vengeance_strings-en.xml")
    vengeance_zh = strings(root / "AnimusForge/ModuleData/Languages/CNs/vengeance_strings-zh-CN.xml")
    missing_en = referenced - set(vengeance_en)
    missing_zh = referenced - set(vengeance_zh)
    if missing_en or missing_zh:
        raise AssertionError(f"Vengeance source keys missing: EN={sorted(missing_en)} zh={sorted(missing_zh)}")
    print(f"PASS localization resource pairs={len(pairs)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
