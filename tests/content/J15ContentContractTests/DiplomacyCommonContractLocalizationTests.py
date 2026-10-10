"""Common prompt localization without changing configuration/migration bytes."""
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
assert settings == baseline, 'all raw settings/defaults/normalization/migration/public getter unchanged'
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
    assert zh[key] == constant(settings, cn), key + ': original Chinese bytes'
    assert en[key] == constant(helper, english) and not HAN.search(en[key]), key + ': English fallback'
    protocol = r'(?<![A-Za-z0-9_])(?:MODE=[A-Z_]+|statement|v25)(?![A-Za-z0-9_])'
    assert set(re.findall(protocol, zh[key])) == set(re.findall(protocol, en[key])), key
assert zh['AF_DIP_Prompt_PreferenceSection'] == constant(settings, 'WorldDiplomacyPreferenceSectionMarker')
assert 'if (!TryExtractWorldDiplomacyPreference(contract, out string preference)) return contract;' in helper
assert 'string.Equals(preference, DefaultWorldDiplomacyPreference, StringComparison.Ordinal)' in helper
assert '+ preference + "\\n"' in helper and 'File.' not in helper and 'catch' not in helper

# Render model for the actual composition expression: arbitrary preferences survive.
for lang in (en, zh):
    for preference in ('custom preference', '保留我的自定义内容', 'custom\nmultiline\nMODE=DECLARE'):
        rendered = (lang['AF_DIP_Prompt_CommonCore'] + '\n' + lang['AF_DIP_Prompt_PreferenceSection']
                    + '\n' + preference + '\n' + lang['AF_DIP_Prompt_PreferenceGuard'])
        assert '\n' + preference + '\n' in rendered
raw = zh['AF_DIP_Prompt_CommonCore'] + '\n' + zh['AF_DIP_Prompt_PreferenceSection'] + '\n' + zh['AF_DIP_Prompt_DefaultPreference'] + '\n' + zh['AF_DIP_Prompt_PreferenceGuard']
assert raw == (constant(settings, 'DefaultWorldDiplomacyContractCore') + '\n' + constant(settings, 'WorldDiplomacyPreferenceSectionMarker') + '\n' + constant(settings, 'DefaultWorldDiplomacyPreference') + '\n' + constant(settings, 'DefaultWorldDiplomacyPreferenceGuard'))
host_path = 'src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs'
host = (ROOT / host_path).read_text(encoding='utf-8-sig')
host_baseline = subprocess.check_output(['git', 'show', 'd446823a6:' + host_path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
assert host.replace('DuelSettings.LocalizeWorldDiplomacyContractForPrompt(DuelSettings.GetWorldDiplomacyCommonContractForExternal() ?? "")', 'DuelSettings.GetWorldDiplomacyCommonContractForExternal() ?? ""') == host_baseline
print('PASS common diplomacy contract: 4 paired resources, raw settings/public API preserved, single live host binding, custom preference retention; compiled replay NOT RUN')
