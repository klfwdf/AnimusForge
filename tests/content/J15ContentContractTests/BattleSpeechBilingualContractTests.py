"""Resource, locale-flow and boundary simulation checks; does not compile C#."""
from pathlib import Path
import hashlib
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
CORE = ROOT / 'extensions/AnimusForge.XihaiAction/src/CoreProject'
RUNTIME = ROOT / 'extensions/AnimusForge.XihaiAction/src/Runtime'
BASE = 'content/modules/AnimusForge.XihaiAction/ModuleData/Languages/'
paths = {'en': BASE + 'afscene_speech_strings.xml', 'zh-CN': BASE + 'CNs/afscene_speech_strings-zh-CN.xml'}
resources = {lang: {n.get('id'): n.get('text') for n in ET.parse(ROOT / path).getroot().iter('string')} for lang, path in paths.items()}
prefix = 'AF_SCENE_SPEECH_'
checks = 0

def check(value, label):
    global checks
    checks += 1
    if not value:
        raise AssertionError(label)

check(set(resources['en']) == set(resources['zh-CN']) and len(resources['en']) == 100, '100 paired speech keys')
digest = hashlib.sha256(json.dumps(resources['zh-CN'], ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
check(digest == '2d562e62ff0b05a30a28ce38b1cdb4327378a5ab4bce81f5d2d2386bc548cee6', 'original Chinese rules, pools, labels retained')
check(not re.search(r'[\u3400-\u9fff]', ''.join(resources['en'].values())), 'English resources contain no Han')
for lang, values in resources.items():
    for key, count in [('LocalFallbackSpeeches', 6), ('AudiencePool', 24), ('AudienceSuffixes', 5), ('AudienceFillers', 5)]:
        check(len(values[prefix + key + '_List'].split('\n')) == count, lang + '/' + key + ' cardinality')
    for project in (ROOT / 'AnimusForge.csproj', CORE / 'AnimusForge.SceneActions.Core.csproj'):
        nodes = [n for n in ET.parse(project).getroot().iter('EmbeddedResource') if n.findtext('LogicalName') == 'AnimusForge.SceneActions.Speech.' + lang + '.xml']
        check(len(nodes) == 1, 'embedded once')
        check((project.parent / nodes[0].get('Include').replace('\\', '/')).resolve() == (ROOT / paths[lang]).resolve(), 'exact resource path')
for file in ['BattleSpeechFrameworkV2.cs', 'BattleSpeechDiversityV1.cs', 'BattleSpeechBattlefieldFactsV1.cs']:
    source = (CORE / file).read_text(encoding='utf-8-sig')
    for key in re.findall(r'BattleSpeechText.Text\("([^"]+)"', source):
        check(all(prefix + key in values for values in resources.values()), file + '/' + key + ' exists')
core = (CORE / 'BattleSpeechFrameworkV2.cs').read_text(encoding='utf-8-sig')
check('HasSpeechLanguage(candidate, useChinese)' in core and '!HasSpeechLanguage(reply, useChinese)' in core, 'body and audience use explicit locale')
check('BattleSpeechText.List("AudienceFillers", useChinese)' in core and 'bool chinese = useChinese;' in core, 'fallback filler language is explicit')
check('return NormalizeNpcSpeechReply(rawText, minimumChars, maximumChars, forbiddenNames, out fallbackReason, true);' in core, 'legacy normalization retains Chinese default')
check('SpeechProtocolOnlyPattern.IsMatch(text)' in core and 'SpeechProtocolOnlyPattern.IsMatch(reply)' in core, 'protocol-only text rejected at both boundaries')
pattern = r'\A(?:NONE|ACCEPT|REFUSE|UNCLEAR|(?:PLAY_ACTION|PLAY_PROGRAM|ACTIONS|TACTIC|REPLIES)(?:\s+[^\r\n]*)?)\Z'
for text in ['NONE', 'PLAY_ACTION kneel', 'REPLIES Hold!', 'TACTIC ADVANCE']:
    check(bool(re.fullmatch(pattern, text)), 'protocol simulation rejects ' + text)
for text in ['Hold the line!', 'Lord Garios leads our charge.', 'Stand together!']:
    check(not re.fullmatch(pattern, text), 'normal speech is not a protocol')
address = r'(?:^|[.!?,]\s*)(?:my lord|my lady|your grace|your lordship|sir|madam)(?:[,!:?]|\s+(?:I|we|you|please)\b)'
for text in ['My lord, I await your orders.', 'Sir, please give me orders.']:
    check(bool(re.search(address, text, re.I)), 'direct-player English reply rejected')
for text in ['Lord Garios leads our charge.', 'Sir Robert guards the gate.']:
    check(not re.search(address, text, re.I), 'third-person title retained')
check('".!?;:".IndexOf(text[before])' in core and 'Watch for <enemy>' in core, 'For-faction slogan requires a clause boundary')
mission = (RUNTIME / 'BattleSpeechMissionBehavior.cs').read_text(encoding='utf-8-sig')
host = (RUNTIME / 'BattleSpeechRuntimeHost.cs').read_text(encoding='utf-8-sig')
adapter = (RUNTIME / 'AfCompatV130.cs').read_text(encoding='utf-8-sig')
plan = (RUNTIME / 'BattleSpeechMissionBehavior.V2.cs').read_text(encoding='utf-8-sig')
check(mission.count('UseChineseText = SceneActionsText.UseChineseLanguage()') == 1, 'language captured once at session creation')
check('useChineseText: _active.UseChineseText' in mission and 'UseChineseText = useChineseText' in host, 'same capture reaches reply claim/snapshot')
check('claim.UseChineseText);' in host and '_replyClaim.UseChineseText);' in host, 'both snapshot construction routes freeze language')
check(host.count('snapshot.BattlefieldFacts,\n                        snapshot.UseChineseText)') == 2 and
      'snapshot.BattlefieldFacts,\n                    snapshot.UseChineseText)' in host, 'all three prompt builders receive snapshot locale')
check(adapter.count('prompt.UseChineseText') >= 5, 'combined/combat parsing, normalization, fallback, political guard receive locale')
check('UseChineseText = session.UseChineseText' in plan and plan.count('session.UseChineseText') >= 3, 'player speech plan and fallback preserve session locale')
worker = (RUNTIME / 'AfV130AuxiliaryTextClassifier.cs').read_text(encoding='utf-8-sig')
check('request.UseChineseText ?? _useChinese' in worker and '_alternatePrompts.Value' in worker, 'classifier uses request locale with cached compatibility fallback')
check('TextObject' not in worker and 'UseChineseLanguage()' not in worker, 'no worker-side engine locale reads')
loader = (CORE / 'BattleSpeechText.cs').read_text(encoding='utf-8')
check('private static class Chinese' in loader and 'private static class English' in loader and
      'DtdProcessing.Prohibit' in loader and 'MaxCharactersInDocument = 1024 * 1024' in loader, 'bounded one-time per-language resources')
print(f'PASS battle speech bilingual static/simulation contract: {checks} checks; compiled Core, actual LLM and game NOT RUN')
