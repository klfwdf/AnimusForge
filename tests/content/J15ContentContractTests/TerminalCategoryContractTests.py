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

check(data['SchemaVersion'] == 1 and len(categories) == 9, 'eight preserved categories plus UI')
legacy_data = dict(data, Categories={k:v for k,v in categories.items() if k != 'Ui'})
check(hashlib.sha256(json.dumps(legacy_data, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest() ==
      '1b83e4c9e053bb6eb5eb87cc35860f2546f2ed337597b3ffafd0f1f49b5c1e6a', 'routing bytes and label keys preserved')
english = {n.get('id'): n.get('text') for en, zh in registered_pairs(ROOT) for n in ET.parse(en).getroot().iter('string')}
for name, item in categories.items():
    match = re.fullmatch(r'\{=([^}]+)\}(.*)', item['DisplayText'])
    check(bool(match) and english[match[1]] == match[2], name + ' registered English fallback')
prefix = 'src/AF.GameAdapter.Bannerlord/UI/Terminal/'
def normalized_literals(text):
    return LEX.sub(lambda m: json.dumps(decode(m[0]), ensure_ascii=False) if m.lastgroup == 'string' else m[0], text)

def restore_reviewed_terminal_description_literals(source):
    """Invert only approved Hint/Group literals; every other source token still has its old oracle."""
    fixture = json.loads((ROOT / 'tests/content/J15ContentContractTests/Fixtures/TerminalHintTranslationInverse.json').read_text('utf-8'))
    changes = fixture['literalChanges']
    assert len(changes) == 209 and len({(row['setting'], row['part']) for row in changes}) == 209
    string = r'"(?:\\.|[^"\\])*"'
    group = r'(?:' + string + r'|DuelSettings\.NewsSettingsGroup)'
    category = r'(?:' + string + r'|TerminalCategories\.\w+)'
    pattern = re.compile(r'new TerminalSettingDef\((' + string + r'),\s*(' + category + r'),\s*(' + group + r'),\s*(' + string + r'),\s*(' + string + r')')
    definitions = {decode(match[1]): match for match in pattern.finditer(source)}
    assert len(definitions) == 259
    replacements = []
    for row in changes:
        assert row['part'] in ('Hint', 'Group')
        match = definitions[row['setting']]
        position = 5 if row['part'] == 'Hint' else 3
        assert match[position] == row['after'], row['setting'] + ': approved full translation changed'
        replacements.append((match.start(position), match.end(position), row['before']))
    for start, end, original in sorted(replacements, reverse=True):
        source = source[:start] + original + source[end:]
    return source

# Exact inverse of nine reviewed literal-safe display bindings. Formatting is
# ignored outside lexed strings/comments; data/logic tokens remain compared.
def code_tokens(text):
    parts, cursor = [], 0
    for token in LEX.finditer(text):
        parts.append(re.sub(r'\s+', '', text[cursor:token.start()]))
        parts.append(json.dumps(decode(token[0]), ensure_ascii=False) if token.lastgroup == 'string' else token[0])
        cursor = token.end()
    parts.append(re.sub(r'\s+', '', text[cursor:]))
    return ''.join(parts)

def restore_reviewed_display_bindings(source):
    raw = json.loads((ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCaptureCompatibility.json').read_text(encoding='utf-8-sig'))
    mcm = json.loads((ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData/McmPromptSourceCompatibility.json').read_text(encoding='utf-8-sig'))
    assert mcm['Mcm_ManualDropdownModelName'] == '*手动填写*'
    source = re.sub(r'AdapterPromptCaptureTexts.PcapCompat\("([^"\n]+)"\)', lambda match: json.dumps(raw[match[1]], ensure_ascii=False), source)
    source = re.sub(r'McmPromptDefaults.Raw\("([^"\n]+)"\)', lambda match: json.dumps(mcm[match[1]], ensure_ascii=False), source)
    source = code_tokens(source)
    bindings = [
        ('hotkeyMessage', 'HotkeySet', 'Hotkey {TITLE} set to [{KEY}]', '("TITLE", (object)_activeListeningItem.Title), ("KEY", keyName)', 'hotkeyMessage.SetTextVariable("TITLE", _activeListeningItem.Title ?? ""); hotkeyMessage.SetTextVariable("KEY", keyName);'),
        ('exported', 'ExportedTo', 'Exported to:\n{PATH}', '("PATH", (object)path)', 'exported.SetTextVariable("PATH", path);'),
        ('titleText', 'NumericTitle', 'Set {TITLE}', '("TITLE", (object)Title)', 'titleText.SetTextVariable("TITLE", Title ?? "");'),
        ('promptText', 'NumericPrompt', 'Enter a value (valid range: {MIN} ~ {MAX}):\n{HINT}', '("MIN", (object)SettingDef.MinValue), ("MAX", SettingDef.MaxValue), ("HINT", HintText)', 'promptText.SetTextVariable("MIN", SettingDef.MinValue); promptText.SetTextVariable("MAX", SettingDef.MaxValue); promptText.SetTextVariable("HINT", HintText ?? "");'),
        ('titleText', 'DropdownTitle', 'Select {TITLE}', '("TITLE", (object)Title)', 'titleText.SetTextVariable("TITLE", Title ?? "");'),
        ('promptText', 'DropdownPrompt', 'Choose from the available options:\n{HINT}', '("HINT", (object)HintText)', 'promptText.SetTextVariable("HINT", HintText ?? "");'),
        ('titleText', 'TextTitle', 'Edit {TITLE}', '("TITLE", (object)Title)', 'titleText.SetTextVariable("TITLE", Title ?? "");'),
        ('triggeredMessage', 'SettingTriggered', 'Triggered: {TITLE}', '("TITLE", (object)Title)', 'triggeredMessage.SetTextVariable("TITLE", Title ?? "");'),
        ('triggerFailedMessage', 'SettingTriggerFailed', 'Trigger failed: {ERROR}', '("ERROR", (object)(ex.Message ?? new TextObject("{=AF_UI_Terminal_UnknownError}Unknown error.").ToString()))', 'triggerFailedMessage.SetTextVariable("ERROR", ex.Message ?? "Unknown error.");'),
    ]
    en_path = ROOT / 'content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml'
    en = {node.get('id'): node.get('text') for node in ET.parse(en_path).iter('string')}
    zh = {node.get('id'): node.get('text') for node in ET.parse(en_path.parent / 'CNs/af_ui_strings-zh-CN.xml').iter('string')}
    historical_en = {node.get('id'): node.get('text') for node in ET.fromstring(subprocess.check_output(['git', 'show', '192390cbb:content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml'], cwd=ROOT)).iter('string')}
    historical_zh = {node.get('id'): node.get('text') for node in ET.fromstring(subprocess.check_output(['git', 'show', '192390cbb:content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml'], cwd=ROOT)).iter('string')}
    for variable, suffix, fallback, actual_variables, original_binding in bindings:
        key = 'AF_UI_Terminal_' + suffix
        assert en[key] == historical_en[key] and zh[key] == historical_zh[key], key + ': both original resource values unchanged'
        assert sorted(re.findall(r'\{\w+\}', fallback)) == sorted(re.findall(r'\{\w+\}', en[key])) == sorted(re.findall(r'\{\w+\}', zh[key])), key
        actual = code_tokens('string ' + variable + ' = DeveloperUiText.Format(' + json.dumps(key) + ', ' + json.dumps(fallback) + ', ' + actual_variables + ');')
        original = code_tokens('var ' + variable + ' = new TextObject(' + json.dumps('{=' + key + '}' + fallback) + ');' + original_binding)
        assert source.count(actual) == 1, key + ': exact variable identity and single consumer'
        source = source.replace(actual, original, 1)
    for name in ('hotkeyMessage', 'triggeredMessage', 'triggerFailedMessage'):
        actual = 'newInformationMessage(' + name + ','
        assert source.count(actual) == 1
        source = source.replace(actual, 'newInformationMessage(' + name + '.ToString(),', 1)
    assert source.count(',exported);') == 1
    source = source.replace(',exported);', ',exported.ToString());', 1)
    formatter = (ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Editors/DeveloperUiText.cs').read_text(encoding='utf-8-sig')
    assert 'LiteralValues.Replace(text.ToString(), match =>' in formatter
    assert 'variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty' in formatter
    return source

for name in ('TerminalSettingsRegistry.cs', 'AnimusForgeTerminalUiModels.cs'):
    current = (ROOT / prefix / name).read_text(encoding='utf-8-sig')
    original = subprocess.check_output(['git', 'show', '192390cbb:' + prefix + name], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    if name == 'TerminalSettingsRegistry.cs':
        # The authorized consolidation changes only routing, metadata completion
        # and per-owner saves. Preserve the original 259 controls and delegates.
        current = current.replace('TabCategory = TerminalSettingsCoverage.CategoryFor(nameof(DuelSettings), Id, tabCategory ?? "");', 'TabCategory = tabCategory ?? "";')
        current = current.replace('TerminalSettingsCoverage.AddMissing(_allDefinitions);', '')
        current = current.replace('bool mainSaved = DuelSettings.SaveCurrentSettings();\n\t\tbool ownersSaved = TerminalSettingsCoverage.SaveDirtyOwners();\n\t\treturn mainSaved && ownersSaved;', 'return DuelSettings.SaveCurrentSettings();')
        current = current.replace('using TaleWorlds.Localization;', '')
        # Exact fourteen display-only V3 TTS descriptions adopted with main.
        fusion = json.loads((ROOT/'tests/content/J15ContentContractTests/Fixtures/TerminalTtsFusionInverse.json').read_text('utf-8'))
        for row in fusion['literalChanges']:
            check(current.count(row['after']) == 1, row['setting'] + ' exact reviewed TTS display')
            current = current.replace(row['after'], row['before'], 1)
        current = restore_reviewed_terminal_description_literals(current)
    if name == 'AnimusForgeTerminalUiModels.cs':
        current = current.replace(', TerminalCategories.Ui };', ' };')
        # These eight labels now belong to the actual nested report datasource,
        # where RuntimeLanguageChecks exercises them through both native languages.
        for suffix in range(8,16):
            pattern = r'\[DataSourceProperty\]\s*public string AF_UI_Terminal_Prefab_' + f'{suffix:03}' + r'Text =>[^\n]+;'
            original = re.sub(pattern, '', original)
    restored = restore_terminal_categories(current, categories)
    if name == 'TerminalSettingsRegistry.cs':
        replacement = 'DuelSettingsUiLocalization.Text("AF_MCM_Option_DeviceCpu", "CPU (default)")'
        check(restored.count(replacement) == 1, 'one null-device display fallback uses its existing resource')
        restored = restored.replace(replacement, '"CPU（默认）"')
    if name == 'AnimusForgeTerminalUiModels.cs':
        restored = restore_reviewed_display_bindings(restored)
    restored_tokens = code_tokens(restored)
    if name == 'TerminalSettingsRegistry.cs':
        restored_tokens = restored_tokens.replace('privatestaticstring_initializedLanguage;', '')
        restored_tokens = restored_tokens.replace('stringlanguage=MBTextManager.ActiveTextLanguage??string.Empty;if(_initialized&&string.Equals(_initializedLanguage,language,StringComparison.Ordinal))return;_initialized=true;_initializedLanguage=language;_allDefinitions.Clear();_byCategory.Clear();_byId.Clear();', 'if(_initialized)return;_initialized=true;')
        restored_tokens = restored_tokens.replace('publicstaticIReadOnlyList<TerminalSettingDef>AllDefinitions{get{EnsureInitialized();return_allDefinitions;}}', 'publicstaticIReadOnlyList<TerminalSettingDef>AllDefinitions=>_allDefinitions;')
    check(restored_tokens == code_tokens(original), name + ' inverse preserves all other logic')
registry = (ROOT / prefix / 'TerminalSettingsRegistry.cs').read_text(encoding='utf-8-sig')
check(not any(HAN.search(decode(m[0])) for m in LEX.finditer(registry) if m.lastgroup == 'string'), 'registry has no remaining Chinese runtime string literals')
check(len(re.findall(r'new TerminalSettingDef\("[^"]+", TerminalCategories\.\w+,', registry)) == 259, 'all definitions routed through preserved categories')
helper = (ROOT / prefix / 'TerminalCategories.cs').read_text(encoding='utf-8')
check('private static class Cache' in helper and 'Data = Load();' in helper and 'StringComparer.Ordinal' in helper, 'one-time cache with ordinal routing')
check('File.' not in helper and 'TextObject' not in helper and 'Task.' not in helper, 'no disk or language reads in matching path')
check('data.Categories.Count != 9' in helper and 'MissingMemberHandling.Error' in helper, 'schema fails closed')
logical = 'AnimusForge.Defaults.TerminalCategories.json'
nodes = [n for n in ET.parse(ROOT / 'AnimusForge.csproj').getroot().iter('EmbeddedResource') if n.findtext('LogicalName') == logical]
check(len(nodes) == 1 and nodes[0].get('Include').replace('\\', '/') == ASSET, 'exact embedded resource')
entries = json.loads((ROOT / 'content/content-map.json').read_text(encoding='utf-8-sig'))['entries']
check(sum(e['source'] == ASSET and e.get('logicalName') == logical for e in entries) == 1, 'one matching content-map entry')
check(helper.count('internal static readonly string ') == 9, 'routing reads use cached fields, not repeated dictionary lookups')
check(categories['Ui']['LegacyValue'] == 'UI', 'stable new UI route')
coverage = (ROOT/prefix/'TerminalSettingsCoverage.cs').read_text('utf-8')
for owner in ('DuelSettings', 'DialogueUiSettings', 'IllustratorSettings', 'CoupSettings'):
    check('AddOwner<' + owner + '>' in coverage, 'complete MCM owner ' + owner)
check('Lazy<Binding[]>' in coverage and 'Expression.Lambda' in coverage and 'SaveSettings(owner)' in coverage,
      'cached metadata and delegates; save original owner')
check('CourierLetterThemeDropdown' in coverage and 'binding.Set(owner, dropdown)' in coverage,
      'letter style is in UI and writes through the original setter')
print(f'PASS terminal categories: {checks} checks, 259 original delegates preserved + four-owner completion + UI page; compiled UI separately verified')
