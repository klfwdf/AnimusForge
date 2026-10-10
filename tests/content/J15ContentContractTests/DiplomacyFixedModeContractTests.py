"""Package-selected fixed MODE contracts; static/inverse evidence, not C# replay."""
from pathlib import Path
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, decode, HAN
base = ROOT / 'content/modules/AF.Module.Diplomacy/ModuleData'
en = json.loads((base / 'DiplomacyPromptTexts.en.json').read_text(encoding='utf-8'))
zh = json.loads((base / 'DiplomacyPromptTexts.json').read_text(encoding='utf-8'))
compat = json.loads((base / 'DiplomacyPromptCompatibility.json').read_text(encoding='utf-8'))
keys = {k for k in en if k.startswith('AF_DIP_Contract_')}
assert len(keys) == 118 and keys <= zh.keys()
for key in keys:
    assert en[key] and zh[key] and not HAN.search(en[key]), key
    protocol = r'(?<![A-Za-z0-9_])(?:[a-z]+_[a-z_]+|MODE=[A-Z_]+)(?![A-Za-z0-9_])'
    assert set(re.findall(protocol, en[key])) == set(re.findall(protocol, zh[key])), key
    assert re.findall(r'\d+', en[key]) == re.findall(r'\d+', zh[key]), key
assert 'must use English' not in ''.join(en[k] for k in keys)
assert 'Write the body in English' not in ''.join(en[k] for k in keys)

