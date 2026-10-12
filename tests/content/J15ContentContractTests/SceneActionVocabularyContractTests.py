"""Preserved bilingual recognition corpus and resource wiring; no compiled replay."""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
CORE = ROOT / 'extensions/AnimusForge.XihaiAction/src/CoreProject'
ASSET = 'content/modules/AnimusForge.XihaiAction/ModuleData/SceneActionVocabulary.xml'
LOGICAL = 'AnimusForge.Defaults.SceneActionVocabulary.xml'
root = ET.parse(ROOT / ASSET).getroot()
data = {n.get('id'): [t.text for t in n] for n in root}
checks = 0

def check(condition, label):
    global checks
    checks += 1
    if not condition:
        raise AssertionError(label)

check(root.tag == 'vocabulary' and root.get('version') == '1', 'schema')
check(all(node.tag == 'list' and all(token.tag == 'token' for token in node) for node in root), 'loader-compatible list/token elements')
check(len(data) == len(root), 'no duplicate list ids')
evidence_path = Path(__file__).with_name('fixtures') / 'SceneActionVocabularyEvidence.json'
evidence = json.loads(evidence_path.read_text(encoding='utf-8'))
original = evidence['original_lists']
check(len(original) == 49, 'historical 49-list boundary is unchanged')
check(sum(map(len, original.values())) == 721, 'historical 721-token boundary is unchanged')
digest = hashlib.sha256(json.dumps(original, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
# This is the original immutable digest, not the hash of the expanded resource.
check(digest == 'f810a307a9f0fa9ab9a3433012a84c6f515a7336c4518d7cb44e2a0129f7e4ba',
      'exact pre-migration values and ordering')
expanded_lists = {
    'CommandParser.ClearlyNonPerformedNpcMarkers',
    'CommandParser.RuntimeControlNonPerformedMarkers',
}
english = evidence['english_nonperformed']
contractions = evidence['english_contractions']
check(len(english) == 46 and len(contractions) == 19, 'reviewed English additions are separate from original tokens')
check(len(set(english + contractions)) == 65, 'English additions have no duplicates')
for key, values in original.items():
    additions = english + contractions if key in expanded_lists else []
    check(data.get(key) == values + additions, key + ' preserves original order and only reviewed suffix')
    file, field = key.split('.')
    source = (CORE / (file + '.cs')).read_text(encoding='utf-8-sig')
    check(f'{field} = SceneActionVocabulary.List("{key}");' in source, key + ' wired')

single_values = evidence['single_values']
speech = evidence['speech_compatibility']
# Seven later extractions are frozen separately from the unchanged original fixture.
later_extractions = {'BattleSpeech.PlayerSpeechRoutingPrefix': {'path': 'extensions/AnimusForge.XihaiAction/src/Runtime/BattleSpeechDedicatedInputMissionBehavior.cs', 'values': ['我演讲：'], 'lines': [213], 'api': 'Text', 'source_sha256': 'e48ef303585f365c2ee9a2aac73723c3750a728115e09f34263c0ee4b9c5ea1a'}, 'CommandParser.AspectParticle': {'path': 'extensions/AnimusForge.XihaiAction/src/CoreProject/CommandParser.cs', 'values': ['着'], 'lines': [475], 'api': 'Text', 'source_sha256': 'f7ba82fe85ad19334f657657f3af68609c51db1821765f8374e21831c6a4b9af'}, 'CommandParser.ForceFramedPrefix': {'path': 'extensions/AnimusForge.XihaiAction/src/CoreProject/CommandParser.cs', 'values': ['强制'], 'lines': [11], 'api': 'Text', 'source_sha256': 'f7ba82fe85ad19334f657657f3af68609c51db1821765f8374e21831c6a4b9af'}, 'NpcReplyDirectiveTagV1.RuleTemplateTag': {'path': 'extensions/AnimusForge.XihaiAction/src/CoreProject/NpcReplyDirectiveTagV1.cs', 'values': ['[ACTION:SCENE_ACT:动作程序]'], 'lines': [17], 'api': 'Text', 'source_sha256': '5305b4fcb44155e2edda2737d161e899f4d042213650e4994cce7f4eeeed51d3'}, 'SceneActionFrameworkV1.ContinuationParticle': {'path': 'extensions/AnimusForge.XihaiAction/src/CoreProject/SceneActionFrameworkV1.cs', 'values': ['的'], 'lines': [583], 'api': 'Text', 'source_sha256': '6e324b12e37af5c2017b7d2842e96d4b004eee125a7e68a8f75806838c8f7cb3'}, 'SceneActionFrameworkV1.LiChar': {'path': 'extensions/AnimusForge.XihaiAction/src/CoreProject/SceneActionFrameworkV1.cs', 'values': ['礼'], 'lines': [951], 'api': 'Text', 'source_sha256': '6e324b12e37af5c2017b7d2842e96d4b004eee125a7e68a8f75806838c8f7cb3'}, 'SceneActionFrameworkV1.LiExcludedNouns': {'path': 'extensions/AnimusForge.XihaiAction/src/CoreProject/SceneActionFrameworkV1.cs', 'values': ['物', '品', '貌', '节', '服', '仪', '制', '法', '拜', '堂', '盒', '金', '炮', '券', '帽', '花', '包'], 'lines': [958, 959, 960, 961, 962, 963, 964, 965, 966, 967, 968, 969, 970, 971, 972, 973, 974], 'api': 'List', 'source_sha256': '6e324b12e37af5c2017b7d2842e96d4b004eee125a7e68a8f75806838c8f7cb3'}}
check(set(data) == set(original) | set(single_values) | set(speech) | set(later_extractions), 'no unexplained additions or missing resources')

check(sum('.LegacySource' in key for key in single_values) == 232, '232 documented LegacySource values')
check(sum('.Raw' in key for key in single_values) == 1194, '1194 independently traced F compatibility values')
# These fixtures were matched against immutable Git source literals, never just
# copied from the current XML. Re-read that evidence to guard fixture drift too.
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, decode
for key, row in later_extractions.items():
    before = subprocess.check_output(['git', 'show', 'a911d4d0de8964a393b6bdc6eaa83705b5218b02:' + row['path']], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    check(hashlib.sha256(before.encode()).hexdigest() == row['source_sha256'], key + ' immutable source identity')
    literals = []
    for line in row['lines']:
        for token in LEX.finditer(before.splitlines()[line - 1]):
            if token.lastgroup in ('string', 'char'):
                literals.append(decode(token[0] if token.lastgroup == 'string' else '"' + token[0][1:-1] + '"'))
    check(literals == row['values'], key + ' exact original consumer literals/order')
    check(data[key] == row['values'], key + ' source value conserved')
    current = (ROOT / row['path']).read_text(encoding='utf-8-sig')
    current = LEX.sub(lambda token: ' ' * len(token[0]) if token.lastgroup == 'comment' else token[0], current)
    check(current.count('SceneActionVocabulary.' + row['api'] + '("' + key + '")') == 1, key + ' exact production consumer')
before_by_file = {}
current_by_file = {}
for file, row in evidence['sources'].items():
    before = subprocess.check_output(['git', 'show', row['revision'] + ':' + row['path']], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    check(hashlib.sha256(before.encode()).hexdigest() == row['normalized_source_sha256'], file + ' source evidence identity')
    before_by_file[file] = before
    current = (ROOT / row['path']).read_text(encoding='utf-8-sig')
    current_by_file[file] = LEX.sub(lambda match: ' ' * len(match.group()) if match.lastgroup == 'comment' else match.group(), current)
for key, row in single_values.items():
    check(data.get(key) == [row['value']], key + ' exact source value')
    source = before_by_file[row['source']]
    tokens = [m for m in LEX.finditer(source) if m.lastgroup == 'string' and '$' not in m.group()[:2]]
    matches = [m for m in tokens if decode(m.group()) == row['value']]
    check(len(matches) == row['pre_migration_occurrences'], key + ' immutable source occurrences')
    check(row['pre_migration_literal'] in source and decode(row['pre_migration_literal']) == row['value'], key + ' raw CSharp literal provenance')
    check(source[:matches[0].start()].count('\n') + 1 == row['pre_migration_line'], key + ' source evidence location')
    call = f'SceneActionVocabulary.Text("{key}")'
    check(current_by_file[row['source']].count(call) == row['consumer_calls'], key + ' exact actual consumer count')
    check(row['consumer_calls'] <= len(matches), key + ' no invented or duplicated source use')
for key, values in data.items():
    check(all(isinstance(v, str) and v for v in values), key + ' nonempty')

reply = (CORE / 'SceneActionReplyCompatibility.cs').read_text(encoding='utf-8-sig')
for key, values in speech.items():
    check(data.get(key) == values, key + ' independently frozen speech markers')
    api = 'List' if key.startswith('SpeechCompat.Failure') else 'Text'
    check(f'SceneActionVocabulary.{api}("{key}")' in reply, key + ' real public Core consumer')
check('StringComparison.OrdinalIgnoreCase' in reply and 'StringComparison.Ordinal;' in reply,
      'English insensitive and Chinese ordinal reply boundary matching')
check(reply.index('SpeechCompat.OrdinaryStartZh') < reply.index('SpeechCompat.OrdinaryStartEn'), 'Chinese-first preamble selection')
check(reply.count('if (end < 0) return false;') == 1, 'missing closing boundary remains unchanged')
runtime = (ROOT / 'extensions/AnimusForge.XihaiAction/src/Runtime/AfCompatV130.cs').read_text(encoding='utf-8-sig')
check('SceneActionReplyCompatibility.IsFailureReply(response)' in runtime, 'Runtime failure marker consumer')
check('SceneActionReplyCompatibility.TryFindOrdinaryTaskBlock(' in runtime and
      'SceneActionPromptPackage.Text("BattleSpeechPreamble")' in runtime, 'Runtime package preamble consumer')
core_tests = (ROOT / 'extensions/AnimusForge.XihaiAction/tests/Program.cs').read_text(encoding='utf-8-sig')
check('TestEnglishInputBoundaries' in core_tests and 'rejected.AiFallbackRequested' in core_tests,
      'compiled English contraction/nonexecution regression retained (execution checked separately)')
check('TestReplyCompatibility' in core_tests and 'missing' in core_tests.lower(), 'compiled speech compatibility regressions retained')
v1 = current_by_file['SceneActionFrameworkV1']
for field in ('RespectMotionAdverbPatternText', 'RespectCeremonyPatternText'):
    check('private static readonly string ' + field + ' =' in v1, field + ' valid runtime resource initializer')
    check('private const string ' + field + ' =' not in v1, field + ' no CS0133 constant initializer')
for project in (ROOT / 'AnimusForge.csproj', CORE / 'AnimusForge.SceneActions.Core.csproj'):
    nodes = [n for n in ET.parse(project).getroot().iter('EmbeddedResource') if n.findtext('LogicalName') == LOGICAL]
    check(len(nodes) == 1, str(project) + ' embeds once')
    check((project.parent / nodes[0].get('Include').replace('\\', '/')).resolve() == (ROOT / ASSET).resolve(),
          str(project) + ' resolves exact corpus')
mapping = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8-sig'))
rows = [n for n in mapping['entries'] if n.get('source') == ASSET]
check(len(rows) == 1 and rows[0]['logicalName'] == LOGICAL and rows[0]['target'] == 'ModuleData/SceneActionVocabulary.xml', 'package wiring')
loader = (CORE / 'SceneActionVocabulary.cs').read_text(encoding='utf-8')
check('private static class Cache' in loader and 'Lists = Load();' in loader, 'one-time initialization independent of consumer field order')
check('DtdProcessing.Prohibit' in loader and 'MaxCharactersInDocument = 1024 * 1024' in loader, 'bounded safe XML read')
check('File.' not in loader and 'Task.' not in loader and loader.count('GetManifestResourceStream(ResourceName)') == 1, 'no disk polling or worker/game access')
print(f'PASS scene action vocabulary static contract: {checks} checks; original 49 lists / 721 ordered tokens preserved; 65 reviewed English additions in two lists; 1426 source-evidenced single values and six speech lists; compiled parser tests NOT RUN')
