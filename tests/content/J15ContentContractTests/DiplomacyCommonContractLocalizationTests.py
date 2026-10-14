"""Common prompt localization and style preferences preserve migration identities."""
from pathlib import Path
import re
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, decode, HAN
settings_path = 'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs'
settings = (ROOT / settings_path).read_text(encoding='utf-8-sig')
baseline = subprocess.check_output(['git', 'show', 'd446823a6:' + settings_path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
raw_defaults = json.loads((ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData/McmPromptSourceCompatibility.json').read_text(encoding='utf-8'))
helper = (ROOT / 'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.DiplomacyPromptLocalization.cs').read_text(encoding='utf-8-sig')
def constant(s, name):
    expression = re.search(r'private const string ' + name + r'\s*=\s*([\s\S]*?);\s*\n', s)[1]
    return ''.join(decode(m[0]) for m in LEX.finditer(expression) if m.lastgroup == 'string')

base = ROOT / 'content/modules/AF.Module.Diplomacy/ModuleData'
en = json.loads((base / 'DiplomacyPromptTexts.en.json').read_text(encoding='utf-8'))
zh = json.loads((base / 'DiplomacyPromptTexts.json').read_text(encoding='utf-8'))
for suffix, cn, english in (
    ('CommonCore', 'DefaultWorldDiplomacyContractCore', 'EnglishWorldDiplomacyContractCore'),
    ('DefaultPreference', 'DefaultWorldDiplomacyPreference', 'EnglishWorldDiplomacyPreference'),
    ('PreferenceGuard', 'DefaultWorldDiplomacyPreferenceGuard', 'EnglishWorldDiplomacyPreferenceGuard'),
):
    key = 'AF_DIP_Prompt_' + suffix
    assert raw_defaults[cn] == constant(baseline, cn), key + ': original Chinese migration bytes preserved'
    if suffix == 'DefaultPreference':
        assert zh[key] == raw_defaults[cn], key + ': default strategic preference preserved'
    assert en[key] == constant(helper, english) and not HAN.search(en[key]), key + ': English fallback'
    protocol = r'(?<![A-Za-z0-9_])(?:MODE=[A-Z_]+|statement|v25)(?![A-Za-z0-9_])'
    assert set(re.findall(protocol, zh[key])) == set(re.findall(protocol, en[key])), key
assert zh['AF_DIP_Prompt_PreferenceSection'] == raw_defaults['WorldDiplomacyPreferenceSectionMarker']
assert 'private static readonly string WorldDiplomacyPreferenceSectionMarker = McmPromptDefaults.Raw("WorldDiplomacyPreferenceSectionMarker");' in settings
assert 'if (!TryExtractWorldDiplomacyPreference(contract, out string preference)) return contract;' in helper
assert 'string.Equals(preference, DefaultWorldDiplomacyPreference, StringComparison.Ordinal)' in helper
assert 'string.Equals(preference, SourceDefaultWorldDiplomacyPreference, StringComparison.Ordinal)' in helper
assert 'string.Equals(contract, SourceDefaultWorldDiplomacyContractCore, StringComparison.Ordinal)' in helper
assert '+ preference + "\\n"' in helper and 'File.' not in helper and 'catch' not in helper

# Render model for the actual composition expression: arbitrary preferences survive.
for lang in (en, zh):
    for preference in ('custom preference', '保留我的自定义内容', 'custom\nmultiline\nMODE=DECLARE'):
        rendered = (lang['AF_DIP_Prompt_CommonCore'] + '\n' + lang['AF_DIP_Prompt_PreferenceSection']
                    + '\n' + preference + '\n' + lang['AF_DIP_Prompt_PreferenceGuard'])
        assert '\n' + preference + '\n' in rendered
raw = zh['AF_DIP_Prompt_CommonCore'] + '\n' + zh['AF_DIP_Prompt_PreferenceSection'] + '\n' + zh['AF_DIP_Prompt_DefaultPreference'] + '\n' + zh['AF_DIP_Prompt_PreferenceGuard']
mcm_base = ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData'
for filename, package in (('McmPromptDefaults.zh-CN.json', zh), ('McmPromptDefaults.en.json', en)):
    defaults = json.loads((mcm_base / filename).read_text(encoding='utf-8'))
    for name, suffix in (('DefaultWorldDiplomacyContractCore', 'CommonCore'), ('DefaultWorldDiplomacyPreference', 'DefaultPreference'), ('DefaultWorldDiplomacyPreferenceGuard', 'PreferenceGuard')):
        assert defaults[name] == package['AF_DIP_Prompt_' + suffix], (filename, name)
        assert 'private static string ' + name + ' => McmPromptDefaults.Get("' + name + '");' in settings
        assert 'private static string Source' + name + ' => McmPromptDefaults.Raw("' + name + '");' in settings
assert zh['AF_DIP_Prompt_CommonCore'] != raw_defaults['DefaultWorldDiplomacyContractCore']
assert zh['AF_DIP_Prompt_PreferenceGuard'] != raw_defaults['DefaultWorldDiplomacyPreferenceGuard']
assert '自定义文风优先于默认写法' in raw
assert 'MODE=DECLARE的正文篇幅是建议，COMPACT的压缩预算仍须遵守' in raw
assert 'Declaration body length is a preference; COMPACT compression budgets remain binding.' in en['AF_DIP_Prompt_CommonCore']
for package in (zh, en):
    core = package['AF_DIP_Prompt_CommonCore']
    assert '不是君主私聊' not in core and 'not private ruler chat' not in core

host_path = 'src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs'
host = (ROOT / host_path).read_text(encoding='utf-8-sig')
host_baseline = subprocess.check_output(['git', 'show', 'd446823a6:' + host_path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
voice_call = 'BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore, DiplomacyPromptTexts.Get)'
assert host.count(voice_call) == 2
restored_host = host.replace(voice_call, 'BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore)')
# Other reviewed prompt migrations are restored from the original CN catalog too;
# preserve the whole-host comparison rather than weakening it to a few call counts.
def restore_host_prompt(match):
    key = match[1]
    assert key in zh and decode(match[2]) == en[key], key
    return json.dumps(zh[key], ensure_ascii=False)
restored_host = re.sub(r'DiplomacyPromptTexts.Get\("([^"\n]+)", ("(?:\\.|[^"\\])*")\)', restore_host_prompt, restored_host)
rule_literal = 'sb.Append(' + json.dumps(zh['AF_DIP_Compatibility_008'], ensure_ascii=False) + ');'
assert restored_host.count(rule_literal) == 1
restored_host = restored_host.replace(rule_literal, 'sb.Append(WorldDiplomacyPromptContractRules.KingdomStrategicIntentRule);')
restored_host = restored_host.replace('DuelSettings.LocalizeWorldDiplomacyContractForPrompt(DuelSettings.GetWorldDiplomacyCommonContractForExternal() ?? "")', 'DuelSettings.GetWorldDiplomacyCommonContractForExternal() ?? ""')
normalize = lambda text: LEX.sub(lambda match: json.dumps(decode(match[0]), ensure_ascii=False) if match.lastgroup == 'string' else match[0], text)
host_compat = json.loads((base / 'DiplomacyMiscCompatibility.json').read_text(encoding='utf-8-sig'))
misc_cn = json.loads((base / 'DiplomacyMiscPromptTexts.json').read_text(encoding='utf-8-sig'))
misc_en = json.loads((base / 'DiplomacyMiscPromptTexts.en.json').read_text(encoding='utf-8-sig'))
restored_host = re.sub(r'DiplomacyCompatibilityTexts.Get\("([^"\n]+)"\)', lambda match: json.dumps(host_compat[match[1]], ensure_ascii=False), restored_host)
def restore_misc(match):
    assert decode(match[2]) == misc_en[match[1]], match[1] + ': package fallback parity'
    return json.dumps(misc_cn[match[1]], ensure_ascii=False)
restored_host = re.sub(r'DiplomacyMiscPromptTexts.Get\("([^"\n]+)", ("(?:\\.|[^"\\])*")\)', restore_misc, restored_host)
assert restored_host.count('" " + "统治者"') == 1
restored_host = restored_host.replace('" " + "统治者"', '" 统治者"')
assert restored_host.count('using AnimusForge;\n') == 1
restored_host = restored_host.replace('using AnimusForge;\n', '', 1)
# The new writing contract invalidates queued prompts without changing host policy.
assert restored_host.count('private const int DiplomacyPromptContractVersion = 30;') == 1
restored_host = restored_host.replace('private const int DiplomacyPromptContractVersion = 30;',
                                      'private const int DiplomacyPromptContractVersion = 29;', 1)
assert normalize(restored_host) == normalize(host_baseline), 'whole host policy/order/source bytes changed beyond reviewed prompt localization'

print('PASS common diplomacy contract: paired resources, legacy migration identities and default strategic preference preserved, custom style priority and suggested declaration length, single live host binding; compiled replay NOT RUN')
