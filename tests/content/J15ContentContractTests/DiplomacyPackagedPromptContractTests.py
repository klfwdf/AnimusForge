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
    retired = {'AF_DIP_Contract_006', 'AF_DIP_Contract_007', 'AF_DIP_Contract_088'}
    original_keys = set(expected) | ({'AF_DIP_Contract_' + str(i).zfill(3) for i in range(118)} - retired) | {'AF_DIP_Fact_' + str(i).zfill(3) for i in range(50)}
    assert not retired.intersection(data), 'retired style constraints must not remain active'
    defaults_name = 'McmPromptDefaults.en.json' if suffix else 'McmPromptDefaults.zh-CN.json'
    defaults = json.loads((ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData' / defaults_name).read_text(encoding='utf-8'))
    expected['AF_DIP_Prompt_CommonCore'] = defaults['DefaultWorldDiplomacyContractCore']
    expected['AF_DIP_Prompt_PreferenceGuard'] = defaults['DefaultWorldDiplomacyPreferenceGuard']
    if suffix:
        expected['AF_DIP_Prompt_Composer_017'] = 'Declarations in the long-term archive are published diplomatic documents. Respond afresh to the current situation. Meaningful negotiations are allowed, but do not repeat historical statements or reiterate positions without substance. Only mechanical diplomatic acts count as round action progress; negotiation statements do not extend the round indefinitely.'
        expected['AF_DIP_Prompt_Composer_168'] = 'Express the position according to this ruler’s character, situation and actual priorities, using the institutional facts below and the shared writing guidance.'
    else:
        expected['AF_DIP_Prompt_Composer_017'] = '长期档案中的宣言是已公开发布的外交文书。根据当前局势作出新的回应；可以进行有意义的谈判往来，但不得重述历史发言或无条件重复立场。机械外交行为才计入回合行动进展，谈判发言不会无限延长回合。'
        expected['AF_DIP_Prompt_Composer_168'] = '依据这位统治者的性格、处境与真实取舍表达立场，并结合下列国家制度事实，使用共用文风要求。'
    assert {k: data[k] for k in expected} == expected, suffix + ': package content preserved except exact reviewed style and shared MCM contract values'
    checkpoint = json.loads(subprocess.check_output(['git', 'show', '76fc6ed40:' + base + 'DiplomacyPromptTexts' + suffix + '.json'], cwd=ROOT).decode('utf-8-sig'))
    assert set(data) == set(checkpoint) - retired, suffix + ': exact checkpoint identities except retired style constraints'
    assert additions <= data.keys() and not original_keys.intersection(additions)
    checkpoint_additions = set(checkpoint) - (original_keys | additions | retired)
    for key in checkpoint_additions:
        old_value = checkpoint[key]
        if key == 'OralGeneration_System':
            before, after = ('只写正式宣言', '按共用文风只起草宣言') if not suffix else ('Write only the formal declaration', 'Draft only the declaration using the shared writing guidance')
            assert before in old_value
            old_value = old_value.replace(before, after, 1)
        elif key == 'OralGeneration_UserHeader':
            old_value = ('player_diplomacy=true\n依据以下已确认约定起草完整的公开外交宣言。\n' if not suffix
                         else 'player_diplomacy=true\nDraft a complete public diplomatic declaration from the confirmed agreement below.\n')
        assert data[key] == old_value, key + ': preserve checkpoint player and oral pipeline content'
    additions |= checkpoint_additions
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
    if suffix:
        oral = (ROOT / 'src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.OralGeneration.cs').read_text(encoding='utf-8-sig')
        for key in ('OralGeneration_System', 'OralGeneration_UserHeader'):
            matches = re.findall(r'DiplomacyPromptTexts.Get\("' + key + r'", ("(?:\\.|[^"\\])*")\)', oral)
            assert len(matches) == 1 and json.loads(matches[0]) == data[key], key + ': actual English fallback matches package resource'

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
print('PASS packaged diplomacy prompts: 216 migrated plus 115 active fixed-contract CN/EN pairs, stable JSON schema, single mapped target and cached package-only readers; C# NOT RUN')
