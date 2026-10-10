"""Weekly material migration and package prompt contracts; no C# compilation or game execution."""
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX, decode, HAN
BASELINE = 'a2391657bbc301f1639090646f11b754206dd33b'
base = ROOT / 'content/modules/AF.Module.Weekly/ModuleData'
raw = json.loads((base / 'WeeklyMaterialCompatibility.json').read_text(encoding='utf-8'))
zh = json.loads((base / 'WeeklyMaterialPrompts.json').read_text(encoding='utf-8'))
en = json.loads((base / 'WeeklyMaterialPrompts.en.json').read_text(encoding='utf-8'))
assert len(raw) == 888
assert zh.keys() == en.keys() and len(zh) == 101
for key in zh:
    assert not HAN.search(en[key]), key
    assert re.findall(r'\{\d+\}', zh[key]) == re.findall(r'\{\d+\}', en[key]), key
    numeric_zh = zh[key]
    for word, number in (('第一档','1'),('第二档','2'),('第三档','3'),('第四档','4')):
        numeric_zh = numeric_zh.replace(word, number)
    assert re.findall(r'\d+', numeric_zh) == re.findall(r'\d+', en[key]), key
    assert re.findall(r'\[(?:TITLE|SHORT|REPORT|TAGS|REPORT_BLOCK_BEGIN|REPORT_BLOCK_END)\]|STAB_[A-Z]+(?:_\d)?',zh[key]) == re.findall(r'\[(?:TITLE|SHORT|REPORT|TAGS|REPORT_BLOCK_BEGIN|REPORT_BLOCK_END)\]|STAB_[A-Z]+(?:_\d)?',en[key]), key

def quoted(value): return json.dumps(value, ensure_ascii=False)
def normalize(source):
    source = LEX.sub(lambda m: quoted(decode(m[0])) if m.lastgroup == 'string' else m[0], source)
    return source.replace('\r\n','\n')
def restore(source):
    source = source.replace('private static string EmptySectionText => WeeklyReportUiText.Get("AF_WEEKLY_EmptySection", "No significant changes were recorded this week.");', 'private static readonly string EmptySectionText = WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0727");')
    source = source.replace('WeeklyReportUiText.Get("AF_WEEKLY_LegacyUnsectionedReport", "This older report was not saved in three sections. Its original text follows:\\n\\n")', 'WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0728")')
    source = re.sub(r'string.Format\(WeeklyMaterialTexts.Prompt\("(AF_WEEKLY_Prompt_\d+)"\), ([^\n;]+?)\)',
        lambda m: '$' + quoted(re.sub(r'\{(\d+)\}', lambda x: '{'+m[2].split(', ')[int(x[1])]+'}', zh[m[1]])), source)
    source = re.sub(r'WeeklyMaterialTexts.Prompt\("(AF_WEEKLY_Prompt_\d+)"\)', lambda m: quoted(zh[m[1]]), source)
    source = re.sub(r'WeeklyMaterialTexts.Raw\("(AF_WEEKLY_Material_\d+)"\)', lambda m: quoted(raw[m[1]]), source)
    source = re.sub(r'case var _ when (?:\(\(label \?\? ""\)\.Trim\(\)\)|text) == ("(?:\\.|[^"\\])*"):', r'case \1:', source)
    source = source.replace('private static readonly string EmptySectionText', 'private const string EmptySectionText')
    for number, label in ((1,'Military events'), (2,'Diplomatic events'), (3,'Domestic events')):
        source = source.replace('\n\t\t\t|| TryConsumeHeading(text, '+str(number)+', "'+label+'", out section, out remainder)', '')
    return source

count = 0
for path in sorted((ROOT / 'src/modules/AF.Module.Weekly/Materials').glob('*.cs')):
    if path.name == 'WeeklyMaterialTexts.cs': continue
    source = path.read_text(encoding='utf-8-sig')
    assert not any(HAN.search(decode(m[0]) if m.lastgroup=='string' else m[0][1:-1]) for m in LEX.finditer(source) if m.lastgroup in ('string','char')), path
    old = subprocess.check_output(['git','show',BASELINE+':'+path.relative_to(ROOT).as_posix()],cwd=ROOT).decode('utf-8-sig')
    assert normalize(restore(source)) == normalize(old), path.name+': original source restored'
    count += 1
adapter_path = 'src/AF.GameAdapter.Bannerlord/Prompt/WeeklyPromptCaptureAdapter.cs'
adapter = (ROOT / adapter_path).read_text(encoding='utf-8-sig')
old = subprocess.check_output(['git','show',BASELINE+':'+adapter_path],cwd=ROOT).decode('utf-8-sig')
assert normalize(restore(adapter)) == normalize(old), 'capture adapter: original source restored, unknown material boundary unchanged'
loader = (ROOT / 'src/modules/AF.Module.Weekly/Materials/WeeklyMaterialTexts.cs').read_text(encoding='utf-8-sig')
assert loader.count('Lazy<Dictionary<string, string>>') == 4
assert 'GetModuleDataFilePath("WeeklyMaterialPrompts.json")' in loader
assert all(x not in loader for x in ('CurrentLanguage', 'ChangeLanguage', 'TextObject', 'catch'))
assert 'prompt_snapshot' not in loader
print(f'PASS Weekly Materials: {count} existing files Han literals=0, 888 raw values, 101 CN/EN prompts, full source inverse and unchanged imported-material consumer; C# NOT RUN')

for filename, is_zh in (('Languages/afweekly_ui_strings.xml', False), ('Languages/CNs/afweekly_ui_strings-zh-CN.xml', True)):
    ui = {n.get('id'): n.get('text') for n in ET.parse(base / filename).iter('string')}
    assert ui['AF_WEEKLY_EmptySection'] == (raw['AF_WEEKLY_Material_0727'] if is_zh else 'No significant changes were recorded this week.')
    assert ui['AF_WEEKLY_LegacyUnsectionedReport'] == (raw['AF_WEEKLY_Material_0728'] if is_zh else 'This older report was not saved in three sections. Its original text follows:\n\n')
ui_source = (ROOT / 'src/modules/AF.Module.Weekly/Materials/WeeklyReportTextHelper.cs').read_text(encoding='utf-8-sig')
assert 'private static string EmptySectionText =>' in ui_source, 'Resolve UI language each display, not static Chinese cache'
assert 'split.NormalizedBodyText.Trim()' in ui_source, 'Custom original body preserved'
print('PASS 2 Weekly UI fallback pairs, original body preservation, per-display language resolution; game language switching NOT RUN')
