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
    assert len(calls) == len({key for key, _ in calls}), "Duplicate notification/entry definitions"
    required = {"AF_ILL_MENU_GALLERY", "AF_ILL_NOTIFY_GENERATION_COMPLETE", "AF_ILL_NOTIFY_GENERATION_FAILED", "AF_ILL_MISSION_DIRECTING", "AF_ILL_MISSION_GENERATING", "AF_ILL_MISSION_NO_DIALOGUE"}
    assert required <= {key for key, _ in calls}, "Missing required actual UI entry contracts"
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
    for diagnostic in ("No player scene is available for screenshot illustration, or image generation is disabled.", "Configure the image API first."):
        assert 'throw new InvalidOperationException("' + diagnostic + '")' in request, "Validation remains technical, independent of UI language"
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
    compatibility = json.loads((ROOT / 'content/modules/AnimusForge.Illustrator/ModuleData/IllustratorCompatibilityTexts.json').read_text(encoding='utf-8-sig'))
    for key, value in {'Residual_IllustratorUiText_3e7ebd8d664d': '卡拉迪亚历史画卷', 'Residual_IllustratorUiText_09e9b2088fa0': '英雄', 'Residual_IllustratorUiText_ddfdac569e37': '对方', 'Residual_DiskImageCacheManager_3e7ebd8d664d': '卡拉迪亚历史画卷'}.items():
        assert compatibility[key] == value, key + ': original title/name bytes'
    assert all('IllustratorText.Raw("' + key + '")' in default_title for key in ('Residual_IllustratorUiText_3e7ebd8d664d', 'Residual_IllustratorUiText_09e9b2088fa0', 'Residual_IllustratorUiText_ddfdac569e37'))
    default_title = re.sub(r'IllustratorText.Raw\("([^"\n]+)"\)', lambda match: json.dumps(compatibility[match[1]], ensure_ascii=False), default_title)
    assert 'string.Equals(title, "卡拉迪亚历史画卷", StringComparison.Ordinal)' in default_title
    assert 'string.Equals(category, "encyclopedia", StringComparison.Ordinal)' in default_title
    assert 'string.Equals(hero, "英雄", StringComparison.Ordinal) ? DefaultHeroName : hero' in default_title
    assert 'string.Equals(category, "conversation", StringComparison.Ordinal)' in default_title
    assert 'string.Equals(partner, "对方", StringComparison.Ordinal) ? DefaultPartnerName : partner' in default_title
    assert "return title;" in default_title, "Unknown/custom titles must remain unchanged"
    card = (SOURCE / "UI/Overlays/IllustrationCardPopup.cs").read_text(encoding="utf-8-sig")
    assert 'hero.Name != null ? hero.Name.ToString() : IllustratorUiText.DefaultHeroName' in card
    assert ': IllustratorUiText.DefaultPartnerName);' in card
    assert 'string heroName = hero.Name?.ToString() ?? IllustratorText.Get("Hero_319");' in card, "Prompt fallback must use the package"
    assert ': IllustratorText.Get("Conversation_31"));' in card, "Conversation model fallback must use the package"
    gallery = (SOURCE / "UI/Gallery/IllustratorGalleryPopupVM.cs").read_text(encoding="utf-8-sig")
    cache = (SOURCE / "Engine/DiskImageCacheManager.cs").read_text(encoding="utf-8-sig")
    assert "ProjectKnownDefaultTitle(item.Title, item.Category)" in card
    assert 'string.IsNullOrWhiteSpace(direction.Title) ? IllustratorText.Format("Card_PortraitCacheTitle", heroName) : direction.Title' in card
    assert 'string.IsNullOrWhiteSpace(direction.Title) ? IllustratorText.Format("Card_ConversationCacheTitle", cachePartnerName) : direction.Title' in card
    assert "SaveImage(key, result.ImageBytes, effectivePrompt," in card, "Image prompt/save input changed"
    assert 'string playerName = player?.Name != null ? player.Name.ToString() : IllustratorText.Get("Side_Player");' in card
    assert 'Speaker = context.InterlocutorHero?.Name?.ToString() ?? context.InterlocutorCharacter?.Name?.ToString() ?? IllustratorText.Get("Conversation_31")' in card
    assert 'string sceneStatus = IllustratorText.Get("Card_ConversationReferenceReuse");' in card
    assert 'IllustratorText.Get("Card_DefaultPartnerCacheName")' in card
    # Inspect the real asynchronous bodies, excluding only explicitly posted UI callbacks.
    def remove_posts(body):
        pattern = r'IllustratorRuntime\.Post\('
        while (match := re.search(pattern, body)) is not None:
            opening = match.end() - 1
            depth, end = 0, opening
            for end in range(opening, len(body)):
                depth += (body[end] == '(') - (body[end] == ')')
                if depth == 0: break
            body = body[:match.start()] + body[end + 1:]
        return body
    # Match braces on a lexical mask so braces inside prompt strings do not end a lambda.
    import sys
    sys.path.insert(0, str(ROOT / 'tools/localization'))
    from inventory_untranslated import LEX
    mask = LEX.sub(lambda match: ' ' * len(match.group()), card)
    workers = []
    for match in re.finditer(r'\.RunGeneration\([^;]*?async token\s*=>\s*\{', mask):
        opening = match.end() - 1
        depth = 0
        for end in range(opening, len(mask)):
            depth += (mask[end] == '{') - (mask[end] == '}')
            if depth == 0: break
        workers.append(card[opening:end + 1])
    assert len(workers) == 2, "Both real portrait/conversation generation workers traced"
    for body in workers:
        assert 'IllustratorUiText.' not in remove_posts(body), "Worker must not resolve interface text"
    prompts_cn = json.loads((ROOT / 'content/modules/AnimusForge.Illustrator/ModuleData/IllustratorPromptTexts.json').read_text('utf-8-sig'))
    prompts_en = json.loads((ROOT / 'content/modules/AnimusForge.Illustrator/ModuleData/IllustratorPromptTexts.en.json').read_text('utf-8-sig'))
    assert prompts_cn.keys() == prompts_en.keys()
    for key in prompts_cn:
        assert sorted(re.findall(r'\{([0-9]+)\}', prompts_cn[key])) == sorted(re.findall(r'\{([0-9]+)\}', prompts_en[key])), key
    # Source-driven semantic clauses, not a no-Han or updated snapshot claim.
    constraints = {
        'Scene_PanoramaNote': ['360', '180', 'six 90', 'forward, backward, left, right, up, and down', 'not six rooms', 'left and right edges meet', 'top is upward', 'bottom downward', 'poles', 'hairstyles', 'clothing', 'poses', 'identity reference', 'normal perspective'],
        'Scene_Nearby30mSummary': ['30-meter', 'six 90', '360 degrees horizontally', '180 degrees vertically', 'does not mean all geometry is covered', 'only the environment'],
        'Panorama_MissingGeo': ['actual resource files', 'placements', 'live damage', 'scripted or dynamic variants', 'not walls or ground'],
        'Panorama_Lighting': ['three', 'shadowless', 'fixed exposure', 'sunlight disabled', 'shadow caches', 'particles', 'physics components', 'does not represent actual lighting'],
        'Panorama_TerrainOmitted': ['heightfield', 'blended terrain materials', 'water surfaces', 'floating geometry', 'flat ground', 'interior'],
        'Scene_ScreenshotRefNote': ['most recent two dialogue turns', 'standing poses', 'gestures', 'facing', 'gaze', 'gallery artwork', 'interface content'],
        'Scene_NoLiveScreenshot': ['fill lighting', 'absence of people', 'actual site'],
        'Scene_PlayerEyeOffset': ['zero additional offset'],
        'Scene_CameraFallbackOffset': ['fallback has been recorded'],
        'Scene_NearbyObjectsBudgetExceeded': ['no incomplete copy'],
    }
    for key, terms in constraints.items():
        assert prompts_cn[key] and all(term.lower() in prompts_en[key].lower() for term in terms), key
    reader = (SOURCE / 'Core/IllustratorText.cs').read_text('utf-8-sig')
    assert 'IllustratorPromptTexts.json' in reader and 'Lazy<Dictionary<string, string>>' in reader
    assert 'TextObject' not in reader and 'CurrentLanguage' not in reader and 'IllustratorUiText' not in reader

    assert "ProjectKnownDefaultTitle(item.Title, item.Category)" in gallery
    assert 'Title = IllustratorText.Raw("Residual_DiskImageCacheManager_3e7ebd8d664d")' in cache, "Legacy cache title uses the stable raw reader"
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
