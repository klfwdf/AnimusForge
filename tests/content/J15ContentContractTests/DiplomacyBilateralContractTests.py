"""Affected CN/EN direct-action contracts and native-mode isolation; no live acceptance."""
from pathlib import Path
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / 'content/modules/AF.Module.Diplomacy/ModuleData'

def unique(pairs):
    data = {}
    for key, value in pairs:
        assert key not in data, 'duplicate key: ' + key
        data[key] = value
    return data

def diplomacy(value):
    if isinstance(value, dict):
        if value.get('Id') == 'diplomacy':
            return value
        values = value.values()
    elif isinstance(value, list):
        values = value
    else:
        return None
    return next((found for item in values if (found := diplomacy(item))), None)

packs = [json.loads((BASE / f'DiplomacyPromptTexts{s}.json').read_text(encoding='utf-8'), object_pairs_hook=unique) for s in ('', '.en')]
assert packs[0].keys() == packs[1].keys()
keys = {key for key in packs[0] if key.startswith('Bilateral_')}
assert keys == {'Bilateral_Postprocess', 'Bilateral_ReviewExclusion', 'Bilateral_Announcement', 'Bilateral_Result',
                'Bilateral_Result_rejected', 'Bilateral_Result_partial', 'Bilateral_Result_unknown'}
affected = keys | {'OralArrangementHeader', 'AF_DIP_PeaceMainIntegrity', 'CountryAdmissionOral'}
for key in affected:
    cn, en = (pack[key] for pack in packs)
    assert not re.search(r'[\u3400-\u9fff]', en), key
    assert set(re.findall(r'\{[A-Z_]+\}', cn)) == set(re.findall(r'\{[A-Z_]+\}', en)), key
    if key.startswith('Bilateral_'):
        assert set(re.findall(r'\[ACTION:[^\]]+\]', cn)) == set(re.findall(r'\[ACTION:[^\]]+\]', en)), key

actions = {'DECLARE_WAR', 'MAKE_PEACE', 'FORM_ALLIANCE', 'BREAK_ALLIANCE', 'MAKE_TRADE', 'CANCEL_TRADE'}
for suffix in ('', '.en'):
    path = f'content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts{suffix}.json'
    current = diplomacy(json.loads((ROOT / path).read_text(encoding='utf-8'), object_pairs_hook=unique))
    baseline = diplomacy(json.loads(subprocess.check_output(['git', 'show', 'dc8bc6143:' + path], cwd=ROOT).decode('utf-8-sig')))
    # A new AI route must not quietly rewrite the disabled-mode runtime contract.
    assert current['RuntimeInstructionTemplates']['native_action_postprocess'] == baseline['RuntimeInstructionTemplates']['native_action_postprocess']
    assert '[ACTION:' not in current['Instruction']
    by_action = {row['Tag'].split(':')[2]: row for row in current['PostprocessRules']}
    assert 'REVIEW_REMAINING]' in by_action
    for action in actions:
        description = by_action[action]['Description']
        assert 'AI disabled' in description if suffix else 'AI关闭时' in description
        native = description.split('AI disabled, retain these native rules: ' if suffix else 'AI关闭时保留以下原版规则：', 1)[1]
        assert not re.search(r'this turn|本轮', native), action + ': new assent rule must not leak to disabled mode'
        assert action in packs[bool(suffix)]['Bilateral_Postprocess']

mapping = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
assert sum(row['source'].endswith('/DiplomacyPromptTexts.json') and row['target'] == 'ModuleData/DiplomacyPromptTexts.json' for row in mapping) == 1
loader = (ROOT / 'src/modules/AF.Module.Diplomacy/Presentation/DiplomacyPromptTexts.cs').read_text(encoding='utf-8-sig')
assert 'GetModuleDataFilePath("DiplomacyPromptTexts.json")' in loader
assert 'Lazy<Dictionary<string, string>>' in loader
print('PASS bilingual bilateral contracts: 7 paired keys, affected placeholders/protocols, real packaged reader, 6 native-mode descriptions isolated')
