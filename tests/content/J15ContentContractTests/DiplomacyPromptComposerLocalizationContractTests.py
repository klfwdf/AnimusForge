"""Static full-composer migration parity and paired prompt/protocol resources."""
from pathlib import Path
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, decode, HAN

prefix = 'AF_DIP_Prompt_Composer_'
base = ROOT / 'content/modules/AF.Module.Diplomacy/ModuleData'
en = {k: v for k, v in json.loads((base / 'DiplomacyPromptTexts.en.json').read_text(encoding='utf-8')).items() if k.startswith(prefix)}
zh = {k: v for k, v in json.loads((base / 'DiplomacyPromptTexts.json').read_text(encoding='utf-8')).items() if k.startswith(prefix)}
assert len(en) == 212 and en.keys() == zh.keys()
def literal_braces(text):
    return text.replace('[[AF_JSON_OPEN]]', '{').replace('[[AF_JSON_CLOSE]]', '}')

for key in en:
    assert en[key] and zh[key] and not HAN.search(en[key]), key
    assert re.findall(r'\{([A-Z_]+)\}', en[key]) == re.findall(r'\{([A-Z_]+)\}', zh[key]), key
    protocol = r'(?<![A-Za-z0-9_])(?:[a-z]+_[a-z_]+|MODE=[A-Z_]+)(?![A-Za-z0-9_])'
    assert set(re.findall(protocol, en[key])) == set(re.findall(protocol, zh[key])), key
    assert re.findall(r'\d+', en[key]) == re.findall(r'\d+', zh[key]), key

path = 'src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyPromptComposer.cs'
source = (ROOT / path).read_text(encoding='utf-8-sig')
baseline = subprocess.check_output(['git', 'show', '8adc66d61:' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
used = []
def restore(match):
    key = match[1]
    assert decode(match[2]) == en[key], key + ': English fallback'
    used.append(key)
    return json.dumps(literal_braces(zh[key]), ensure_ascii=False)

restored = re.sub(r'world.LocalizePrompt\("(AF_DIP_Prompt_Composer_\d+)", ("(?:\\.|[^"\\])*")\)', restore, source)
restored = restored.replace('AppendPlayerContext(StringBuilder sb, IWorldDiplomacyPromptWorld world, IWorldDiplomacyOrchestration orchestration,', 'AppendPlayerContext(StringBuilder sb, IWorldDiplomacyOrchestration orchestration,')
restored = restored.replace('AppendPlayerContext(sb, world, orchestration,', 'AppendPlayerContext(sb, orchestration,')
for old_call in (
    'BuildCurrentLegalDiplomaticOptions(legalActionsByTarget)',
    'AppendRoundSubstantiveProgressRequirement(sb, round, age, targetDays)',
    'AppendOpenOfferAnswerRequirement(sb, round, author)',
    'BuildAnalysisModeContract()',
    'DescribePotentialDiplomaticActions(actions)',
):
    localized_call = old_call[:-1] + ('' if old_call.endswith('()') else ', ') + 'world.LocalizePrompt)'
    assert restored.count(localized_call) == 1, localized_call
    restored = restored.replace(localized_call, old_call)
def normalize(s):
    return LEX.sub(lambda m: json.dumps(decode(m[0]), ensure_ascii=False) if m.lastgroup == 'string' else m[0], s)
assert normalize(restored) == normalize(baseline), 'all branching, IDs, history/raw fields and Chinese prompt bytes preserved'
assert len(used) == 237 and set(used) == en.keys()
remaining = [decode(m[0]) for m in LEX.finditer(source) if m.lastgroup == 'string' and HAN.search(decode(m[0]))]
assert remaining == ['语义分析服务未完成评估，交由本地结构化规则给出非零评价。'], 'only raw fallback analysis reason remains'
adapter = (ROOT / 'src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.PromptContext.cs').read_text(encoding='utf-8-sig')
assert 'IWorldDiplomacyPromptWorld.LocalizePrompt' in adapter
assert 'DiplomacyPromptTexts.Get(id, englishFallback)' in adapter
assert 'TextObject' not in source and 'Task.Run' not in source
schema = literal_braces(en[prefix + '015']).split(': ', 1)[1]
assert json.loads(schema)['selected_kingdom_ids'] == ['ID']
print('PASS diplomacy composer: 237 localized occurrences / 212 pairs, whole-source Chinese parity, protocol/numeric invariants, port binding and JSON schema; compiled replay NOT RUN')
