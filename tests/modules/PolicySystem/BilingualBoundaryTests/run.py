"""Resource contracts plus real parser/reader and source-derived UI/throw boundaries.

UI TextObject and module paths are test providers. This does not run Gauntlet,
ONNX/game objects, the full target router, or perform semantic approval of a corpus.
"""
from pathlib import Path
import collections
import hashlib
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
OUT = ROOT / 'artifacts/localization/verification-20261011'
BEFORE = OUT / 'policy-diagnostic-before-0926'
RUN = OUT / 'policy-diagnostic-runtime'
RUN.mkdir(exist_ok=True)


def load(path):
    def unique(items):
        data = {}
        for key, value in items:
            assert key not in data, (str(path), 'duplicate key', key)
            data[key] = value
        return data
    return json.loads(path.read_text('utf-8-sig'), object_pairs_hook=unique)


def variables(text):
    return collections.Counter(re.findall(r'\{[A-Z][A-Z0-9_]*\}', text))


def numbers(text):
    return collections.Counter(re.findall(r'(?<![A-Za-z0-9])[-+]?\d+(?:\.\d+)?', text))


base = ROOT / 'content/modules/PolicySystem'
pairs, numeric_exceptions = [], []
for path in sorted(base.rglob('*.en.json')):
    cn = load(path.with_name(path.name.replace('.en.json', '.json')))
    en = load(path)
    assert cn.keys() == en.keys(), (str(path), 'key mismatch')
    for key in cn:
        assert type(cn[key]) is type(en[key]), (str(path), key, 'type mismatch')
        if key in ('ModuleId', 'Version'):
            assert cn[key] == en[key], (str(path), key, 'identity mismatch')
        if not isinstance(cn[key], str):
            continue
        assert variables(cn[key]) == variables(en[key]), (str(path), key, 'variable mismatch')
        assert not re.search(r'[\u3400-\u9fff]', en[key]), (str(path), key, 'EN Han text')
        if numbers(cn[key]) != numbers(en[key]):
            assert '骑马与砍杀2' in cn[key] and 'Mount & Blade II' in en[key]
            assert numbers(cn[key]) - numbers(en[key]) == {'2': 1} and not numbers(en[key]) - numbers(cn[key])
            numeric_exceptions.append([str(path.relative_to(ROOT)), key, 'game title 2 / II'])
    pairs.append({'path': path.relative_to(ROOT).as_posix(), 'keys': len(cn), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
generation = load(base / 'ModuleData/PolicyGenerationPromptTexts.json')
assert len(generation) == 687
effect_pairs = [pair for pair in pairs if '/Effects/' in pair['path']]
assert len(effect_pairs) == 19
mirrors = 0
generation_en = load(base / 'ModuleData/PolicyGenerationPromptTexts.en.json')
catalog_summaries = [key for key in generation if key.startswith('Module_') and key.endswith('_catalogSummary')]
assert all(0 < len(generation[key]) <= 60 and 0 < len(generation_en[key]) <= 60 for key in catalog_summaries), 'catalog summary length'
for pair in effect_pairs:
    name = Path(pair['path']).name[:-8]
    if name == '_Common':
        continue
    path = ROOT / pair['path']
    cn, en = load(path.with_name(name + '.json')), load(path)
    for key in ('UnderstandingPrompt', 'EvaluationPrompt'):
        catalog_key = 'Module_' + name + '_editable' + key
        assert generation[catalog_key] == cn[key] and generation_en[catalog_key] == en[key]
        mirrors += 1
xml_pairs = []
all_ui_ids = set()
for filename in ('afpol_ui_strings', 'afpol_misc_strings'):
    def xml_values(path):
        values = {}
        for node in ET.parse(path).getroot().iter('string'):
            key = node.attrib['id']
            assert key not in values, (str(path), 'duplicate id', key)
            values[key] = node.attrib['text']
        return values
    en = xml_values(base / 'ModuleData/Languages' / (filename + '.xml'))
    cn = xml_values(base / 'ModuleData/Languages/CNs' / (filename + '-zh-CN.xml'))
    assert en.keys() == cn.keys(), (filename, 'UI keys mismatch')
    assert not all_ui_ids.intersection(en), (filename, 'duplicate cross-file UI ids')
    all_ui_ids.update(en)
    for key in cn:
        assert variables(cn[key]) == variables(en[key]), (filename, key, 'UI variables mismatch')
    xml_pairs.append({'file': filename, 'keys': len(en)})
references = 0
for path in (ROOT / 'PolicySystem').rglob('*.cs'):
    text = path.read_text('utf-8-sig')
    for key in re.findall(r'Policy(?:Effect)?UiText[s]?\.(?:Get|Format)\("([^"\n]+)"\s*,', text):
        assert key in all_ui_ids, (str(path), key, 'missing UI id')
        references += 1
    for key in re.findall(r'PolicyGenerationPromptTexts\.(?:Get|Format)\("([^"\n]+)"\s*,', text):
        assert key in generation, (str(path), key, 'missing generation key')
        references += 1
effect_ui = (ROOT / 'PolicySystem/UI/PolicyEffectUiText.cs').read_text('utf-8-sig')
effect_keys = set(re.findall(r'return "([A-Za-z]+)"', effect_ui[effect_ui.index('private static string GetKey('):]))
assert len(effect_keys) == 18
for key in effect_keys:
    assert 'AFPOL_UI_Effect_' + key + 'Name' in generation
    assert all('AFPOL_UI_Effect_' + key + suffix in all_ui_ids for suffix in ('Name', 'Summary'))
hero_selectors = (ROOT / 'PolicySystem/Targets/PolicyHeroTargetSelectorResolver.cs').read_text('utf-8-sig')
occupations = set(re.findall(r'\[Occupation\.(\w+)\]', hero_selectors))
assert len(occupations) == 31
assert all('Targets_Occupation_' + occupation in generation for occupation in occupations)

# Inverse checks cover this task's edits against its local pre-value, not HEAD.
changes = load(OUT / 'policy-diagnostic-management-changes.json')
management = ROOT / 'PolicySystem/Core/CustomPolicyBehavior.Management.cs'
restored = management.read_text('utf-8-sig')
for item in reversed(changes):
    lines = restored.splitlines(keepends=True)
    index = item['line_before'] - 1
    assert item['after'] in lines[index], item
    # Exact replacement on that line prevents replacing unrelated equal strings.
    lines[index] = lines[index].replace(item['after'], item['before'], 1)
    restored = ''.join(lines)
assert restored == (BEFORE / management.relative_to(ROOT)).read_text('utf-8-sig'), 'management non-text behavior changed'

router = ROOT / 'PolicySystem/Targets/PolicyTargetSemanticRouter.cs'
before = (BEFORE / router.relative_to(ROOT)).read_text('utf-8-sig')
current = router.read_text('utf-8-sig')
old_throws = [line.strip()[6:-1] for line in before.splitlines() if 'throw new ' in line]
new_throws = [line.strip()[6:-1] for line in current.splitlines() if 'throw new ' in line]
assert len(old_throws) == len(new_throws) == 8
restored = current
for old, new in zip(old_throws, new_throws):
    restored = restored.replace(new, old, 1)
assert restored == before, 'router non-text behavior changed'

storage = ROOT / 'PolicySystem/Effects/PolicyEffectPromptService.cs'
storage_source = storage.read_text('utf-8-sig')
storage_before = (BEFORE / storage.relative_to(ROOT)).read_text('utf-8-sig')
storage_start = storage_source.index('\tprivate static string ProjectStorageErrorForDisplay(')
storage_end = storage_source.index('\tprivate static long FileFingerprint(', storage_start)
storage_snippet = storage_source[storage_start:storage_end]
restored = storage_source[:storage_start] + storage_source[storage_end:]
assert restored.count('error = ProjectStorageErrorForDisplay(ex.Message);') == 4
restored = restored.replace('error = ProjectStorageErrorForDisplay(ex.Message);', 'error = ex.Message;')
storage_old = re.findall(r'throw new InvalidOperationException\((PolicyUiTexts.Get\("[^"\n]+", ("(?:[^"\\]|\\.)*")\))\);', storage_before)
assert len(storage_old) == 6
for old, english in storage_old:
    restored = restored.replace('throw new InvalidOperationException(' + english + ');', 'throw new InvalidOperationException(' + old + ');', 1)
assert restored == storage_before, 'storage behavior changed beyond diagnostics/display projection'
balance = ROOT / 'PolicySystem/UI/SettlementBalancePopup.cs'
balance_source = balance.read_text('utf-8-sig')
balance_start = balance_source.index('\t\t\tstring displayError = ')
balance_end = balance_source.index('\t\t\tInformationManager.DisplayMessage(', balance_start)
balance_snippet = balance_source[balance_start:balance_end]
restored = balance_source[:balance_start] + balance_source[balance_end:]
restored = restored.replace('"ERROR", displayError)', '"ERROR", ex.Message)')
restored = restored.replace('throw new InvalidOperationException("No settings screen is available.");', 'throw new InvalidOperationException(PolicyUiTexts.Get("AFPOL_UI_NoSettingsScreen", "No settings screen is available."));')
assert restored == (BEFORE / balance.relative_to(ROOT)).read_text('utf-8-sig'), 'balance non-text behavior changed'

# The production parser, safe input sanitizer, prompt reader and UI reader compile
# directly. Only UI method and throw expressions are extracted from their sources.
ui_source = (ROOT / 'PolicySystem/UI/LocalPolicyUi.cs').read_text('utf-8-sig')
start = ui_source.index('\tprivate static string MapPlayerPolicyAutoDraftErrorToDisplay(')
end = ui_source.index('\n\tpublic void ExecutePublish()', start)
snippet = ui_source[start:end]
template = Path(__file__).with_name('Program.template.cs').read_text('utf-8')
diagnostics = '\n'.join('                case ' + str(i) + ': return before ? ' + old + ' : ' + new + ';'
                        for i, (old, new) in enumerate(zip(old_throws, new_throws)))
(RUN / 'Program.cs').write_text(template.replace('/*UI_SOURCE*/', snippet).replace('/*DIAGNOSTIC_SOURCE*/', diagnostics)
                              .replace('/*STORAGE_SOURCE*/', storage_snippet).replace('/*BALANCE_SOURCE*/', balance_snippet), 'utf-8')
sources = ['PolicySystem/Core/PlayerPolicyAutoDraft.cs', 'PolicySystem/Core/PolicyGenerationPromptTexts.cs',
           'PolicySystem/UI/PolicyUiTexts.cs', 'src/AF.GameAdapter.Bannerlord/UI/Common/AnimusForgeTextInputSanitizer.cs']
project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" />'
project += ''.join('<Compile Include="' + escape(str(ROOT / source)) + '" />' for source in sources)
project += '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(ROOT / '_deps_auto/Newtonsoft.Json.dll')) + '</HintPath></Reference><PackageReference Include="System.Security.Permissions" Version="8.0.0" /></ItemGroup></Project>'
(RUN / 'Boundary.csproj').write_text(project, 'utf-8')
build = subprocess.run(['dotnet', 'build', str(RUN / 'Boundary.csproj'), '--nologo', '-v:q'], cwd=ROOT, capture_output=True, text=True, encoding='utf-8')
(OUT / 'policy-diagnostic-build.log').write_text(build.stdout + build.stderr, 'utf-8')
assert build.returncode == 0, build.stdout + build.stderr
matrix = []
for package in ('CN', 'EN'):
    directory = RUN / package
    directory.mkdir(exist_ok=True)
    source = base / 'ModuleData' / ('PolicyGenerationPromptTexts' + ('.en' if package == 'EN' else '') + '.json')
    (directory / 'PolicyGenerationPromptTexts.json').write_bytes(source.read_bytes())
    for ui in ('CN', 'EN'):
        result = subprocess.run(['dotnet', str(RUN / 'bin/Debug/net8.0/Boundary.dll'), str(directory), package, ui, str(ROOT)], cwd=ROOT, capture_output=True, text=True, encoding='utf-8')
        (OUT / ('policy-diagnostic-' + package + '-' + ui + '.log')).write_text(result.stdout + result.stderr, 'utf-8')
        assert result.returncode == 0, result.stdout + result.stderr
        matrix.append(json.loads(result.stdout.strip()))
for package in ('CN', 'EN'):
    subset = [row for row in matrix if row['Package'] == package]
    assert subset[0]['Messages'] == subset[1]['Messages'], 'UI language changed prompt package'
assert matrix[0]['Messages'] != matrix[2]['Messages'], 'package language did not select real corpus'
for ui in ('CN', 'EN'):
    subset = [row for row in matrix if row['UI'] == ui]
    assert subset[0]['DisplayedParseError'] == subset[1]['DisplayedParseError'], 'package changed UI language'
report = {'effect_pairs': len(effect_pairs), 'generation_key_pairs': len(generation), 'all_json_pairs': pairs,
          'effect_template_mirrors': mirrors, 'numeric_exceptions': numeric_exceptions, 'ui_xml_pairs': xml_pairs,
          'checked_consumer_references': references, 'management_getter_replacements': len(changes),
          'router_exceptions': 8, 'exact_edit_inverse': True, 'catalog_summaries': len(catalog_summaries),
          'storage_english_exceptions': len(storage_old), 'storage_display_catches': 4, 'balance_english_exception': 1,
          'dynamic_effect_ui_keys': len(effect_keys), 'dynamic_occupation_keys': len(occupations), 'matrix': matrix,
          'scope_limit': 'No corpus-wide semantic approval; no native Gauntlet/ONNX/game or full AF build.'}
(OUT / 'policy-diagnostic-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', 'utf-8')
print('PASS: 19 effect pairs, 687 generation key pairs, 36 effect mirrors, paired UI XML, exact local inverse, four package/UI combinations; full semantic/game acceptance NOT RUN.')
