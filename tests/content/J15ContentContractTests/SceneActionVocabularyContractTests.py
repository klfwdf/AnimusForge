"""Preserved bilingual recognition corpus and resource wiring; no compiled replay."""
from pathlib import Path
import hashlib
import json
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
check(len(data) == len(root) == 49, 'no missing/duplicate lists')
check(sum(map(len, data.values())) == 721, 'all original tokens')
digest = hashlib.sha256(json.dumps(data, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
# Changing aliases/negations/precedence needs explicit parser review, not translation.
check(digest == 'f810a307a9f0fa9ab9a3433012a84c6f515a7336c4518d7cb44e2a0129f7e4ba',
      'exact pre-migration values and ordering')
for key, values in data.items():
    file, field = key.split('.')
    source = (CORE / (file + '.cs')).read_text(encoding='utf-8-sig')
    check(f'{field} = SceneActionVocabulary.List("{key}");' in source, key + ' wired')
    check(all(isinstance(v, str) and v for v in values), key + ' nonempty')
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
print(f'PASS scene action vocabulary static contract: {checks} checks; 721 tokens preserved; compiled parser tests NOT RUN')
