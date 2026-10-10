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


def main():
    catalog = (ROOT / PREFIX / "AnimusForgeTagCatalog.cs").read_text(encoding="utf-8-sig")
    vm = (ROOT / PREFIX / "AnimusForgeTerminalUiModels.cs").read_text(encoding="utf-8-sig")
    assert restore(catalog, catalog_pairs) == old("AnimusForgeTagCatalog.cs"), "Snapshot/scan/cache/ID/tag/category/group/sort/log/export-file behavior changed"
    categories = json.loads((ROOT / "content/foundation/AF.Foundation.Localization/ModuleData/TerminalCategories.json").read_text(encoding="utf-8"))["Categories"]
    assert restore(restore_terminal_categories(vm, categories), vm_pairs) == old("AnimusForgeTerminalUiModels.cs"), "VM input/lifecycle/search/original routing changed"
    helper = (ROOT / PREFIX / "TagCatalogUiText.cs").read_text(encoding="utf-8-sig")
    assert 'default: return raw;' in helper and 'return description;' in helper
    assert ': raw ?? string.Empty;' in helper and 'return raw != null && raw.StartsWith(prefix, StringComparison.Ordinal)' in helper
    assert not re.search(r'\b(?:entry|snapshot)\.\w+\s*=(?!=)', helper), "Projection writes raw snapshot"
    assert 'private static readonly HashSet<string> VillageActions' in helper and 'VillageActions.Contains(action)' in helper
    search = helper.split('internal static string SearchTerms(', 1)[1].split('internal static string ExportEmpty', 1)[0]
    for preserved in ('entry.Description', 'string.Join(" ", entry.Sources)', 'Append(entry.Tag)', 'Append(entry.Category)', 'Description(entry.Description)', 'Append(Source(source))'):
        assert preserved in search, "Search alias lost: " + preserved
    assert 'Enum.IsDefined' not in helper, "Repeated enum reflection in display path"
    # Only the known natural-language pseudo-tag is projected; all executable protocol tags pass through.
    tag = helper.split('internal static string Tag(', 1)[1].split('internal static string Description(', 1)[0]
    assert 'raw == "正文（自然语言）"' in tag and ': raw ?? string.Empty;' in tag
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
        assert description in built_ins and f'case "{description}":' in description_method and key in description_method
    assert 'Title = BuildTagCatalogEntryTitle(entry)' in vm and 'Hint = BuildTagCatalogEntryHint(entry)' in vm
    assert 'BuildTagCatalogEntryDetailText(entry)' in vm and 'BuildExportText(snapshot, moduleRoot)' in catalog
    print("PASS terminal tag static integration: 42 new keys + 3 reused; raw protocol/snapshot/cache/scan/sort/export mechanics exact inverse; raw and English search aliases present; unknown values retained; C#/game NOT-RUN")


if __name__ == "__main__":
    main()
