"""Static tag-catalog UI integration and protocol/cache inverse; C#/game NOT-RUN."""
from pathlib import Path
import importlib.util
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from terminal_category_contract_support import restore_terminal_categories

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "63591ee19"
PREFIX = "src/AF.GameAdapter.Bannerlord/UI/Terminal/"
spec = importlib.util.spec_from_file_location("inventory", ROOT / "tools/localization/inventory_untranslated.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)
LEX, decode = audit.LEX, audit.decode


def old(relative):
    return subprocess.check_output(["git", "show", BASELINE + ":" + PREFIX + relative], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")


def restore(source, pairs):
    for before, after in pairs:
        assert source.count(after) == 1, after
        source = source.replace(after, before, 1)
    return source


catalog_pairs = [
    ('error = "当前没有可导出的标签。";', 'error = TagCatalogUiText.ExportEmpty;'),
    ('error = "没有找到可写入的 AnimusForge 模块目录。";', 'error = TagCatalogUiText.ExportRootMissing;'),
    ('error = "导出文件名冲突过多，请稍后再试。";', 'error = TagCatalogUiText.ExportNameConflict;'),
    ('stringBuilder.AppendLine("AnimusForge 标签列表");', 'stringBuilder.AppendLine(TagCatalogUiText.ExportTitle);'),
    ('stringBuilder.AppendLine("导出时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));', 'stringBuilder.AppendLine(TagCatalogUiText.ExportTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)));'),
    ('stringBuilder.AppendLine("模块目录：" + (moduleRoot ?? ""));', 'stringBuilder.AppendLine(TagCatalogUiText.ExportRoot(moduleRoot ?? ""));'),
    ('stringBuilder.AppendLine("标签总数：" + snapshot.Entries.Count.ToString(CultureInfo.InvariantCulture));', 'stringBuilder.AppendLine(TagCatalogUiText.ExportTotal(snapshot.Entries.Count.ToString(CultureInfo.InvariantCulture)));'),
    ('stringBuilder.AppendLine("正文/历史：" + bodyCount.ToString(CultureInfo.InvariantCulture));', 'stringBuilder.AppendLine(TagCatalogUiText.ExportBody(bodyCount.ToString(CultureInfo.InvariantCulture)));'),
    ('stringBuilder.AppendLine("后处理：" + postprocessCount.ToString(CultureInfo.InvariantCulture));', 'stringBuilder.AppendLine(TagCatalogUiText.ExportPostprocess(postprocessCount.ToString(CultureInfo.InvariantCulture)));'),
    ('stringBuilder.AppendLine("扫描文件：" + snapshot.ScannedFileCount.ToString(CultureInfo.InvariantCulture));', 'stringBuilder.AppendLine(TagCatalogUiText.ExportFiles(snapshot.ScannedFileCount.ToString(CultureInfo.InvariantCulture)));'),
    ('stringBuilder.AppendLine("扫描来源：");', 'stringBuilder.AppendLine(TagCatalogUiText.ExportSources);'),
    ('stringBuilder.AppendLine("说明：此文件由游戏内 U 键 AnimusForge 终端的一键导出生成。列表来自当前模块文件、当前程序集字符串和内置运行时规则。");', 'stringBuilder.AppendLine(TagCatalogUiText.ExportExplanation);'),
    ('stringBuilder.AppendLine("==== " + group.Key + " ====");', 'stringBuilder.AppendLine("==== " + TagCatalogUiText.Category(group.Key) + " ====");'),
    ('stringBuilder.AppendLine(entry.Tag ?? "");', 'stringBuilder.AppendLine(TagCatalogUiText.Tag(entry.Tag));'),
    ('stringBuilder.AppendLine("说明：" + entry.Description.Trim());', 'stringBuilder.AppendLine(TagCatalogUiText.DescriptionLabel + " " + TagCatalogUiText.Description(entry.Description));'),
    ('stringBuilder.AppendLine("来源：");', 'stringBuilder.AppendLine(TagCatalogUiText.SourcesLabel);'),
    ('stringBuilder.AppendLine("- " + source);', 'stringBuilder.AppendLine("- " + TagCatalogUiText.Source(source));'),
]

vm_pairs = [
    ('SearchTerms = (entry.Description ?? "") + " " + string.Join(" ", entry.Sources),', 'SearchTerms = TagCatalogUiText.SearchTerms(entry),'),
    ('BreadcrumbText = "Terminal / " + _selectedTab + " / " + new TextObject("{=AF_UI_Terminal_DiagnosticsTitle}System error analysis and diagnostics").ToString();', 'BreadcrumbText = new TextObject("{=AF_UI_Terminal_BreadcrumbRoot}Terminal").ToString() + " / " + LocalizeTab(_selectedTab) + " / " + new TextObject("{=AF_UI_Terminal_DiagnosticsTitle}System error analysis and diagnostics").ToString();'),
    ('return "[" + (entry.Category ?? "标签") + "] " + (entry.Tag ?? "");', 'return "[" + TagCatalogUiText.Category(entry.Category) + "] " + TagCatalogUiText.Tag(entry.Tag);'),
    ('string description = CompactOneLine(entry.Description);', 'string description = CompactOneLine(TagCatalogUiText.Description(entry.Description));'),
    ('string source = (entry.Sources != null && entry.Sources.Count > 0) ? entry.Sources[0] : "";', 'string source = (entry.Sources != null && entry.Sources.Count > 0) ? TagCatalogUiText.Source(entry.Sources[0]) : "";'),
    ('stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_TagLabel}Tag:").ToString() + " " + (entry.Tag ?? ""));', 'stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_TagLabel}Tag:").ToString() + " " + TagCatalogUiText.Tag(entry.Tag));'),
    ('stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_CategoryLabel}Category:").ToString() + " " + (entry.Category ?? new TextObject("{=AF_UI_Terminal_Tag}Tag").ToString()));', 'stringBuilder.AppendLine(new TextObject("{=AF_UI_Terminal_CategoryLabel}Category:").ToString() + " " + TagCatalogUiText.Category(entry.Category));'),
    ('stringBuilder.AppendLine(entry.Description.Trim());', 'stringBuilder.AppendLine(TagCatalogUiText.Description(entry.Description));'),
    ('stringBuilder.AppendLine(source);', 'stringBuilder.AppendLine(TagCatalogUiText.Source(source));'),
]



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

def main():
    catalog = (ROOT / PREFIX / "AnimusForgeTagCatalog.cs").read_text(encoding="utf-8-sig")
    vm = (ROOT / PREFIX / "AnimusForgeTerminalUiModels.cs").read_text(encoding="utf-8-sig")
    raw = json.loads((ROOT / 'content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCaptureCompatibility.json').read_text(encoding='utf-8-sig'))
    restored_catalog = restore(catalog, catalog_pairs)
    restored_catalog = re.sub(r'AdapterPromptCaptureTexts.PcapCompat\("([^"\n]+)"\)', lambda match: json.dumps(raw[match[1]], ensure_ascii=False), restored_catalog)
    assert code_tokens(restored_catalog) == code_tokens(old("AnimusForgeTagCatalog.cs")), "Snapshot/scan/cache/ID/tag/category/group/sort/log/export-file behavior changed"
    categories = json.loads((ROOT / "content/foundation/AF.Foundation.Localization/ModuleData/TerminalCategories.json").read_text(encoding="utf-8"))["Categories"]
    vm = vm.replace(', TerminalCategories.Ui };', ' };')
    original_vm = old("AnimusForgeTerminalUiModels.cs")
    # Report filter labels were moved to their actual nested datasource to fix
    # blank controls; preserve the remaining input/lifecycle/tag/search oracle.
    for suffix in range(8,16):
        original_vm = re.sub(r'\[DataSourceProperty\]\s*public string AF_UI_Terminal_Prefab_' + f'{suffix:03}' + r'Text =>[^\n]+;', '', original_vm)
    assert restore_reviewed_display_bindings(restore(restore_terminal_categories(vm, categories), vm_pairs)) == code_tokens(original_vm), "VM input/lifecycle/search/original routing changed"
    helper = (ROOT / PREFIX / "TagCatalogUiText.cs").read_text(encoding="utf-8-sig")
    assert 'return raw;' in helper and 'return description;' in helper
    assert ': raw ?? string.Empty;' in helper and 'return raw != null && raw.StartsWith(prefix, StringComparison.Ordinal)' in helper
    assert not re.search(r'\b(?:entry|snapshot)\.\w+\s*=(?!=)', helper), "Projection writes raw snapshot"
    assert 'private static readonly HashSet<string> VillageActions' in helper and 'VillageActions.Contains(action)' in helper
    search = helper.split('internal static string SearchTerms(', 1)[1].split('internal static string ExportEmpty', 1)[0]
    for preserved in ('entry.Description', 'string.Join(" ", entry.Sources)', 'Append(entry.Tag)', 'Append(entry.Category)', 'Description(entry.Description)', 'Append(Source(source))'):
        assert preserved in search, "Search alias lost: " + preserved
    assert 'Enum.IsDefined' not in helper, "Repeated enum reflection in display path"
    # Only the known natural-language pseudo-tag is projected; all executable protocol tags pass through.
    tag = helper.split('internal static string Tag(', 1)[1].split('internal static string Description(', 1)[0]
    assert raw['TagCat_NaturalLanguage'] == '正文（自然语言）'
    assert 'raw == AdapterPromptCaptureTexts.PcapCompat("TagCat_NaturalLanguage")' in tag and ': raw ?? string.Empty;' in tag
    lang = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages"
    en = {n.get("id"): n.get("text") for n in ET.parse(lang / "af_ui_strings.xml").iter("string")}
    zh = {n.get("id"): n.get("text") for n in ET.parse(lang / "CNs/af_ui_strings-zh-CN.xml").iter("string")}
    calls = re.findall(r'(?:Text|Format)\("(AF_UI_[^"]+)",\s*("(?:[^"\\]|\\.)*")', helper)
    keys = set()
    for key, fallback in calls:
        value = audit.decode(fallback)
        assert en[key] == value and zh[key].strip(), key
        assert not re.search(r'[\u4e00-\u9fff]', value), key
        assert sorted(re.findall(r'\{[A-Z_]+\}', value)) == sorted(re.findall(r'\{[A-Z_]+\}', zh[key])), key
        keys.add(key)
    assert len({key for key in keys if key.startswith('AF_UI_TagCatalog_')}) == 42
    village_actions = (
        "GatherElders", "RestrainTroops", "Pacify", "Relief", "Fine", "RequisitionFood", "RequisitionProduce",
        "RequisitionLivestock", "LevyRecruits", "PunishRingleader", "ConfiscateProperty", "DestroyLivelihood", "Massacre", "CulturalReform",
    )
    for action in village_actions:
        assert f'case "{action}": return Text("AF_UI_TagCatalog_VillageAction{action}",' in helper
    built_ins = catalog.split('private static void AddBuiltInEntries', 1)[1].split('private static void AddTag', 1)[0]
    builtin_descriptions = (
        ("不是动作标签。标签测试输入框里除动作标签外的文本会作为 NPC 可见正文显示或写入历史。", "AF_UI_TagCatalog_DescriptionBody"),
        ("内部正文分隔标记。通常不是玩家手写动作标签。", "AF_UI_TagCatalog_DescriptionContent"),
        ("玩家行为事实写入标记，用于告诉 LLM 已实际发生的玩家行为。", "AF_UI_TagCatalog_DescriptionPlayerFact"),
        ("NPC行为事实写入标记，用于告诉 LLM 已实际发生的 NPC 行为。", "AF_UI_TagCatalog_DescriptionNpcFact"),
    )
    description_method = helper.split('internal static string Description(', 1)[1].split('private static string VillageActionDisplay', 1)[0]
    for description, key in builtin_descriptions:
        raw_key = {'AF_UI_TagCatalog_DescriptionBody': 'TagCat_DescBody', 'AF_UI_TagCatalog_DescriptionContent': 'TagCat_DescContent', 'AF_UI_TagCatalog_DescriptionPlayerFact': 'TagCat_DescPlayerFact', 'AF_UI_TagCatalog_DescriptionNpcFact': 'TagCat_DescNpcFact'}[key]
        assert raw[raw_key] == description
        assert 'AdapterPromptCaptureTexts.PcapCompat("' + raw_key + '")' in built_ins
        assert 'if (description == AdapterPromptCaptureTexts.PcapCompat("' + raw_key + '"))' in description_method and key in description_method
    assert 'Title = BuildTagCatalogEntryTitle(entry)' in vm and 'Hint = BuildTagCatalogEntryHint(entry)' in vm
    assert 'BuildTagCatalogEntryDetailText(entry)' in vm and 'BuildExportText(snapshot, moduleRoot)' in catalog
    print("PASS terminal tag static integration: 42 new keys + 3 reused; raw protocol/snapshot/cache/scan/sort/export mechanics exact inverse; raw and English search aliases present; unknown values retained; C#/game NOT-RUN")


if __name__ == "__main__":
    main()
