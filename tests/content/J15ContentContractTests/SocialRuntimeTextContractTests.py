"""Static Social source/resource boundary checks; does not compile or execute C#."""
from pathlib import Path
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, HAN, decode

BASE = ROOT / 'content/modules/AF.Module.Social/ModuleData'
raw = json.loads((BASE / 'SocialSourceTexts.json').read_text(encoding='utf-8'))
cn = json.loads((BASE / 'SocialPromptTexts.json').read_text(encoding='utf-8'))
en = json.loads((BASE / 'SocialPromptTexts.en.json').read_text(encoding='utf-8'))
assert len(raw) == 444
assert hashlib.sha256(json.dumps(raw, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest() == '6a5699b2dafa9c3c7a04945dc5addcab060904126ae1817af24d6c2df0e31f21'
assert set(cn) == set(en) and len(cn) == 88
assert all(not HAN.search(v) for v in en.values())
placeholder = re.compile(r'\{\d+(?:,[^{}:]+)?(?::[^{}]+)?\}')
for key in cn:
    assert placeholder.findall(cn[key]) == placeholder.findall(en[key]), key

module = ROOT / 'src/modules/AF.Module.Social'
for path in module.rglob('*.cs'):
    source = path.read_text(encoding='utf-8-sig')
    for match in LEX.finditer(source):
        if match.lastgroup in ('string', 'char'):
            token = match[0] if match.lastgroup == 'string' else '"' + match[0][1:-1] + '"'
            assert not HAN.search(decode(token)), (path, match[0])
    for key in re.findall(r'SocialSourceTexts.Get\("([^"]+)"\)', source):
        assert key in raw, (path, key)
    for key in re.findall(r'SocialPromptTexts.Get\("([^"]+)"\)', source):
        assert key in cn, (path, key)

ui = []
for path in [BASE / 'Languages/afsocial_ui_strings.xml', BASE / 'Languages/CNs/afsocial_ui_strings-zh-CN.xml']:
    entries = list(ET.parse(path).iter('string'))
    values = {item.get('id'): item.get('text') for item in entries}
    assert len(values) == len(entries)
    ui.append(values)
assert set(ui[0]) == set(ui[1])
assert len([key for key in ui[0] if key.startswith('AF_SOCIAL_UI_ProactiveNeed_')]) == 31
assert '{NUMBER}' in ui[0]['AF_SOCIAL_UI_Century'] and '{NUMBER}' in ui[1]['AF_SOCIAL_UI_Century']

loader = (module / 'Host/SocialPromptTexts.cs').read_text(encoding='utf-8')
assert 'Lazy<Dictionary<string, string>>' in loader
assert 'GetModuleDataFilePath("SocialPromptTexts.json")' in loader
assert 'TextObject' not in loader and 'Language' not in loader
romance = (module / 'Host/RomanceSystemBehavior.cs').read_text(encoding='utf-8')
assert 'BuildBridePriceSummary(MarriageRecord record, bool forPrompt = false)' in romance
assert romance.count('forPrompt: true') == 3
assert 'BuildBridePriceSummary(GetMarriageRecord(playerClanHero, targetHero))' in romance
assert 'case string _ when text == SocialSourceTexts.Get("S306"):' in (module / 'Patience/PatienceRules.cs').read_text(encoding='utf-8')
print('PASS Social: no Chinese runtime literals, 444 invariant entries, 88 packaged prompt pairs, 32 UI keys, cached package selection and fact-boundary checks; C# NOT RUN')
