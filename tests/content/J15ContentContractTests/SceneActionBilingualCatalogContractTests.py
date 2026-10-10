"""Bilingual prompt/resource wiring, exact Chinese corpus, and frozen action keys.

Static checks and format simulation only; compiled Core tests are separate.
"""
from pathlib import Path
import hashlib
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
CORE = ROOT / 'extensions/AnimusForge.XihaiAction/src/CoreProject'
RUNTIME = ROOT / 'extensions/AnimusForge.XihaiAction/src/Runtime'
BASE = 'content/modules/AnimusForge.XihaiAction/ModuleData/Languages/'
prefix = 'AF_SCENE_CATALOG_'
paths = {'en': BASE + 'afscene_catalog_strings.xml', 'zh-CN': BASE + 'CNs/afscene_catalog_strings-zh-CN.xml'}
catalogs = {lang: {n.get('id'): n.get('text') for n in ET.parse(ROOT / path).getroot().iter('string')}
            for lang, path in paths.items()}
checks = 0

def check(value, label):
    global checks
    checks += 1
    if not value:
        raise AssertionError(label)

check(set(catalogs['en']) == set(catalogs['zh-CN']) and len(catalogs['en']) == 115, '115 paired keys')
digest = hashlib.sha256(json.dumps(catalogs['zh-CN'], ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
check(digest == '11f10f0cda62cdf4005503a2b7105b068b9ea52ef87225adc3b76648e4619426', 'original Chinese prompts and examples retained')
check(not re.search(r'[\u3400-\u9fff]', ''.join(catalogs['en'].values())), 'English corpus contains no Han')
keys = {key[len(prefix):-len('_Name')] for key in catalogs['en'] if key.endswith('_Name')}
check(len(keys) == 27, 'frozen V4 action count')
for key in keys:
    for lang, values in catalogs.items():
        for field in ('Name', 'Description', 'Positive', 'Negative'):
            check(bool(values[prefix + key + '_' + field]), lang + '/' + key + '/' + field)
        for field in ('Positive', 'Negative'):
            check(len(values[prefix + key + '_' + field].split('\n')) >= 2, lang + '/' + key + '/' + field + ' examples')
    file = 'SceneActionFrameworkV4.cs' if key in ('command', 'follow_me', 'cut_throat') else 'SceneActionFrameworkV3.cs'
    source = (CORE / file).read_text(encoding='utf-8-sig')
    for field in ('Name', 'Description', 'Positive', 'Negative'):
        check(f'SceneActionCatalogText.Get("{key}", true).{field}' in source, key + ' preserves legacy metadata')
for lang, values in catalogs.items():
    system = values[prefix + 'Prompt_SystemPromptPreamble']
    for token in ('NONE', 'PLAY_ACTION <key>', 'PLAY_PROGRAM <program>', 'allowedIntentKeys', 'implicitEmotionIntentKeys',
                  'PlayerSceneShout', 'NpcSceneShoutReply', 'previousPlayerText', 'fullNpcReplyText', 'cut_throat'):
        check(token in system, lang + ' classifier protocol ' + token)
    plan = values[prefix + 'Prompt_BattleSpeechPlanSystemPromptPreamble']
    for token in ('ACTIONS NONE', 'TACTIC NONE', 'TACTIC ADVANCE', 'REPLIES NONE', 'allowAdvance=false',
                  'audienceReplyCount', 'audienceReplyMinimumChars', 'audienceReplyMaximumChars', 'PLAY_PROGRAM laugh>command'):
        check(token in plan, lang + ' speech protocol ' + token)
    for project in (ROOT / 'AnimusForge.csproj', CORE / 'AnimusForge.SceneActions.Core.csproj'):
        nodes = [n for n in ET.parse(project).getroot().iter('EmbeddedResource')
                 if n.findtext('LogicalName') == 'AnimusForge.SceneActions.Catalog.' + lang + '.xml']
        check(len(nodes) == 1, str(project) + '/' + lang + ' resource')
        check((project.parent / nodes[0].get('Include').replace('\\', '/')).resolve() == (ROOT / paths[lang]).resolve(), 'resource resolves')
for version in (3, 4):
    source = (CORE / f'SceneActionFrameworkV{version}.cs').read_text(encoding='utf-8-sig')
    check('=> BuildClassifierDefinitionBlock(allowedIntentKeys, true);' in source, 'legacy overload remains')
    check('.Where(entry => allowed.Contains(entry.IntentKey))' in source and
          'SceneActionCatalogText.FormatEntry(entry.IntentKey, useChinese, true)' in source, 'allow-list filters translated definitions')
loader = (CORE / 'SceneActionCatalogText.cs').read_text(encoding='utf-8')
check('private static class ChineseCache' in loader and 'private static class EnglishCache' in loader, 'per-language one-time caches')
check('DtdProcessing.Prohibit' in loader and 'MaxCharactersInDocument = 1024 * 1024' in loader, 'safe bounded XML load')
worker = (RUNTIME / 'AfV130AuxiliaryTextClassifier.cs').read_text(encoding='utf-8-sig')
check('TaleWorlds.Localization' not in worker and 'TextObject' not in worker, 'no language resolution on classifier workers')
check(worker.count('new SceneActionClassifierPrompts(useChinese)') == 1 and worker.count('BuildClassifierDefinitionBlock(allowed') == 2, 'single immutable prompt snapshot, both classifier paths')
check('BuildClassifierDefinitionBlock(allowed, useChinese)' in worker and 'request.UseChineseText ?? _useChinese' in worker and
      'BuildClassifierDefinitionBlock(allowedIntentKeys, _useChinese)' in worker, 'session/legacy snapshot language reaches both definitions')
check('_alternatePrompts.Value' in worker and 'new Lazy<SceneActionClassifierPrompts>' in worker,
      'alternate request language reuses a cached immutable snapshot')
bridge = (RUNTIME / 'AfCompatV130.cs').read_text(encoding='utf-8-sig')
check('new AfV130AuxiliaryTextClassifier(_classifierApiMethod, useChinese)' in bridge and
      'new AfV130AuxiliaryTextClassifier(configuredTransport, useChinese)' in bridge, 'both transports receive captured language')
host = (ROOT / 'src/AF.GameAdapter.Bannerlord/SceneActions/SceneActionDirectiveController.cs').read_text(encoding='utf-8-sig')
check('BuildCatalogText(offeredKeys, useChinese)' in host and 'AF_RUNTIME_SceneActions_Language' in host, 'unified postprocess language route')
print(f'PASS scene action bilingual catalog: {checks} static checks; compiled Core/LLM/game replay NOT RUN')
