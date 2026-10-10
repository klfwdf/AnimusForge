"""Verify stable routing data, display keys and complete inverse source parity."""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from terminal_category_contract_support import restore_terminal_categories

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, decode, HAN
from resource_catalog import registered_pairs
ASSET = 'content/foundation/AF.Foundation.Localization/ModuleData/TerminalCategories.json'
data = json.loads((ROOT / ASSET).read_text(encoding='utf-8'))
categories = data['Categories']
checks = 0

def check(value, label):
    global checks
    checks += 1
    if not value:
        raise AssertionError(label)

check(data['SchemaVersion'] == 1 and len(categories) == 8, 'eight canonical categories')
check(hashlib.sha256(json.dumps(data, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest() ==
      '1b83e4c9e053bb6eb5eb87cc35860f2546f2ed337597b3ffafd0f1f49b5c1e6a', 'routing bytes and label keys preserved')
english = {n.get('id'): n.get('text') for en, zh in registered_pairs(ROOT) for n in ET.parse(en).getroot().iter('string')}
for name, item in categories.items():
    match = re.fullmatch(r'\{=([^}]+)\}(.*)', item['DisplayText'])
    check(bool(match) and english[match[1]] == match[2], name + ' registered English fallback')
prefix = 'src/AF.GameAdapter.Bannerlord/UI/Terminal/'
def normalized_literals(text):
    return LEX.sub(lambda m: json.dumps(decode(m[0]), ensure_ascii=False) if m.lastgroup == 'string' else m[0], text)
for name in ('TerminalSettingsRegistry.cs', 'AnimusForgeTerminalUiModels.cs'):
    current = (ROOT / prefix / name).read_text(encoding='utf-8-sig')
    original = subprocess.check_output(['git', 'show', '192390cbb:' + prefix + name], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    restored = restore_terminal_categories(current, categories)
    if name == 'TerminalSettingsRegistry.cs':
        replacement = 'DuelSettingsUiLocalization.Text("AF_MCM_Option_DeviceCpu", "CPU (default)")'
        check(restored.count(replacement) == 1, 'one null-device display fallback uses its existing resource')
        restored = restored.replace(replacement, '"CPU（默认）"')
    check(normalized_literals(restored) == normalized_literals(original), name + ' inverse preserves all other logic')
registry = (ROOT / prefix / 'TerminalSettingsRegistry.cs').read_text(encoding='utf-8-sig')
check(not any(HAN.search(decode(m[0])) for m in LEX.finditer(registry) if m.lastgroup == 'string'), 'registry has no remaining Chinese runtime string literals')
check(len(re.findall(r'new TerminalSettingDef\("[^"]+", TerminalCategories\.\w+,', registry)) == 259, 'all definitions routed through preserved categories')
helper = (ROOT / prefix / 'TerminalCategories.cs').read_text(encoding='utf-8')
check('private static class Cache' in helper and 'Data = Load();' in helper and 'StringComparer.Ordinal' in helper, 'one-time cache with ordinal routing')
check('File.' not in helper and 'TextObject' not in helper and 'Task.' not in helper, 'no disk or language reads in matching path')
check('data.Categories.Count != 8' in helper and 'MissingMemberHandling.Error' in helper, 'schema fails closed')
logical = 'AnimusForge.Defaults.TerminalCategories.json'
nodes = [n for n in ET.parse(ROOT / 'AnimusForge.csproj').getroot().iter('EmbeddedResource') if n.findtext('LogicalName') == logical]
check(len(nodes) == 1 and nodes[0].get('Include').replace('\\', '/') == ASSET, 'exact embedded resource')
entries = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
check(sum(e['source'] == ASSET and e.get('logicalName') == logical for e in entries) == 1, 'one matching content-map entry')
check(helper.count('internal static readonly string ') == 8, 'routing reads use cached fields, not repeated dictionary lookups')
print(f'PASS terminal categories: {checks} checks, 259 definitions and complete inverse source parity; compiled UI NOT RUN')
