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
    required_resources = [
        ("ModuleData/Languages/afconv_ui_strings.xml", "ModuleData/Languages/CNs/afconv_ui_strings-zh-CN.xml", "afconv_ui_strings.xml", "CNs/afconv_ui_strings-zh-CN.xml"),
        ("ModuleData/Languages/afeconomy_strings.xml", "ModuleData/Languages/CNs/afeconomy_strings-zh-CN.xml", "afeconomy_strings.xml", "CNs/afeconomy_strings-zh-CN.xml"),
        ("ModuleData/Languages/afknowledge_strings.xml", "ModuleData/Languages/CNs/afknowledge_strings-zh-CN.xml", "afknowledge_strings.xml", "CNs/afknowledge_strings-zh-CN.xml"),
        ("ModuleData/Languages/coup_strings.xml", "ModuleData/Languages/CNs/coup_strings-zh-CN.xml", "coup_strings.xml", "CNs/coup_strings-zh-CN.xml"),
        ("ModuleData/Languages/vengeance_strings-en.xml", "ModuleData/Languages/CNs/vengeance_strings-zh-CN.xml", "vengeance_strings-en.xml", "CNs/vengeance_strings-zh-CN.xml"),
        ("ModuleData/Languages/illustrator_strings.xml", "ModuleData/Languages/CNs/illustrator_strings-zh-CN.xml", "illustrator_strings.xml", "CNs/illustrator_strings-zh-CN.xml"),
    ]
    for en_target, zh_target, en_path, zh_path in required_resources:
        if en_target not in mapped_targets or zh_target not in mapped_targets:
            raise AssertionError(f"localization resources are not mapped: {en_target}")
        if f'xml_path="{en_path}"' not in language_en or f'xml_path="{zh_path}"' not in language_zh:
            raise AssertionError(f"localization resources are not registered: {en_path}")
    pairs = [
        (root / "content/modules/AF.Module.Conversation/ModuleData/Languages/afconv_ui_strings.xml",
         root / "content/modules/AF.Module.Conversation/ModuleData/Languages/CNs/afconv_ui_strings-zh-CN.xml"),
        (root / "content/modules/AF.Module.Economy/ModuleData/Languages/afeconomy_strings.xml",
         root / "content/modules/AF.Module.Economy/ModuleData/Languages/CNs/afeconomy_strings-zh-CN.xml"),
        (root / "content/modules/AF.Module.Knowledge/ModuleData/Languages/afknowledge_strings.xml",
         root / "content/modules/AF.Module.Knowledge/ModuleData/Languages/CNs/afknowledge_strings-zh-CN.xml"),
        (root / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml",
         root / "content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml"),
        (root / "content/modules/AnimusForge.XihaiAction/ModuleData/Languages/sceneactions_strings.xml",
         root / "content/modules/AnimusForge.XihaiAction/ModuleData/Languages/CNs/sceneactions_strings-zh-CN.xml"),
        (root / "content/modules/AnimusForge.DialogueUI/ModuleData/Languages/afd_ui_strings.xml",
         root / "content/modules/AnimusForge.DialogueUI/ModuleData/Languages/CNs/afd_ui_strings-zh-CN.xml"),
        (root / "content/modules/AnimusForge.Coup/ModuleData/Languages/coup_strings.xml",
         root / "content/modules/AnimusForge.Coup/ModuleData/Languages/CNs/coup_strings-zh-CN.xml"),
        (root / "content/modules/AnimusForge.Illustrator/ModuleData/Languages/illustrator_strings.xml",
         root / "content/modules/AnimusForge.Illustrator/ModuleData/Languages/CNs/illustrator_strings-zh-CN.xml"),
        (root / "AnimusForge/ModuleData/Languages/vengeance_strings-en.xml",
         root / "AnimusForge/ModuleData/Languages/CNs/vengeance_strings-zh-CN.xml"),
    ]
    for english, chinese in pairs:
        en = strings(english)
        zh = strings(chinese)
        if set(en) != set(zh):
            raise AssertionError(f"key mismatch: {english} vs {chinese}: EN-only={set(en)-set(zh)}, zh-only={set(zh)-set(en)}")
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
