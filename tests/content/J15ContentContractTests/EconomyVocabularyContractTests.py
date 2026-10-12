"""Exact bilingual input-corpus and immutable resource wiring contract."""
from pathlib import Path
import hashlib
import json
import re
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[3]
asset='content/modules/AF.Module.Economy/ModuleData/GeneratedRpVocabulary.json'
data=json.loads((ROOT/asset).read_text(encoding='utf-8'))
source=(ROOT/'src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs').read_text(encoding='utf-8-sig')
loader=(ROOT/'src/modules/AF.Module.Economy/Host/RewardSystemBehavior.Vocabulary.cs').read_text(encoding='utf-8-sig')
checks=0
def check(ok,label):
    global checks
    checks+=1
    if not ok:raise AssertionError(label)

# Immutable migration corpus: changing this requires an intentional vocabulary change,
# not a translation that silently removes old input aliases or changes precedence.
digest=hashlib.sha256(json.dumps(data,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode('utf-8')).hexdigest()
check(digest=='7cfd04b0681d58a3e15dda41a3685b1a62c55f464615532ea86a6d3f4b4b8744',
      'migrated alias/order/kind/word-boundary corpus changed')
check(data['SchemaVersion']==1 and len(data['StringLists'])==15,'vocabulary schema and lists')
for name,values in data['StringLists'].items():
    check(f'{name} = GeneratedRpVocabulary.Strings(nameof({name}));' in source,name+' is wired')
    check(values and all(isinstance(v,str) and v.strip() for v in values),name+' values are intact')
for category,count in (('Equipment',64),('Food',28)):
    rows=data[category+'SuffixRules']
    check(len(rows)==count,category+' rule count')
    check(f'GeneratedRp{category}SuffixRules = GeneratedRpVocabulary.{category}Rules();' in source,category+' production route')
    enum=re.search(r'private enum GeneratedRp'+category+r'Kind\s*\{([^}]+)\}',source).group(1)
    kinds=set(re.findall(r'\b\w+\b',enum))
    for row in rows:
        check(row['Kind'] in kinds and row['Kind']!='None',category+' kind '+row['Kind'])
        check(type(row['RequiresEnglishWordBoundary']) is bool and row['Suffixes'],category+' boundary and suffixes')
check(sum(len(r['Suffixes']) for r in data['FoodSuffixRules'])==718,'all food aliases retained')
check(sum(len(r['Suffixes']) for r in data['EquipmentSuffixRules'])==483,'all equipment aliases retained')

def classify(value,category):
    exceptions=data['StringLists']['GeneratedRpFoodNonFoodEndingExceptions' if category=='Food' else 'GeneratedRpEquipmentNonEquipmentEndingExceptions']
    if any(value.casefold().endswith(v.casefold()) for v in exceptions):return None
    best=None;length=-1
    for row in data[category+'SuffixRules']:
        for suffix in row['Suffixes']:
            if not value.casefold().endswith(suffix.casefold()):continue
            prefix=value[:-len(suffix)]
            if row['RequiresEnglishWordBoundary'] and prefix and prefix[-1].isascii() and prefix[-1].isalnum():continue
            if len(suffix)>length:best=row['Kind'];length=len(suffix)
    return best
for word,kind in (('苹果','Fruit'),('apple','Fruit'),('牛肉','Meat'),('beef','Meat'),('清水','Water'),('drinking water','Water'),('药水','Medicine'),('potion','Medicine')):
    check(classify(word,'Food')==kind,'food '+word)
for word in ('结果','王八蛋','铁饼','香水','火药','sea water'):
    check(classify(word,'Food') is None,'food false positive '+word)
for word,kind in (('长剑','Sword'),('iron sword','Sword'),('bottle cap',None),('gold standard',None)):
    check(classify(word,'Equipment')==kind,'equipment '+word)

logical='AnimusForge.Defaults.GeneratedRpVocabulary.json'
project=ET.parse(ROOT/'AnimusForge.csproj').getroot()
nodes=[n for n in project.iter('EmbeddedResource') if (n.get('Include') or '').replace('\\','/')==asset]
check(len(nodes)==1 and nodes[0].findtext('LogicalName')==logical,'implementation embeds the exact vocabulary')
mapping=json.loads((ROOT/'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
entries=[e for e in mapping if e['source']==asset]
check(len(entries)==1 and entries[0].get('logicalName')==logical and entries[0]['target']=='ModuleData/GeneratedRpVocabulary.json',
      'content mapping preserves resource identity')
check('private static class GeneratedRpVocabulary' in loader and 'private static readonly VocabularyData Data = Load();' in loader,
      'nested static initialization avoids partial-field ordering and repeated parsing')
check(loader.count('GetManifestResourceStream(ResourceName)')==1 and 'File.Read' not in loader and 'Task.Run' not in loader,
      'single embedded read, no game/disk polling')
check('MissingMemberHandling.Error' in loader and 'data.SchemaVersion != 1' in loader,
      'invalid catalog does not silently remove rules')
print(f'PASS bilingual economy vocabulary static/simulation contract: {checks} checks, 1470 preserved tokens; compiled replay NOT RUN')
