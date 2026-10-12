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
assert zh.keys() == en.keys()
# The original 101 prompt contracts are a fixed revision, not a cap that forces
# later package-language consumers back to raw Chinese.
resource_baseline = json.loads(subprocess.check_output(['git', 'show', '8a29ce5f49a76230006cb508fd511d00a135bd8a:content/modules/AF.Module.Weekly/ModuleData/WeeklyMaterialPrompts.json'], cwd=ROOT).decode('utf-8-sig'))
assert len(resource_baseline) == 101
assert all(zh.get(key) == value for key, value in resource_baseline.items()), 'Original Chinese prompt values changed or lost'
extra = zh.keys() - resource_baseline.keys()
named = {'AF_WEEKLY_Prompt_WordSeparator': '', 'AF_WEEKLY_Prompt_ListSeparator': '、', 'AF_WEEKLY_Prompt_ParenthesisSentenceEnd': '）。', 'AF_WEEKLY_Prompt_SentenceEnd': '。', 'AF_REC_BulletinScopeLine': '快报视角：天下大事，但以{AREA}读者关心的角度组织；与玩家本人或该国相关的事实优先交代', 'AF_REC_BulletinScopeFallbackArea': '玩家所在地区'}
for key in extra:
    assert key in raw or key in named, key
    assert zh[key] == (raw[key] if key in raw else named[key]), key + ': original value preserved'
all_weekly_source = '\n'.join(path.read_text(encoding='utf-8-sig') for path in (ROOT / 'src').rglob('*.cs'))
for key in extra:
    assert re.search(r'(?:Prompt|Raw|Get)\("' + re.escape(key) + r'"', all_weekly_source), key + ': no actual source consumer'

for key in zh:
    assert not HAN.search(en[key]), key
    assert re.findall(r'\{(?:\d+|[A-Z_]+)\}', zh[key]) == re.findall(r'\{(?:\d+|[A-Z_]+)\}', en[key]), key
    numeric_zh = zh[key]
    for word, number in (('第一档','1'),('第二档','2'),('第三档','3'),('第四档','4')):
        numeric_zh = numeric_zh.replace(word, number)
    assert re.findall(r'\d+', numeric_zh) == re.findall(r'\d+', en[key]), key
    assert re.findall(r'\[(?:TITLE|SHORT|REPORT|TAGS|REPORT_BLOCK_BEGIN|REPORT_BLOCK_END)\]|STAB_[A-Z]+(?:_\d)?',zh[key]) == re.findall(r'\[(?:TITLE|SHORT|REPORT|TAGS|REPORT_BLOCK_BEGIN|REPORT_BLOCK_END)\]|STAB_[A-Z]+(?:_\d)?',en[key]), key

def quoted(value): return json.dumps(value, ensure_ascii=False)
def normalize(source):
    source = LEX.sub(lambda m: quoted(decode(m[0])) if m.lastgroup == 'string' else m[0], source)
    return source.replace('\r\n','\n')
