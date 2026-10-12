"""Known error display boundary; source/resource evidence, not C# or game execution."""
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[4]
PREFIX = "extensions/AnimusForge.Illustrator/src/"
BASELINE = "aa102ec17"


def baseline(relative):
    return subprocess.check_output(["git", "show", BASELINE + ":" + PREFIX + relative], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")


def restore_failure_catalog(source):
    for name, value, count in (("variable", "value", 1), ("firstName", "firstValue", 2), ("secondName", "secondValue", 2), ("thirdName", "thirdValue", 1)):
        before = f'text.SetTextVariable({name}, {value}?.ToString() ?? string.Empty);'
        after = f'text.SetTextVariable({name}, DisplayValue({name}, {value}));'
        assert source.count(after) == count, (after, source.count(after))
        source = source.replace(after, before)
    added = '''
        private static string DisplayValue(string name, object value)
        {
            string raw = value?.ToString() ?? string.Empty;
            return name == "ERROR" ? IllustrationFailureUiText.Project(raw) : raw;
        }
'''
    assert source.count(added) == 1
    return source.replace(added, "", 1)


def main():
    helper = (ROOT / PREFIX / "UI/IllustrationFailureUiText.cs").read_text(encoding="utf-8-sig")
    catalog = (ROOT / PREFIX / "UI/IllustratorUiText.cs").read_text(encoding="utf-8-sig")
    assert restore_failure_catalog(catalog) == baseline("UI/IllustratorUiText.cs"), "Unexpected catalog behavior change"
    producers = ("UI/Overlays/IllustrationCardPopup.cs", "Core/UniversalOpenAiImageClient.cs", "Core/IllustratorRuntime.cs", "Core/MissionScreenshotIllustration.cs", "Engine/ScreenCaptureHelper.cs", "Core/BulletinIllustrationPreloader.cs")
    producer_text = ""
    for relative in producers:
        current = (ROOT / PREFIX / relative).read_text(encoding="utf-8-sig")
        assert current == baseline(relative), "Error/diagnostic/Prompt/cache/queue producer changed: " + relative
        producer_text += current
    lang = ROOT / "content/modules/AnimusForge.Illustrator/ModuleData/Languages"
    pairs = []
    for path in (lang / "illustrator_strings.xml", lang / "CNs/illustrator_strings-zh-CN.xml"):
        rows = list(ET.parse(path).iter("string"))
        data = {x.get("id"): x.get("text") for x in rows}
        assert len(rows) == len(data)
        pairs.append(data)
    en, zh = pairs
    assert en.keys() == zh.keys()
    calls = re.findall(r'IllustratorUiText\.(?:Resolve|Format)\("(AF_ILL_ERROR_[A-Z_]+)", "([^"]+)"', helper)
    assert len(calls) == 17
    for key, fallback in calls:
        assert en[key] == fallback and zh[key]
        assert not re.search(r'[\u3400-\u4dbf\u4e00-\u9fff]', fallback)
        assert sorted(re.findall(r'\{[A-Z_]+\}', fallback)) == sorted(re.findall(r'\{[A-Z_]+\}', zh[key]))
    cases = re.findall(r'case "([^"]+)":', helper)
    assert len(cases) == 19
    for raw in cases:
        assert raw in producer_text or raw == zh["AF_ILL_UI_CARD_NOT_SAVED_OR_DISPLAYED"], "Mapping has no known producer: " + raw
    assert 'return raw;' in helper and 'unwrapCommunication: false' in helper
    assert '"DETAIL", Project(raw.Substring(communicationPrefix.Length)' in helper, "Provider detail lost"
    assert 'if (IsBudget(seconds))' in helper and "value[i] < '0' || value[i] > '9'" in helper
    assert 'name == "ERROR"' in catalog, "Non-error variables must retain raw contents"
    assert '"ERROR", error' in catalog and '"ERROR", error);' in catalog
    print("PASS Illustrator failure display: 17 new pairs + 2 reused; 19 fixed producers; known timeout/communication wrappers; unknown raw detail retained; producer bytes unchanged; C#/game NOT-RUN")


if __name__ == "__main__":
    main()
