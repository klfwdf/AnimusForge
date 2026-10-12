"""Preserve exact historical migration inputs and their one-time resource loading."""
from pathlib import Path
import hashlib
import json
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
ASSET = 'content/foundation/AF.Foundation.Localization/ModuleData/LegacySettingsPrompts.json'
data = json.loads((ROOT / ASSET).read_text(encoding='utf-8'))
source = (ROOT / 'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs').read_text(encoding='utf-8-sig')
loader = (ROOT / 'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.LegacyPromptDefaults.cs').read_text(encoding='utf-8')
checks = 0

def check(value, label):
    global checks
    checks += 1
    if not value:
        raise AssertionError(label)

digest = hashlib.sha256(json.dumps(data, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
check(digest == 'e670050b8c5ac31ebb7d04e22451da404f50a326454cc52d442d833b8fe634bc', 'all historical values byte-equivalent to old constant expressions')
check(data['SchemaVersion'] == 1 and len(data['Values']) == 54, 'complete corpus')
for key, value in data['Values'].items():
    check(key.startswith(('Previous', 'Leaked')) and bool(value), 'historical input only: ' + key)
    check(f'private static readonly string {key} = LegacySettingsPrompts.Get(nameof({key}));' in source,
          'private field and exact key retained: ' + key)
check('string.Equals(text, PreviousDefaultWorldDiplomacyPreferenceV4, StringComparison.Ordinal)' in source,
      'old default migration still uses ordinal equality')
check('text.Contains(PreviousDefaultWorldDiplomacyPreferenceV4' not in source and
      'text.StartsWith(PreviousDefaultWorldDiplomacyPreferenceV4' not in source, 'no broadened migration matching')
check('private static class LegacySettingsPrompts' in loader and 'Values = Load();' in loader,
      'nested one-time cache, independent of partial-field order')
check('File.' not in loader and 'Task.' not in loader and loader.count('GetManifestResourceStream(ResourceName)') == 1,
      'no per-access disk read or game-thread work')
check('MissingMemberHandling.Error' in loader and 'MaxDepth = 8' in loader and 'StringComparer.Ordinal' in loader,
      'strict schema and ordinal lookup')
logical = 'AnimusForge.Defaults.LegacySettingsPrompts.json'
nodes = [n for n in ET.parse(ROOT / 'AnimusForge.csproj').getroot().iter('EmbeddedResource') if n.findtext('LogicalName') == logical]
check(len(nodes) == 1 and nodes[0].get('Include').replace('\\', '/') == ASSET, 'embedded resource matches')
entries = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
mapped = [e for e in entries if e['source'] == ASSET]
check(len(mapped) == 1 and mapped[0]['logicalName'] == logical, 'content mapping matches')
print(f'PASS legacy settings prompts: {checks} static checks, 54 exact historical values; compiled migrations NOT RUN')
