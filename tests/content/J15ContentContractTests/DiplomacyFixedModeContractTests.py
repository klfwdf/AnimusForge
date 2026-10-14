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
removed_style_keys = {'AF_DIP_Contract_006', 'AF_DIP_Contract_007', 'AF_DIP_Contract_088'}
assert len(keys) == 115 and keys <= zh.keys() and not keys.intersection(removed_style_keys)
assert not removed_style_keys.intersection(zh)
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
checkpoint_zh = json.loads(subprocess.check_output(['git', 'show', '76fc6ed40:content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyPromptTexts.json'], cwd=ROOT).decode('utf-8-sig'))
reviewed_style = {
    'AF_DIP_Contract_002': ('正文建议篇幅为', 'Suggested body length: '),
    'AF_DIP_Contract_003': ('至', ' to '),
    'AF_DIP_Contract_004': ('字，含标点。这是偏好，不是发布的最低或最高字数。简单答复可以更短，完整条款和清楚表达优先；不要为了凑字数铺陈，也不要为了缩短删掉条件。', ' characters, including punctuation. This is a preference, not a minimum or maximum for publication. Simple replies may be shorter; complete terms and clear meaning take precedence over length. Do not pad or omit terms to fit the range.'),
    'AF_DIP_Contract_005': ('除非仍需进一步商议，否则应明确给出最终决定。', 'Include a final decision unless further discussion is still needed.'),
}
for key, (chinese, english) in reviewed_style.items():
    assert zh[key] == chinese and en[key] == english, key + ': reviewed writing policy'
