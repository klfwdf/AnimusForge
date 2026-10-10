#!/usr/bin/env python3
"""Validate paired English/zh-CN XML localization resources."""

from __future__ import annotations

import re
import json
import sys
import xml.etree.ElementTree as ET
import xml.dom.minidom as DOM
from pathlib import Path


ID = re.compile(r"^[A-Za-z0-9_]+$")
VAR = re.compile(r"\{([A-Za-z][A-Za-z0-9_]*)\}")


def strings(path: Path) -> dict[str, str]:
    document = DOM.parse(str(path))
    if not path.read_text(encoding="utf-8-sig").startswith("<?xml") or document.firstChild.nodeName != "base":
        raise AssertionError(f"native loader requires XML declaration followed by base: {path}")
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
            if re.search(r"[\u3400-\u4dbf\u4e00-\u9fff]", en[key]):
                raise AssertionError(f"Chinese text in English localization resource: {english}:{key}")
            if key.startswith("AF_MEMORY_UI_") and ("\\n" in en[key] or "\\n" in zh[key]):
                raise AssertionError(f"literal newline escape in XML text: {key}; use &#10;")
            if sorted(VAR.findall(en[key])) != sorted(VAR.findall(zh[key])):
                raise AssertionError(f"placeholder mismatch: {key}")
    ui_en = strings(root / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml")
    ui_zh = strings(root / "content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml")
    required_ui_keys = {
        "AF_UI_Duel_StakeGold", "AF_UI_Duel_StakeItem", "AF_UI_Duel_StakeSeparator", "AF_UI_Duel_StakeNone",
        "AF_UI_Duel_StakeDebtIdSuffix", "AF_UI_Duel_StakeDueSuffix", "AF_UI_Duel_StakeNoteSuffix", "AF_UI_Duel_StakeDebtResult",
        "AF_UI_Duel_RenownLoserClan", "AF_UI_Duel_RenownWinnerClan", "AF_UI_Duel_RenownSettlement", "AF_UI_Duel_StakeNotSettled",
        "AF_UI_Duel_StakePaidPartially", "AF_UI_Duel_StakePaid", "AF_UI_Duel_StakeUnpaid", "AF_UI_Terminal_HotkeySet",
        "AF_UI_Weekly_Browser_PageStatus", "AF_UI_Weekly_Notice_Current",
    }
    missing_ui_keys = required_ui_keys - (set(ui_en) & set(ui_zh))
    if missing_ui_keys:
        raise AssertionError(f"Required confirmed AF_UI keys missing: {sorted(missing_ui_keys)}")
    weekly_path = root / "src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.Presentation.cs"
    weekly_source = weekly_path.read_text(encoding="utf-8-sig")
    weekly_keys = set(re.findall(r'"(AF_UI_Weekly_Bulletin_[A-Za-z0-9_]+)"', weekly_source))
    missing_weekly_keys = weekly_keys - (set(ui_en) & set(ui_zh))
    if missing_weekly_keys:
        raise AssertionError(f"Weekly bulletin display keys missing: {sorted(missing_weekly_keys)}")
    panel_source = (root / "src/modules/AF.Module.Weekly/Panel/WorldBulletinPanelVM.cs").read_text(encoding="utf-8-sig")
    panel_prefab = (root / "content/modules/AF.Module.Weekly/GUI/Prefabs/WorldBulletinPanel.xml").read_text(encoding="utf-8-sig")
    for field in ("MastheadText", "KindText", "HeadlineText", "MetaText"):
        if f"{field} = Sanitize(data.{field})" not in panel_source or f'Text="@{field}"' not in panel_prefab:
            raise AssertionError(f"World bulletin display binding missing: {field}")
    if 'BodyText="@BodyText"' not in panel_prefab or 'Text="@TagText"' not in panel_prefab or 'Text="@Text"' not in panel_prefab:
        raise AssertionError("World bulletin body/minor display bindings missing")
    if 'new WorldBulletinMinorItemVM("¶ " + Sanitize(minor.Key)' not in panel_source:
        raise AssertionError("World bulletin minor display tag no longer uses the projected key")
    if 'MajorKind = selection.Major.Kind ?? ""' not in weekly_source or 'MinorKinds = (selection.Minors ?? new List<WorldBulletinMinor>()).Select(m => m?.Events?.FirstOrDefault()?.Kind ?? "").ToList()' not in weekly_source:
        raise AssertionError("World bulletin internal kind IDs changed")
    display_source = weekly_source.split("internal WorldBulletinPanelData BuildWorldBulletinPanelData", 1)[1].split("internal static void SplitWorldBulletinBody", 1)[0]
    display_source += weekly_source.split("internal string BuildWorldBulletinMetaText", 1)[1].split("internal EventRecordEntry FindLatestWorldBulletinRecord", 1)[0]
    if re.search(r"[\u3400-\u4dbf\u4e00-\u9fff]", display_source):
        raise AssertionError("Fixed Chinese bulletin labels remain in the display projection")
    if "BodyText = major" not in display_source or "HeadlineText = _port.NoticeTitle(entry)" not in display_source:
        raise AssertionError("Raw bulletin body/headline facts changed at the display boundary")
    world_policy = (root / "src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs").read_text(encoding="utf-8-sig")
    if "Title = TitleForKind(lead.Kind)" not in world_policy or "Title = TitleForKind(selection?.Major?.Kind)" not in world_policy:
        raise AssertionError("World bulletin raw/model title projection changed")
    mapped = json.loads((root / "content/content-map.json").read_text(encoding="utf-8"))["entries"]
    registered = {path.resolve() for pair in pairs for path in pair}
    for entry in mapped:
        if not entry["target"].startswith("ModuleData/Languages/"):
            continue
        source = root / entry["source"]
        if ET.parse(source).getroot().attrib.get("type") == "string" and source.resolve() not in registered:
            raise AssertionError(f"mapped localization strings missing LanguageFile registration: {source}")
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
