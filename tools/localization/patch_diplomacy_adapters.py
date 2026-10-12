import json, pathlib
ROOT = pathlib.Path(".").resolve()
p_zh = ROOT / "content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyMiscPromptTexts.json"
p_en = ROOT / "content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyMiscPromptTexts.en.json"
zh = json.loads(p_zh.read_text(encoding="utf-8"))
en = json.loads(p_en.read_text(encoding="utf-8"))
data = {}
# test line with "quote"
# appended test
# line1
# line2
