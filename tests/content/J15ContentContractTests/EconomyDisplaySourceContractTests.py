"""Static resource wiring and full inverse-source parity; no C# execution."""
import json
import re
import subprocess
import sys
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, decode, HAN

asset = 'content/modules/AF.Module.Economy/ModuleData/EconomyDisplaySources.json'
logical = 'AnimusForge.Defaults.EconomyDisplaySources.json'
def unique_object(pairs):
    result = {}
    for key, value in pairs:
        assert key not in result, key
        result[key] = value
    return result

data = json.loads((ROOT / asset).read_text(encoding='utf-8'), object_pairs_hook=unique_object)
assert data['SchemaVersion'] == 1 and len(data['Values']) == 92
values = data['Values']
assert all(isinstance(v, str) and v for v in values.values())
used = []
def restore(match):
    key = match[1]
    used.append(key)
    return json.dumps(values[key], ensure_ascii=False)

def normalize(text):
    return LEX.sub(lambda m: json.dumps(decode(m[0]), ensure_ascii=False)
                   if m.lastgroup == 'string' else m[0], text)

for name in ('PlayerRpForgeDisplayTexts', 'EconomyRecruitmentDetailTexts'):
    path = f'src/modules/AF.Module.Economy/Presentation/{name}.cs'
    source = (ROOT / path).read_text(encoding='utf-8-sig')
    restored = re.sub(r'EconomyDisplaySourceTexts.Get\("([\w.]+)"\)', restore, source)
    restored = restored.replace('private static readonly string CarriedFief', 'private const string CarriedFief')
    for local in ('repairFailedPrefix', 'dissolveFailedPrefix'):
        restored = restored.replace('string ' + local + ' =', 'const string ' + local + ' =')
    baseline = subprocess.check_output(['git', 'show', 'ebe438790:' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    assert normalize(restored) == normalize(baseline), name + ': exact inverse, including unknown passthrough and all regexes'
    assert not any(HAN.search(decode(m[0])) for m in LEX.finditer(source) if m.lastgroup == 'string'), name
    assert re.findall(r'"(AF_ECO_UI_\w+)"', source) == re.findall(r'"(AF_ECO_UI_\w+)"', baseline), name + ': display keys unchanged'
assert len(used) == 93 and set(used) == set(values), 'every corpus value is consumed'
nodes = [n for n in ET.parse(ROOT / 'AnimusForge.csproj').getroot().iter('EmbeddedResource') if n.findtext('LogicalName') == logical]
assert len(nodes) == 1 and nodes[0].get('Include').replace('\\', '/') == asset
entries = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
assert sum(e['source'] == asset and e.get('logicalName') == logical and e['owner'] == 'AF.Module.Economy' for e in entries) == 1
helper = (ROOT / 'src/modules/AF.Module.Economy/Presentation/EconomyDisplaySourceTexts.cs').read_text(encoding='utf-8')
assert 'private static class Cache' in helper and 'Values = Load();' in helper
assert 'StringComparer.Ordinal' in helper and 'MissingMemberHandling.Error' in helper
assert 'File.' not in helper and 'TextObject' not in helper and 'Task.' not in helper
print('PASS economy display sources: 93 references, 92 exact values, both complete inverse sources, embedding and content-map; C# NOT RUN')
