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
    additions = set(json.loads(Path(__file__).with_name('DiplomacyPackagedPromptAdditions.json').read_text(encoding='utf-8')))
    original_keys = set(expected) | {'AF_DIP_Contract_' + str(i).zfill(3) for i in range(118)} | {'AF_DIP_Fact_' + str(i).zfill(3) for i in range(50)}
    assert {k: data[k] for k in expected} == expected, suffix + ': original package content values preserved'
    assert set(data) == original_keys | additions and not original_keys.intersection(additions), suffix + ': only explicitly reviewed additions allowed'
    assert {'HistoryProjection_PolicyIssuer', 'HistoryProjection_PolicyScope', 'FallbackDocumentTitleTargetSeparator'} <= additions
    paired = json.loads((ROOT / base / ('DiplomacyPromptTexts' + ('' if suffix else '.en') + '.json')).read_text(encoding='utf-8'))
    assert data.keys() == paired.keys(), 'CN/EN additions must have identical identities'
    import re
    for key in additions:
        assert isinstance(data[key], str) and isinstance(paired[key], str), key
        assert sorted(re.findall(r'\{[A-Z_0-9]+\}', data[key])) == sorted(re.findall(r'\{[A-Z_0-9]+\}', paired[key])), key + ': placeholders'
        if suffix: assert not re.search(r'[\u3400-\u9fff]', data[key]), key + ': English package added text contains Han'

    protocol = r'\{[A-Za-z_][A-Za-z0-9_]*\}|\[(?:ACTION|RELAY|AD|ADP|AFEF):[^\]\r\n]*\]'
    for key in data:
        assert set(re.findall(protocol, data[key])) == set(re.findall(protocol, paired[key])), key + ': complete packaged action protocol identity'
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
