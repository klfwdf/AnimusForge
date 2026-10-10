"""Package-selected prompt corpus: exact migration, no UI-language dependency."""
from pathlib import Path
import json
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
base = 'content/modules/AF.Module.Diplomacy/ModuleData/'
for suffix, xml_path in (('', 'Languages/CNs/afdip_ui_strings-zh-CN.xml'), ('.en', 'Languages/afdip_ui_strings.xml')):
    data = json.loads((ROOT / base / ('DiplomacyPromptTexts' + suffix + '.json')).read_text(encoding='utf-8'))
    old_xml = subprocess.check_output(['git', 'show', 'a7de67231:' + base + xml_path], cwd=ROOT)
    expected = {n.get('id'): n.get('text').replace('[[AF_JSON_OPEN]]', '{').replace('[[AF_JSON_CLOSE]]', '}')
                for n in ET.fromstring(old_xml).iter('string') if n.get('id').startswith('AF_DIP_Prompt_')}
    assert {k: data[k] for k in expected} == expected and set(data) == set(expected) | {'AF_DIP_Contract_' + str(i).zfill(3) for i in range(118)}, suffix + ': exact package content migration'
    assert not any(n.get('id').startswith('AF_DIP_Prompt_') for n in ET.parse(ROOT / base / xml_path).iter('string'))
    schema = data['AF_DIP_Prompt_Composer_015']
    assert json.loads(schema[schema.index('{'):])['selected_kingdom_ids'] == ['ID']

entries = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
assert sum(e['source'] == base + 'DiplomacyPromptTexts.json' and e['target'] == 'ModuleData/DiplomacyPromptTexts.json' for e in entries) == 1
assert not any(e['source'] == base + 'DiplomacyPromptTexts.en.json' for e in entries), 'parallel EN must replace the same target, not leak a second file'
loader = (ROOT / 'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyPromptTexts.cs').read_text(encoding='utf-8-sig')
assert 'Lazy<Dictionary<string, string>>' in loader and 'GetModuleDataFilePath("DiplomacyPromptTexts.json")' in loader
assert all(s not in loader for s in ('TextObject', 'ChangeLanguage', 'CurrentLanguage', 'DiplomacyUiLocalization', 'catch'))
adapter = (ROOT / 'src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.PromptContext.cs').read_text(encoding='utf-8-sig')
assert '=> DiplomacyPromptTexts.Get(id, englishFallback);' in adapter
common = (ROOT / 'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.DiplomacyPromptLocalization.cs').read_text(encoding='utf-8-sig')
assert common.count('DiplomacyPromptTexts.Get(') == 4 and 'DiplomacyUiLocalization' not in common
print('PASS packaged diplomacy prompts: 216 migrated plus 118 fixed-contract CN/EN pairs, stable JSON schema, single mapped target and cached package-only readers; C# NOT RUN')