path = 'src/modules/AF.Module.Diplomacy/Domain/WorldDiplomacyPromptContractRules.cs'
source = (ROOT / path).read_text(encoding='utf-8-sig')
old = subprocess.check_output(['git', 'show', 'dae630884:' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
used = []
def restore(m):
    used.append(m[1])
    assert decode(m[2]) == en[m[1]], m[1] + ': fallback equality'
    return json.dumps(zh[m[1]], ensure_ascii=False)
restored = re.sub(r'ContractText\(localize, "(AF_DIP_Contract_\d+)", ("(?:\\.|[^"\\])*")\)', restore, source)
start = restored.index('\n    internal static string ContractText(')
end = restored.index('\n\tpublic static string StablePromptHash(', start)
restored = restored[:start] + restored[end:]
methods = ('AppendDiplomaticDeclarationWritingContract', 'BuildDiplomaticDeclarationModeContract',
           'BuildCanonicalHistoryCompressionModeContract', 'BuildCanonicalHistorySystemPrompt',
           'BuildGenerationSystemPrompt', 'BuildRelayGenerationSystemPrompt', 'BuildDeclareModePrompt',
           'AppendRoundSubstantiveProgressRequirement', 'BuildAnalysisSystemPrompt', 'BuildAnalysisModeContract',
           'BuildTokenCompressionPrompt', 'BuildRoundCompressionSystemPrompt', 'BuildRealmInstitutionalVoiceText',
           'BuildCurrentLegalDiplomaticOptions', 'DescribePotentialDiplomaticActions', 'AppendOpenOfferAnswerRequirement',
           'BuildPolicySignalContext', 'AppendGeneratedRepairCorrection')
for name in methods:
    restored, count = re.subn(r'^[ \t]*public static (?:void|string) ' + name + r'\([^{};]*?\)\s*=> [^;]+;\n\n', '', restored, flags=re.M)
    assert count == 1, name + ': original callable signature retained'
restored = restored.replace(', Func<string, string, string> localize)', ')').replace('(Func<string, string, string> localize)', '()')
restored = restored.replace(', localize)', ')')
for name in methods:
    restored = restored.replace(name + '(localize)', name + '()')
restored = re.sub(r'CompatibilityContractText.Values\["(AF_DIP_Compatibility_\d+)"\]', lambda m: json.dumps(compat[m[1]], ensure_ascii=False), restored)
restored = restored.replace('internal static readonly string ', 'internal const string ')
for label in ('institutionalHeading', 'familyHeading', 'actorProfileHeading', 'relayProfileHeading'):
    restored = restored.replace('string ' + label + ' =', 'const string ' + label + ' =')
restored = restored.replace('\n\t\tif (institutionalIndex < 0) institutionalIndex = prompt.IndexOf("[Institutional, legitimacy and ceremonial voice of the issuing kingdom]", StringComparison.Ordinal);', '')
restored = restored.replace('\n\t\t\tif (familyIndex < 0) familyIndex = prompt.IndexOf("[Authoritative people and family relationships]", institutionalIndex + 1, StringComparison.Ordinal);', '')
restored = restored.replace('prompt.IndexOf(familyHeading, institutionalIndex + 1,', 'prompt.IndexOf(familyHeading, institutionalIndex + institutionalHeading.Length,')

def normalize(s):
    s = LEX.sub(lambda m: json.dumps(decode(m[0]), ensure_ascii=False) if m.lastgroup == 'string' else m[0], s)
    return re.sub(r'\n[ \t]*\n+', '\n\n', s)
assert normalize(restored) == normalize(old), 'complete original Chinese algorithm/contract restored'
assert len(used) == 118 and set(used) == keys
assert json.loads(en['AF_DIP_Contract_026'])['actions'][0]['peace_terms'] == {}
assert json.loads(en['AF_DIP_Contract_030'].split(': ', 1)[1])['covered_through_sequence'] == 0

app = ROOT / 'src/modules/AF.Module.Diplomacy/Application'
orch = (app / 'WorldDiplomacyOrchestration.cs').read_text(encoding='utf-8-sig')
assert orch.count('_host.PromptWorld().LocalizePrompt') == 6, 'new, rebuild, two refreshes, compression, analysis'
schedule = (app / 'WorldDiplomacyOrchestration.Scheduling.cs').read_text(encoding='utf-8-sig')
assert 'BuildDeclareModePrompt(basePrompt + tail, _host.PromptWorld().LocalizePrompt)' in schedule
prep = (app / 'WorldDiplomacyJobPreparationApplication.cs').read_text(encoding='utf-8-sig')
assert prep.count('return Rebuild(port, orchestration, job, localize);') == 2
assert 'BuildDeclareModePrompt(dynamicPrompt, localize)' in prep
repair = (app / 'WorldDiplomacyDraftRepairApplication.cs').read_text(encoding='utf-8-sig')
assert 'BuildDeclareModePrompt(correctionBuilder.ToString(), world.LocalizePrompt)' in repair
for project in (ROOT / 'AnimusForge.csproj', ROOT / 'tests/modules/AF.Module.Diplomacy/WorldDiplomacyRoundLifecycle.SmokeTests/WorldDiplomacyRoundLifecycle.SmokeTests.csproj'):
    nodes = [n for n in ET.parse(project).iter('EmbeddedResource') if (n.findtext('LogicalName') or n.get('LogicalName')) == 'AnimusForge.Diplomacy.PromptCompatibility.json']
    assert len(nodes) == 1
assert not any(HAN.search(decode(m[0]) if m.lastgroup == 'string' else m[0][1:-1]) for m in LEX.finditer(source) if m.lastgroup in ('string', 'char')), 'Rules runtime Chinese literal count must be zero'
for key in keys:
    assert compat[key] == zh[key]
composer = (app / 'WorldDiplomacyPromptComposer.cs').read_text(encoding='utf-8-sig')
for call in ('BuildCurrentLegalDiplomaticOptions(legalActionsByTarget, world.LocalizePrompt)',
             'AppendRoundSubstantiveProgressRequirement(sb, round, age, targetDays, world.LocalizePrompt)',
             'AppendOpenOfferAnswerRequirement(sb, round, author, world.LocalizePrompt)',
             'BuildAnalysisModeContract(world.LocalizePrompt)',
             'DescribePotentialDiplomaticActions(actions, world.LocalizePrompt)'):
    assert call in composer, call
assert 'protectedTokens, localize)' in (app / 'WorldDiplomacyHistoryCompressionApplication.cs').read_text(encoding='utf-8-sig')
assert '() => world.BuildGovernmentHardFact(author), world.LocalizePrompt)' in repair
world = (ROOT / 'src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs').read_text(encoding='utf-8-sig')
assert world.count('BuildRealmInstitutionalVoiceText(kingdomName, cultureName, rulerTitle, governmentHardFact, lore, DiplomacyPromptTexts.Get)') == 2
print('PASS complete diplomacy contracts: 118 pairs, original Chinese inverse, 18 callable signatures, package resolver wiring, Rules Han literals=0; C# NOT RUN')