adapter_prompt = json.loads((ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.zh-CN.json').read_text(encoding='utf-8'))
adapter_english = json.loads((ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.en.json').read_text(encoding='utf-8'))
adapter_compat = json.loads((ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCaptureCompatibility.json').read_text(encoding='utf-8-sig'))

def restore(source):
    # Reviewed policy-only request projection runs after detached copies exist.
    # Invert its exact hunk so the immutable pre-migration oracle still checks
    # every original branch, save clone, qualification and source material.
    policy_projection = '''\t\tforeach (EventMaterialReference material in group.PromptMaterials)
\t\t{
\t\t\tif (material == null || !PolicyWeeklyFactPromptProjection.IsPolicyKind(material.MaterialType)) continue;
\t\t\tmaterial.Label = PolicyWeeklyFactPromptProjection.Label(material.MaterialType, material.Label);
\t\t\tmaterial.SnapshotText = PolicyWeeklyFactPromptProjection.Snapshot(material.MaterialType, material.SnapshotText);
\t\t}
'''
    if 'PolicyWeeklyFactPromptProjection' in source:
        assert source.count(policy_projection) == 1, 'Only the reviewed policy request hunk may be inverted'
        assert source.index('group.PromptMaterials = shortOnly ? BuildShort(group) : BuildFull(group);') < source.index(policy_projection)
        source = source.replace(policy_projection, '', 1)
    source = re.sub(r'AdapterPromptCaptureTexts.PcapCompat\("([^"\n]+)"\)', lambda match: quoted(adapter_compat[match[1]]), source)
    source = re.sub(r'AdapterPromptCaptureTexts.PcapText\("([^"\n]+)"\)', lambda match: quoted(adapter_prompt[match[1]]), source)
    source = source.replace('WeeklyMaterialTexts.ProjectRaw(text4)', 'text4')
    source = source.replace('text + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_WordSeparator") + text4', 'text + text4')
    source = source.replace('string.Join(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_ListSeparator"),\n\t\t\tresult.Split(\'、\').Select(WeeklyMaterialTexts.ProjectRaw))', 'result')
    source = source.replace('.Select(WeeklyMaterialTexts.ProjectRaw)', '')
    source = source.replace('\t\t// Aggregate records retain their original labels; project only the derived prompt.\n', '')
    source = re.sub(r'\t\tstring result = ([^\n]+);\n\t\treturn result;', r'\t\treturn \1;', source)
    source = source.replace('private static string EmptySectionText => WeeklyReportUiText.Get("AF_WEEKLY_EmptySection", "No significant changes were recorded this week.");', 'private static readonly string EmptySectionText = WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0727");')
    source = source.replace('WeeklyReportUiText.Get("AF_WEEKLY_LegacyUnsectionedReport", "This older report was not saved in three sections. Its original text follows:\\n\\n")', 'WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0728")')
    source = re.sub(r'string.Format\(WeeklyMaterialTexts.Prompt\("(AF_WEEKLY_Prompt_\d+)"\), ([^\n;]+?)\)',
        lambda m: '$' + quoted(re.sub(r'\{(\d+)\}', lambda x: '{'+m[2].split(', ')[int(x[1])]+'}', zh[m[1]])), source)
    source = re.sub(r'WeeklyMaterialTexts.Prompt\("(AF_WEEKLY_[A-Za-z0-9_]+)"\)', lambda m: quoted(zh[m[1]]), source)
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
# Three English labels were added as recognition aliases. Prove the exact
# aliases and package labels, then remove only those branches for baseline comparison.
aliases = [('围城与守城', 'siege and defense'), ('军团行动', 'army actions'), ('家族与王国归属', 'clan and kingdom allegiance')]
restored_adapter = restore(adapter)
for chinese_label, english_label in aliases:
    chinese_guard = 'string.Equals((material.Label ?? "").Trim(), ' + quoted(chinese_label) + ', StringComparison.OrdinalIgnoreCase)'
    english_guard = 'string.Equals((material.Label ?? "").Trim(), ' + quoted(english_label) + ', StringComparison.OrdinalIgnoreCase)'
    assert restored_adapter.count(chinese_guard) == 1 and restored_adapter.count(english_guard) == 1
    assert any(zh[key] == chinese_label and en[key].casefold() == english_label.casefold() for key in zh)
    restored_adapter = restored_adapter.replace('\n\t\t\t|| ' + english_guard, '')
# Normalize formatting outside lexed literals only; never erase spaces in strings.
def code_tokens(text):
    text = normalize(text)
    result, cursor = [], 0
    for token in LEX.finditer(text):
        result.append(re.sub(r'\s+', '', text[cursor:token.start()]))
        result.append(token[0])
        cursor = token.end()
    result.append(re.sub(r'\s+', '', text[cursor:]))
    return ''.join(result)
assert code_tokens(restored_adapter) == code_tokens(old), 'capture adapter: original source restored, unknown material boundary unchanged'
for key in re.findall(r'AdapterPromptCaptureTexts.PcapText\("([^"\n]+)"\)', adapter):
    assert key in adapter_prompt and key in adapter_english
    assert re.findall(r'\d+', adapter_prompt[key]) == re.findall(r'\d+', adapter_english[key].replace('week-zero', 'week-0')), key
    assert re.findall(r'\[(?:TITLE|SHORT|REPORT|TAGS)\]', adapter_prompt[key]) == re.findall(r'\[(?:TITLE|SHORT|REPORT|TAGS)\]', adapter_english[key]), key
loader = (ROOT / 'src/modules/AF.Module.Weekly/Materials/WeeklyMaterialTexts.cs').read_text(encoding='utf-8-sig')
assert set(re.findall(r'private static readonly Lazy<Dictionary<string, string>> (\w+) =', loader)) == {'RawValues', 'PromptValues', 'SourceProjection'}
assert loader.count('File.ReadAllText(') == 1 and loader.count('GetManifestResourceStream(') == 1
assert 'projection[source] = entry.Value' in loader and 'RawValues.Value.TryGetValue(entry.Key, out string source)' in loader
assert 'SourceProjection.Value.TryGetValue(source, out string text) ? text : source' in loader
assert 'GetModuleDataFilePath("WeeklyMaterialPrompts.json")' in loader
assert all(x not in loader for x in ('CurrentLanguage', 'ChangeLanguage', 'TextObject', 'catch'))
assert 'prompt_snapshot' not in loader
print(f'PASS Weekly Materials: {count} existing files Han literals=0, 888 raw values, {len(zh)} CN/EN prompts with fixed 101-key baseline, full source inverse and unchanged imported-material consumer; C# NOT RUN')

for filename, is_zh in (('Languages/afweekly_ui_strings.xml', False), ('Languages/CNs/afweekly_ui_strings-zh-CN.xml', True)):
    ui = {n.get('id'): n.get('text') for n in ET.parse(base / filename).iter('string')}
    assert ui['AF_WEEKLY_EmptySection'] == (raw['AF_WEEKLY_Material_0727'] if is_zh else 'No significant changes were recorded this week.')
    assert ui['AF_WEEKLY_LegacyUnsectionedReport'] == (raw['AF_WEEKLY_Material_0728'] if is_zh else 'This older report was not saved in three sections. Its original text follows:\n\n')
ui_source = (ROOT / 'src/modules/AF.Module.Weekly/Materials/WeeklyReportTextHelper.cs').read_text(encoding='utf-8-sig')
assert 'private static string EmptySectionText =>' in ui_source, 'Resolve UI language each display, not static Chinese cache'
assert 'split.NormalizedBodyText.Trim()' in ui_source, 'Custom original body preserved'
print('PASS 2 Weekly UI fallback pairs, original body preservation, per-display language resolution; game language switching NOT RUN')