def restore(m):
    used.append(m[1])
    assert decode(m[2]) == en[m[1]], m[1] + ': fallback equality'
    return json.dumps(checkpoint_zh[m[1]] if m[1] in reviewed_style else zh[m[1]], ensure_ascii=False)
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
upsert_signature = 'public static string UpsertKingdomStrategicProfilePrompt(string existing, string profilePrompt, string authorId, Func<string, string, string> localize = null)'
assert restored.count(upsert_signature) == 1, 'legacy 3-argument call remains valid'
restored = restored.replace(upsert_signature, upsert_signature.replace(', Func<string, string, string> localize = null', ''), 1)
end_rule = 'string endRule = ContractText(localize, "AF_DIP_Compatibility_008", ' + json.dumps(en['AF_DIP_Compatibility_008'], ensure_ascii=False) + ');'
assert restored.count(end_rule) == 1
restored = restored.replace(end_rule, 'string endRule = KingdomStrategicIntentRule;', 1)
fallback_branch = '\n\t\tif (ruleIndex < 0 && localize != null)\n\t\t{\n\t\t\tendRule = KingdomStrategicIntentRule;\n\t\t\truleIndex = existing.IndexOf(endRule, markerIndex, StringComparison.Ordinal);\n\t\t}'
assert restored.count(fallback_branch) == 1, 'old CN end marker remains recognized in localized existing profiles'
restored = restored.replace(fallback_branch, '', 1)
assert restored.count('ruleIndex + endRule.Length') == 1, 'suffix uses the length of the marker actually matched'
restored = restored.replace('ruleIndex + endRule.Length', 'ruleIndex + KingdomStrategicIntentRule.Length', 1)
restored = restored.replace('string endRule = KingdomStrategicIntentRule;\n\t\tint ruleIndex = existing.IndexOf(endRule,', 'int ruleIndex = existing.IndexOf(KingdomStrategicIntentRule,', 1)
# Reverse only the explicitly reviewed writing changes before the full historical comparison.
style_line = '\t\tsb.AppendLine(DiplomacyMiscPromptTexts.GetRequired("Declaration_DefaultStyle"));\n'
stance_line = '\t\tsb.AppendLine(DiplomacyMiscPromptTexts.GetRequired("Declaration_SemanticStance"));\n'
assert restored.count(style_line) == 1 and restored.count(stance_line) == 2
restored = restored.replace(style_line, '').replace(stance_line, '')
anchor = '\t\tsb.AppendLine(' + json.dumps(checkpoint_zh['AF_DIP_Contract_005'], ensure_ascii=False) + ');\n'
assert restored.count(anchor) == 1
restored = restored.replace(anchor, anchor + ''.join('\t\tsb.AppendLine(' + json.dumps(checkpoint_zh[key], ensure_ascii=False) + ');\n' for key in ('AF_DIP_Contract_006', 'AF_DIP_Contract_007')), 1)
branch = re.search(r'        else if \(string.Equals\(reason, "private_chat_style_in_public_declaration"[\s\S]*?\n        }\n', old)[0]
next_branch = '        else if (string.Equals(reason, "realm_ruler_title_conflicts_with_hard_fact"'
assert next_branch in restored and 'private_chat_style_in_public_declaration' not in source
restored = restored.replace(next_branch, branch + next_branch, 1)
assert restored.count('diplomacy-history:v30') == 1
restored = restored.replace('diplomacy-history:v30', 'diplomacy-history:v29', 1)
# The pre-change checkpoint already includes output-language enforcement. Compare
# the complete live source to it, undoing only this task's exact writing changes.
checkpoint = subprocess.check_output(['git', 'show', '76fc6ed40:' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
checkpoint_en = json.loads(subprocess.check_output(['git', 'show', '76fc6ed40:content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyPromptTexts.en.json'], cwd=ROOT).decode('utf-8-sig'))
policy_source = source.replace(style_line, '').replace(stance_line, '')
for key in reviewed_style:
    current_literal = json.dumps(en[key], ensure_ascii=False)
    assert policy_source.count(current_literal) == 1
    policy_source = policy_source.replace(current_literal, json.dumps(checkpoint_en[key], ensure_ascii=False), 1)
anchor_line = next(line for line in checkpoint.splitlines(True) if 'ContractText(localize, "AF_DIP_Contract_005"' in line)
retired_lines = ''.join(line for line in checkpoint.splitlines(True) if 'ContractText(localize, "AF_DIP_Contract_006"' in line or 'ContractText(localize, "AF_DIP_Contract_007"' in line)
assert policy_source.count(anchor_line) == 1
policy_source = policy_source.replace(anchor_line, anchor_line + retired_lines, 1)
retired_branch = re.search(r'        else if \(string.Equals\(reason, "private_chat_style_in_public_declaration"[\s\S]*?\n        }\n', checkpoint)[0]
policy_source = policy_source.replace(next_branch, retired_branch + next_branch, 1).replace('diplomacy-history:v30', 'diplomacy-history:v29', 1)
capacity_branch = ('        else if (string.Equals(reason, "public_document_capacity_exceeded", StringComparison.OrdinalIgnoreCase))\n'
                   '        {\n'
                   '            correctionBuilder.AppendLine(DiplomacyMiscPromptTexts.GetRequired("Declaration_CapacityRepair"));\n'
                   '        }\n')
assert policy_source.count(capacity_branch) == 1, 'capacity overflow uses the existing repair pipeline without truncating clauses'
policy_source = policy_source.replace(capacity_branch, '', 1)
assert normalize(policy_source) == normalize(checkpoint), 'complete checkpoint source changed outside exact reviewed writing changes'

assert len(used) == 115 and set(used) == keys
assert json.loads(en['AF_DIP_Contract_026'])['actions'][0]['peace_terms'] == {}
assert json.loads(en['AF_DIP_Contract_030'].split(': ', 1)[1])['covered_through_sequence'] == 0

app = ROOT / 'src/modules/AF.Module.Diplomacy/Application'
orch = (app / 'WorldDiplomacyOrchestration.cs').read_text(encoding='utf-8-sig')
expected_wiring = {
    'TryRebuildPendingJob': 1, 'BuildCanonicalHistoryBlock': 1,
    'EnqueueCompressionJob': 1, 'EnqueueGeneration': 1,
    'EnsureGenerationJobHasKingdomStrategicProfile': 1, 'RefreshDiplomaticActionPresentationAndPrompt': 1,
    'RefreshDiplomaticThreatPresentationAndPrompt': 1, 'EnqueueAnalysisJob': 1,
}
masked_orch = LEX.sub(lambda match: ' ' * len(match[0]), orch)
for symbol, count in expected_wiring.items():
    declaration = re.search(r'\b(?:private|public|internal) [^\n]+ ' + symbol + r'\(', masked_orch)
    assert declaration, symbol
    opening = masked_orch.index('{', declaration.end()); depth = 1; end = opening + 1
    while depth:
        depth += (masked_orch[end] == '{') - (masked_orch[end] == '}'); end += 1
    assert orch[opening:end].count('_host.PromptWorld().LocalizePrompt') == count, symbol + ': actual resolver consumer'
assert orch.count('_host.PromptWorld().LocalizePrompt') == sum(expected_wiring.values()), 'no unidentified resolver consumer'
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
print('PASS complete diplomacy contracts: 115 active pairs, three retired style constraints, reviewed style inverse, 18 callable signatures, package resolver wiring, Rules Han literals=0; C# NOT RUN')
