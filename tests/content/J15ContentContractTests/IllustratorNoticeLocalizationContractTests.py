#!/usr/bin/env python3
"""Static UI/resource contracts; does not establish game or compiler acceptance."""

import json
import re
from pathlib import Path
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "extensions/AnimusForge.Illustrator/src"
RESOURCES = ROOT / "content/modules/AnimusForge.Illustrator/ModuleData/Languages"
HAN = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff]")
VARIABLE = re.compile(r"\{([A-Za-z][A-Za-z0-9_]*)\}")


def read_strings(path):
    items = ET.parse(path).getroot().findall("strings/string")
    result = {item.attrib["id"]: item.attrib["text"] for item in items}
    assert len(result) == len(items), f"Duplicate IDs: {path}"
    return result


def main():
    en_path = RESOURCES / "illustrator_strings.xml"
    zh_path = RESOURCES / "CNs/illustrator_strings-zh-CN.xml"
    en, zh = read_strings(en_path), read_strings(zh_path)
    assert en.keys() == zh.keys(), "Illustrator language key mismatch"
    for key in en:
        assert not HAN.search(en[key]), f"Chinese in English resource: {key}"
        assert sorted(VARIABLE.findall(en[key])) == sorted(VARIABLE.findall(zh[key])), key

    helper = (SOURCE / "UI/IllustratorUiText.cs").read_text(encoding="utf-8-sig")
    calls = re.findall(r'(?:Resolve|Format[23]?)\("(AF_ILL_(?:MISSION_|NOTIFY_|MENU_)[A-Z_]+)", "((?:\\.|[^"\\])*)"', helper)
    assert len(calls) == 24, f"Expected 24 notification/entry keys, found {len(calls)}"
    for key, fallback in calls:
        assert en[key] == fallback, f"English fallback differs: {key}"
        assert zh[key].strip(), f"Empty Chinese translation: {key}"

    runtime = (SOURCE / "Core/IllustratorRuntime.cs").read_text(encoding="utf-8-sig")
    mission = (SOURCE / "Core/MissionScreenshotIllustration.cs").read_text(encoding="utf-8-sig")
    menu = (SOURCE / "SubModule.cs").read_text(encoding="utf-8-sig")
    for source in (runtime, mission, menu):
        for line in source.splitlines():
            if any(token in line for token in ("InformationMessage(", "SetStatus(", "Finish(", "ShoutTextInputPopup.Show(", "ShowForMissionScreenshot(", "fail(")):
                # The result-error callback on this line retains the original diagnostic.
                ui_line = line.replace('_ => "\u6210\u56fe\u4fdd\u5b58\u5931\u8d25\u3002"', '_ => "diagnostic"')
                assert not HAN.search(ui_line), f"Unlocalized fixed UI text: {line.strip()}"
    assert not HAN.search(menu), "Unlocalized gallery entry"
    assert "IllustratorUiText.GalleryMenu" in menu
    request = mission.split("private static void Request(", 1)[1].split("private static void StartGeneration(", 1)[0]
    for diagnostic in ("\u5f53\u524d\u6ca1\u6709\u53ef\u4f9b\u622a\u56fe\u751f\u56fe\u7684\u73a9\u5bb6\u573a\u666f\uff0c\u6216\u751f\u56fe\u5c1a\u672a\u542f\u7528\u3002", "\u8bf7\u5148\u914d\u7f6e\u751f\u56fe API\u3002"):
        assert 'throw new InvalidOperationException("' + diagnostic + '")' in request, "Keep original validation diagnostic"
    assert 'diagnostics?.Finish("failed", ex.Message)' in runtime, "Keep raw worker diagnostics"
    assert 'Error = error?.Message ??' in runtime, "Keep raw result errors"
    assert "AssertMainThread();" in runtime.split("internal static void PublishGeneration(", 1)[1].split("var listeners", 1)[0]
    assert "IllustratorRuntime.AssertMainThread();" in mission.split("private static void Request(", 1)[1].split("Mission mission", 1)[0]
    for label in ("MissionDirecting", "MissionGenerating"):
        line = next(line for line in mission.splitlines() if "IllustratorUiText." + label in line)
        assert "IllustratorRuntime.Post(" in line, "Worker status must resolve on the main thread"
    worker = runtime.split("bool started = IllustratorRuntime.Start(async () =>", 1)[1].split("}, (result, error) =>", 1)[0]
    assert "IllustratorUiText" not in worker, "Background runtime must not resolve UI text"
    assert en["AF_ILL_UI_GALLERY_LEGACY_DEFAULT_TITLE"] == "Calradia history illustration"
    assert zh["AF_ILL_UI_GALLERY_LEGACY_DEFAULT_TITLE"] == "卡拉迪亚历史画卷"
    assert en["AF_ILL_UI_DEFAULT_HERO_NAME"] == "Hero" and zh["AF_ILL_UI_DEFAULT_HERO_NAME"] == "英雄"
    assert en["AF_ILL_UI_DEFAULT_PARTNER_NAME"] == "the other person" and zh["AF_ILL_UI_DEFAULT_PARTNER_NAME"] == "对方"
    default_title = helper.split("internal static string ProjectKnownDefaultTitle", 1)[1].split("internal static string ConversationSessionChanged", 1)[0]
    assert 'string.Equals(title, "卡拉迪亚历史画卷", StringComparison.Ordinal)' in default_title
    assert 'string.Equals(category, "encyclopedia", StringComparison.Ordinal)' in default_title
    assert 'string.Equals(hero, "英雄", StringComparison.Ordinal) ? DefaultHeroName : hero' in default_title
    assert 'string.Equals(category, "conversation", StringComparison.Ordinal)' in default_title
    assert 'string.Equals(partner, "对方", StringComparison.Ordinal) ? DefaultPartnerName : partner' in default_title
    assert "return title;" in default_title, "Unknown/custom titles must remain unchanged"
    card = (SOURCE / "UI/Overlays/IllustrationCardPopup.cs").read_text(encoding="utf-8-sig")
    assert 'hero.Name != null ? hero.Name.ToString() : IllustratorUiText.DefaultHeroName' in card
    assert ': IllustratorUiText.DefaultPartnerName);' in card
    assert 'string heroName = hero.Name?.ToString() ?? "英雄";' in card, "Prompt/source fallback must remain raw"
    assert ': "对方");' in card, "Conversation model/source fallback must remain raw"
    gallery = (SOURCE / "UI/Gallery/IllustratorGalleryPopupVM.cs").read_text(encoding="utf-8-sig")
    cache = (SOURCE / "Engine/DiskImageCacheManager.cs").read_text(encoding="utf-8-sig")
    assert "ProjectKnownDefaultTitle(item.Title, item.Category)" in card
    assert 'string.IsNullOrWhiteSpace(direction.Title) ? $"{heroName} 纪事肖像" : direction.Title' in card
    assert 'string.IsNullOrWhiteSpace(direction.Title) ? $"与 {partnerName} 的会晤纪事" : direction.Title' in card
    assert "SaveImage(key, result.ImageBytes, effectivePrompt," in card, "Image prompt/save input changed"
    assert "ProjectKnownDefaultTitle(item.Title, item.Category)" in gallery
    assert 'Title = "卡拉迪亚历史画卷"' in cache, "Legacy cache title must remain raw"
    assert "GenerationComplete(IllustratorUiText.ProjectKnownDefaultTitle(update.Saved.Title, update.Saved.Category))" in runtime

    mapped = json.loads((ROOT / "content/content-map.json").read_text(encoding="utf-8-sig"))["entries"]
    sources = {entry["target"]: ROOT / entry["source"] for entry in mapped}
    for path in (en_path, zh_path):
        assert any((ROOT / entry["source"]).resolve() == path.resolve() for entry in mapped), f"Missing content mapping: {path}"
    for file_name, resource in (("language_data.xml", "illustrator_strings.xml"), ("CNs/language_data.xml", "CNs/illustrator_strings-zh-CN.xml")):
        index = sources["ModuleData/Languages/" + file_name]
        registered = {item.attrib["xml_path"] for item in ET.parse(index).getroot().iter("LanguageFile")}
        assert resource in registered, f"Missing language registration: {resource}"
    print(f"PASS Illustrator notices/entry keys={len(calls)}; pair keys={len(en)}; static UI/thread/resource contracts")


if __name__ == "__main__":
    main()
