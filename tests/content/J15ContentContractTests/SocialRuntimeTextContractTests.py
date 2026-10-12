"""Static Social source/resource boundary checks; does not compile or execute C#."""
from pathlib import Path
import hashlib
import json
import re
import sys
import subprocess
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
# The historical 88 values remain an immutable subset in both packages.
baseline_hashes = {'cn': '368c6a2219a1633b499f30bbdb417e81c60ca1f6b96a8ec244df6b0d703e8040', 'en': '01698acb1d14f39bdebed639f72c19aac64d1b4f69aa00168a52574afb46dd3a'}
for language, suffix, current in [('cn', '', cn), ('en', '.en', en)]:
    text = subprocess.check_output(['git', 'show', 'a911d4d0de8964a393b6bdc6eaa83705b5218b02:content/modules/AF.Module.Social/ModuleData/SocialPromptTexts' + suffix + '.json'], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    assert hashlib.sha256(text.encode()).hexdigest() == baseline_hashes[language]
    original = json.loads(text)
    assert len(original) == 88 and all(current.get(key) == value for key, value in original.items()), 'old prompt values lost/changed'
new_need_keys = {'AF_SOCIAL_UI_ProactiveNeed_' + str(index).zfill(2) for index in range(1, 32)}
assert set(cn) == set(en) == set(original) | new_need_keys
consumer_path = 'src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs'
consumer = (ROOT / consumer_path).read_text(encoding='utf-8-sig')
before = subprocess.check_output(['git', 'show', 'a911d4d0de8964a393b6bdc6eaa83705b5218b02:' + consumer_path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
assert hashlib.sha256(before.encode()).hexdigest() == '53cceafae5cd1c67fb5b8ac5ea3e9f6f20f1eaba01de3df7e7c7a8115efd9e68'
need_names = ['FoodShortage', 'MoneyShortage', 'TroopShortage', 'PrisonerOverload', 'ClanCaptive', 'LowMorale', 'MountShortage', 'Overburdened', 'ClanFinanceStrain', 'ClanService', 'RomanticInteraction', 'Greeting', 'PolicyDiscussion', 'Friendship', 'Courtship', 'ArmyJoinRequest', 'BanditSuppression', 'PoliticalRivalSuppression', 'SettlementPurchase', 'SettlementSale', 'TerritorialInterrogation', 'MarriageAlliancePressure', 'RevengePressure', 'FiefGovernanceAnxiety', 'AllySupport', 'KingdomMercenaryInvite', 'KingdomVassalInvite', 'PoliticalAgenda', 'PolicySupport', 'Diplomacy']
start = consumer.index('private static string GetNeedDisplayName(')
end = consumer.index('\n\t}', start) + len('\n\t}')
actual = consumer[start:end]
for index, name in enumerate(need_names, 1):
    key = 'AF_SOCIAL_UI_ProactiveNeed_' + str(index).zfill(2)
    assert actual.count('if (string.Equals(needType, Need' + name + ', StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("' + key + '");') == 1, key
assert 'return string.IsNullOrWhiteSpace(needType) ? SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_31") : needType;' in actual
restored = re.sub(r'SocialPromptTexts.Get\("(AF_SOCIAL_UI_ProactiveNeed_\d+)"\)', lambda match: json.dumps(cn[match[1]], ensure_ascii=False), actual)
old_start = before.index('private static string GetNeedDisplayName(')
old_end = before.index('\n\t}', old_start) + len('\n\t}')
old_method = before[old_start:old_end]
# Original a911 method uses UI getters, whose CN fallback values are stable.
old_ui = {node.get('id'): node.get('text') for node in ET.fromstring(subprocess.check_output(['git', 'show', 'a911d4d0de8964a393b6bdc6eaa83705b5218b02:content/modules/AF.Module.Social/ModuleData/Languages/CNs/afsocial_ui_strings-zh-CN.xml'], cwd=ROOT)).iter('string')}
def restore_old_need(match):
    assert decode(match[2]) == en[match[1]], match[1] + ': original English fallback conserved'
    return json.dumps(old_ui[match[1]], ensure_ascii=False)
old_method = re.sub(r'SocialUiLocalization.Text\("(AF_SOCIAL_UI_ProactiveNeed_\d+)", ("(?:\\.|[^"\\])*")\)', restore_old_need, old_method)
assert restored == old_method, 'all 30 need branches, empty fallback, unknown input and original CN meaning preserved'
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
print('PASS Social: no Chinese runtime literals, 444 invariant entries, 88 invariant original + 31 source-bound packaged prompt pairs, 32 UI keys, cached package selection and fact-boundary checks; C# NOT RUN')
