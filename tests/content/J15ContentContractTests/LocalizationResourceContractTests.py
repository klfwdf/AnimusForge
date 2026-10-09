#!/usr/bin/env python3
"""Validate paired English/zh-CN XML localization resources."""

from __future__ import annotations

import re
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


ID = re.compile(r"^[A-Za-z0-9_]+$")
VAR = re.compile(r"\{([A-Z][A-Z0-9_]*)\}")


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
    content_map = json.loads((root / "content/content-map.json").read_text(encoding="utf-8"))
    mapped_targets = {entry["target"] for entry in content_map["entries"]}
    language_en = (root / "content/foundation/AF.Foundation.Localization/ModuleData/Languages/language_data.xml").read_text(encoding="utf-8")
    language_zh = (root / "content/foundation/AF.Foundation.Localization/ModuleData/Languages/CNs/language_data.xml").read_text(encoding="utf-8")
    if "ModuleData/Languages/af_ui_strings.xml" not in mapped_targets or "ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml" not in mapped_targets:
        raise AssertionError("AF UI localization resources are not mapped into the module")
    if 'xml_path="af_ui_strings.xml"' not in language_en or 'xml_path="CNs/af_ui_strings-zh-CN.xml"' not in language_zh:
        raise AssertionError("AF UI localization resources are not registered in LanguageData")
    if "ModuleData/Languages/afd_ui_strings.xml" not in mapped_targets or "ModuleData/Languages/CNs/afd_ui_strings-zh-CN.xml" not in mapped_targets:
        raise AssertionError("DialogueUI localization resources are not mapped into the module")
    if 'xml_path="afd_ui_strings.xml"' not in language_en or 'xml_path="CNs/afd_ui_strings-zh-CN.xml"' not in language_zh:
        raise AssertionError("DialogueUI localization resources are not registered in LanguageData")
    pairs = [
        (root / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml",
         root / "content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml"),
        (root / "content/modules/AnimusForge.XihaiAction/ModuleData/Languages/sceneactions_strings.xml",
         root / "content/modules/AnimusForge.XihaiAction/ModuleData/Languages/CNs/sceneactions_strings-zh-CN.xml"),
        (root / "content/modules/AnimusForge.DialogueUI/ModuleData/Languages/afd_ui_strings.xml",
         root / "content/modules/AnimusForge.DialogueUI/ModuleData/Languages/CNs/afd_ui_strings-zh-CN.xml"),
    ]
    for english, chinese in pairs:
        en = strings(english)
        zh = strings(chinese)
        if set(en) != set(zh):
            raise AssertionError(f"key mismatch: {english} vs {chinese}: EN-only={set(en)-set(zh)}, zh-only={set(zh)-set(en)}")
        for key in en:
            if sorted(VAR.findall(en[key])) != sorted(VAR.findall(zh[key])):
                raise AssertionError(f"placeholder mismatch: {key}")
    print(f"PASS localization resource pairs={len(pairs)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
