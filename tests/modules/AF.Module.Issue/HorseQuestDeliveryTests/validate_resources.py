import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parents[4]
data = root / "content/modules/AF.Module.Issue/ModuleData"
keys = {"AF_ISSUE_HorseDelivered", "AF_ISSUE_HorsePaymentObserved", "AF_ISSUE_HorseObservation", "AF_ISSUE_HorseEvidence"}
packs = []
for suffix, xml in [("", "Languages/CNs/afissue_prompt_strings-zh-CN.xml"), (".en", "Languages/afissue_prompt_strings.xml")]:
    pack = json.loads((data / ("IssuePromptTexts" + suffix + ".json")).read_text(encoding="utf-8-sig"))
    strings = {node.attrib["id"]: node.attrib["text"] for node in ET.parse(data / xml).findall(".//string")}
    for key in keys:
        assert pack[key] == strings[key], key
    packs.append(pack)
for key in keys:
    assert re.findall(r"\{[^{}]+\}", packs[0][key]) == re.findall(r"\{[^{}]+\}", packs[1][key]), key
    assert not re.search("[\u3400-\u9fff]", packs[1][key]), key
mapping = (root / "content/content-map.json").read_text(encoding="utf-8-sig")
assert "AF.Module.Issue/ModuleData/IssuePromptTexts.json" in mapping
for language, path in [("", "afissue_prompt_strings.xml"), ("CNs/", "afissue_prompt_strings-zh-CN.xml")]:
    registry = root / ("content/foundation/AF.Foundation.Localization/ModuleData/Languages/" + language + "language_data.xml")
    assert path in registry.read_text(encoding="utf-8-sig")
print("PASS horse delivery CN/EN resource parity, placeholders, package mapping and UI registration")
