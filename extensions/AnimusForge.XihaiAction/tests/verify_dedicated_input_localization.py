"""Static resource/source contract; does not execute battle input or native localization."""
import importlib.util
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
RELATIVE = "extensions/AnimusForge.XihaiAction/src/Runtime/BattleSpeechDedicatedInputMissionBehavior.cs"
source = (ROOT / RELATIVE).read_text(encoding="utf-8-sig")
baseline = subprocess.check_output(["git", "show", "37467703ea5b8a6b77564e1e0035d711faefdd14:" + RELATIVE], cwd=ROOT).decode("utf-8-sig")
spec = importlib.util.spec_from_file_location("inventory", ROOT / "tools/localization/inventory_untranslated.py")
inventory = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inventory)

def method(text, name):
    code = inventory.tokenize(text)[0]
    start = code.index(name)
    opening = code.index("{", start)
    depth = 1
    for end in range(opening + 1, len(code)):
        depth += (code[end] == "{") - (code[end] == "}")
        if depth == 0:
            return text[start:end + 1]
    raise AssertionError(name)

for name in ("private void StartDedicatedNpcSpeechGeneration(", "private async Task GenerateAndQueueDedicatedNpcSpeechAsync(",
             "private static void NotifyDedicatedSpeechMessage("):
    assert method(source, name) == method(baseline, name), name

fallbacks = dict(re.findall(r'new TextObject\("\{=(SAX_BattleSpeechInput[^}]+)\}([^"\\]*)"\)', source))
assert len(fallbacks) == 12
for tree in ("extensions/AnimusForge.XihaiAction", "content/modules/AnimusForge.XihaiAction"):
    tables = []
    for filename in ("sceneactions_strings.xml", "CNs/sceneactions_strings-zh-CN.xml"):
        entries = list(ET.parse(ROOT / tree / "ModuleData/Languages" / filename).iter("string"))
        ids = [node.get("id") for node in entries]
        assert len(ids) == len(set(ids)), (tree, filename, "duplicate ID")
        tables.append({node.get("id"): node.get("text") for node in entries})
    en, zh = tables
    assert en.keys() == zh.keys(), tree
    for key, fallback in fallbacks.items():
        assert en[key] == fallback, (tree, key)
        assert zh[key]
        assert re.findall(r"\{[^}]+\}", en[key]) == re.findall(r"\{[^}]+\}", zh[key]), key

for match, _ in inventory.tokenize(source)[1]:
    value = inventory.decode(match[0])
    if re.search(r"[\u3400-\u9fff]", value):
        assert value == "我演讲：", value
assert 'string routedText = "我演讲：" + content;' in source
assert "Mission.CurrentTime);" in source
assert "submit,\n                        cancel)" in source.replace("\r\n", "\n")

mapping = json.loads((ROOT / "content/content-map.json").read_text(encoding="utf-8-sig"))
entries = mapping if isinstance(mapping, list) else mapping["entries"]
for filename in ("sceneactions_strings.xml", "CNs/sceneactions_strings-zh-CN.xml"):
    expected = "content/modules/AnimusForge.XihaiAction/ModuleData/Languages/" + filename
    assert any(entry.get("source") == expected for entry in entries), expected
    registration = ROOT / "content/foundation/AF.Foundation.Localization/ModuleData/Languages" / ("CNs/language_data.xml" if filename.startswith("CNs/") else "language_data.xml")
    assert filename in [node.get("xml_path") for node in ET.parse(registration).iter("LanguageFile")]

project = ET.parse(ROOT / "extensions/AnimusForge.XihaiAction/src/AnimusForge.XihaiAction.csproj")
assert any(node.get("Include") == "TaleWorlds.Localization" for node in project.iter("Reference"))
assert "EventEditorUiText" not in source and "WeeklyReportUiText" not in source
print("PASS 12 dedicated-input pairs, native resource registration, unchanged generation methods and internal route prefix; C# and game NOT-RUN")
