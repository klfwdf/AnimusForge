"""Static boundary evidence; this does not execute C# or test a live editor."""
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PATH = "src/AF.GameAdapter.Bannerlord/UI/Editors/MemoryEditorProjection.cs"
source = (ROOT / PATH).read_text(encoding="utf-8-sig")
baseline = subprocess.check_output(["git", "show", "c8b6c652e:" + PATH], cwd=ROOT).decode("utf-8")
en_file = ROOT / "content/modules/AF.Module.Memory/ModuleData/Languages/afmemory_ui_strings.xml"
zh_file = ROOT / "content/modules/AF.Module.Memory/ModuleData/Languages/CNs/afmemory_ui_strings-zh-CN.xml"
english = {node.get("id"): node.get("text") for node in ET.parse(en_file).iter("string")}
chinese = {node.get("id"): node.get("text") for node in ET.parse(zh_file).iter("string")}
new_keys = set()
for key, literal in re.findall(r'MemoryEditorUiText\.[TF]\("(AF_MEMORY_UI_[A-Za-z0-9_]+)",\s*"((?:\\.|[^"\\])*)"', source):
    new_keys.add(key)
    assert key in english and key in chinese, key
    assert english[key] == json.loads('"' + literal + '"'), key
    assert sorted(re.findall(r"\{([A-Z][A-Z0-9_]*)\}", english[key])) == sorted(re.findall(r"\{([A-Z][A-Z0-9_]*)\}", chinese[key])), key
assert len(new_keys) == 20, new_keys
assert source.count("string speaker = SpeakerForDisplay(line);") == 2
raw = json.loads((ROOT / 'content/modules/AF.Module.Memory/ModuleData/MemorySourceTexts.json').read_text(encoding='utf-8-sig'))
assert raw['MEM_Raw_007'] == '手动', 'legacy speaker identity remains raw'
assert 'string.Equals(speaker, MemoryTextResources.Raw("MEM_Raw_007"), StringComparison.OrdinalIgnoreCase)' in source
assert source.count("HourRangeForDisplay(block.StartHour, block.EndHour)") == 2
assert source.count("forDisplay: false") == 2
assert "forDisplay: true" in source and "port.BuildWeeklyMemoryMaterialTagLabel" in source
assert "WeeklyMaterialTagForDisplay" in source
assert not re.search(r"(?:line|draft|block|trigger)\.[A-Za-z]+\s*=(?!=)", source)
# Outside the explicitly reviewed display/search methods, preserve original source byte-for-byte.
def body(text, name):
    match = re.search(r"\binternal static [^\n]+ " + name + r"\(", text)
    start = text.index("\n\t{", match.start())
    end = text.index("\n\t}", start) + len("\n\t}")
    return text[start:end]
for name in ("GetDevCompressedMemoryBlockId", "FindDevCompressedMemoryBlock", "GetDefaultDevDailyMemoryLineScene",
             "BuildDevDailyMemoryLineDescription", "NormalizeDevCompressedMemoryMultilineInput", "ParseDevCompressedMemoryLineList"):
    assert body(source, name) == body(baseline, name), name
controller = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Editors/MemoryEditorController.cs").read_text(encoding="utf-8-sig")
assert 'target.Speaker = MemoryTextResources.Raw("MEM_Raw_007");' in controller and 'Speaker = isAfef ? "AFEF" : MemoryTextResources.Raw("MEM_Raw_007")' in controller
assert 'target.Speaker = "AFEF";' in controller
assert "WeeklyMaterialTagForDisplay" not in controller
print("PASS Memory projection: 20 keys, fallback/variable parity, display-only speaker/hour/tag conversion, raw search and mutation boundaries retained; C# NOT-RUN")
