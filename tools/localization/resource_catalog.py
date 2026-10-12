"""Resolve every loader-registered string resource through the content map."""
from __future__ import annotations

import json
import xml.etree.ElementTree as ET
from pathlib import Path


def registered_pairs(root: Path) -> list[tuple[Path, Path]]:
    entries = json.loads((root / "content/content-map.json").read_text(encoding="utf-8"))["entries"]
    mapped: dict[str, Path] = {}
    for entry in entries:
        target = entry["target"]
        if target in mapped:
            raise AssertionError(f"duplicate content-map target: {target}")
        mapped[target] = root / entry["source"]

    def resources(index_target: str) -> dict[str, Path]:
        index = ET.parse(mapped[index_target]).getroot()
        result: dict[str, Path] = {}
        for item in index.findall("LanguageFile"):
            relative = item.attrib["xml_path"]
            target = "ModuleData/Languages/" + relative
            if target not in mapped or not mapped[target].is_file():
                raise AssertionError(f"registered localization resource missing: {target}")
            # Vengeance names English with -en, most other modules omit it.
            name = Path(relative).stem.removesuffix("-zh-CN").removesuffix("-en")
            if name in result:
                raise AssertionError(f"duplicate LanguageFile: {relative}")
            result[name] = mapped[target]
        return result

    en = resources("ModuleData/Languages/language_data.xml")
    zh = resources("ModuleData/Languages/CNs/language_data.xml")
    if set(en) != set(zh):
        raise AssertionError(f"unpaired registered resources: EN-only={sorted(set(en)-set(zh))}, zh-only={sorted(set(zh)-set(en))}")
    return [(en[name], zh[name]) for name in sorted(en)]
