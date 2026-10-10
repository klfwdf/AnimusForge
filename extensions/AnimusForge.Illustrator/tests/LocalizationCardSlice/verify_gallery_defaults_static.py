"""Editable gallery defaults and isolated probe note; C#/game NOT-RUN."""
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET
from verify_failure_static import restore_failure_catalog

ROOT = Path(__file__).resolve().parents[4]
PREFIX = "extensions/AnimusForge.Illustrator/src/"
BASELINE = "31e691f0f"


def old(relative):
    return subprocess.check_output(["git", "show", BASELINE + ":" + PREFIX + relative], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")


def restore_converter(source):
    changes = [
        ('var text = new StringBuilder("这是一幅名为" + ItemName(item) + "的画作。\\n");', "var text = new StringBuilder(IllustratorRemainingUiText.GalleryIntroductionName(DisplayItemName(item))).Append('\\n');"),
        ('text.Append("画面主题：").Append(theme)', 'text.Append(IllustratorRemainingUiText.GalleryIntroductionTheme(theme))'),
        ('text.Append("画面描绘：").Append(action)', 'text.Append(IllustratorRemainingUiText.GalleryIntroductionAction(action))'),
        ('text.Append("画面内容尚未注明。\\n");', "text.Append(IllustratorRemainingUiText.GalleryIntroductionUnannotated).Append('\\n');"),
        ('text.Append("画中的内容属于艺术描绘，不代表其中的事件已经真实发生。");', 'text.Append(IllustratorRemainingUiText.GalleryIntroductionFiction);'),
        ('private static string DisplayItemName(', 'internal static string DisplayItemName('),
    ]
    for before, after in changes:
        assert source.count(after) == 1, after
        source = source.replace(after, before, 1)
    return source


def restore_probe(source):
    original = re.search(r'const string probeNote = [^\n]+;', old("UI/Overlays/IllustrationCardPopup.cs"))[0]
    target = 'string probeNote = IllustratorUiText.SceneProbeNote;'
    assert source.count(target) == 1
    return source.replace(target, original, 1)


def restore_probe_catalog(source):
    source = restore_failure_catalog(source)
    lines = [line for line in source.splitlines(keepends=True) if '"AF_ILL_UI_SCENE_PROBE_NOTE"' in line]
    assert len(lines) == 1
    return source.replace(lines[0], "", 1)


def main():
    converter = (ROOT / PREFIX / "UI/Gallery/GalleryRpItemConverter.cs").read_text(encoding="utf-8-sig")
    assert restore_converter(converter) == old("UI/Gallery/GalleryRpItemConverter.cs"), "Identity/roster/description storage/original ItemName changed"
    intro = converter.split('internal static string BuildIntroduction(', 1)[1].split('internal static bool TryConvert(', 1)[0]
    assert not re.search(r'[\u4e00-\u9fff]', intro), "Fixed introduction still Chinese"
    assert 'Clean(item?.Theme, 600)' in intro and 'Clean(item?.ActionSummary, 900)' in intro and '.Prompt' not in intro
    vm = (ROOT / PREFIX / "UI/Gallery/IllustratorGalleryPopupVM.cs").read_text(encoding="utf-8-sig")
    assert vm.replace('GalleryRpItemConverter.DisplayItemName(image)', 'GalleryRpItemConverter.ItemName(image)') == old("UI/Gallery/IllustratorGalleryPopupVM.cs"), "Conversion lifecycle changed"
    popup = (ROOT / PREFIX / "UI/Overlays/IllustrationCardPopup.cs").read_text(encoding="utf-8-sig")
    assert restore_probe(popup) == old("UI/Overlays/IllustrationCardPopup.cs"), "Capture/cache/diagnostics changed"
    callback = popup.split('}, imageBytes =>', 1)[1].split('}, SetSceneProbeFailure)', 1)[0]
    assert 'IllustratorUiText.SceneProbeNote' in callback and 'PublishImage(null, imageBytes, probeNote)' in callback
    regular = (ROOT / PREFIX / "UI/IllustratorUiText.cs").read_text(encoding="utf-8-sig")
    assert restore_probe_catalog(regular) == old("UI/IllustratorUiText.cs")
    remaining = (ROOT / PREFIX / "UI/IllustratorRemainingUiText.cs").read_text(encoding="utf-8-sig")
    restored = remaining
    lines = [line for line in remaining.splitlines(keepends=True) if '"AF_ILL_UI_GALLERY_INTRODUCTION_' in line]
    assert len(lines) == 5
    for line in lines:
        restored = restored.replace(line, "", 1)
    restored = restored.replace('    // Fixed display and editable input defaults. Existing asset titles and metadata stay in their producer path.', '    // Narrow display catalog for the remaining gallery/conversion/photo sinks. Asset titles and descriptions\n    // are intentionally kept in their producer path; these methods only create player-facing messages.')
    assert restored == old("UI/IllustratorRemainingUiText.cs"), "Existing catalog changed"
    lang = ROOT / "content/modules/AnimusForge.Illustrator/ModuleData/Languages"
    en = {n.get("id"): n.get("text") for n in ET.parse(lang / "illustrator_strings.xml").iter("string")}
    zh = {n.get("id"): n.get("text") for n in ET.parse(lang / "CNs/illustrator_strings-zh-CN.xml").iter("string")}
    calls = re.findall(r'(?:Resolve|Format)\("(AF_ILL_UI_(?:GALLERY_INTRODUCTION_[^"]+|SCENE_PROBE_NOTE))", "([^"]*)"', regular + remaining)
    assert len(calls) == 6
    for key, fallback in calls:
        assert en[key] == fallback and zh[key].strip(), key
        assert not re.search(r'[\u4e00-\u9fff]', fallback), key
        assert sorted(re.findall(r'\{[A-Z_]+\}', en[key])) == sorted(re.findall(r'\{[A-Z_]+\}', zh[key])), key
    original_note = re.search(r'const string probeNote = "([^"]*)";', old("UI/Overlays/IllustrationCardPopup.cs"))[1]
    assert zh['AF_ILL_UI_SCENE_PROBE_NOTE'] == original_note
    print("PASS gallery defaults/probe static: 6 keys, source identity/roster/metadata/capture exact inverse; raw ItemName still pending; C#/game NOT-RUN")


if __name__ == "__main__":
    main()
