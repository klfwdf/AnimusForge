"""Static contract for the weekly editor display projection."""
import re
import json
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Editors/WeeklyEditorProjection.cs"
EN = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml"
ZH = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml"

source = SOURCE.read_text(encoding="utf-8-sig")
composition = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyEditorUi.cs").read_text(encoding="utf-8-sig")
controller = (SOURCE.parent / "WeeklyReportEditorController.cs").read_text(encoding="utf-8-sig")
diagnostic = (ROOT / "src/AF.GameAdapter.Bannerlord/Records/WeeklyDiagnosticPreviewProjection.cs").read_text(encoding="utf-8-sig")
english = {node.get("id"): node.get("text") for node in ET.parse(EN).iter("string")}
chinese = {node.get("id"): node.get("text") for node in ET.parse(ZH).iter("string")}
for path in (EN, ZH):
    rows = list(ET.parse(path).iter("string"))
    assert len(rows) == len({row.get("id") for row in rows}), path
ids = set(re.findall(r'T\("(AF_UI_WeeklyProjection_[A-Za-z0-9_]+)"', source))
assert ids
assert ids <= english.keys(), sorted(ids - english.keys())
assert ids <= chinese.keys(), sorted(ids - chinese.keys())
for key in ids:
    en = english[key]
    zh = chinese[key]
    assert en and zh
    assert re.findall(r"\{([A-Z][A-Z0-9_]*)\}", en) == re.findall(r"\{([A-Z][A-Z0-9_]*)\}", zh), key
assert "WeeklyReportArchivePolicy" in source
assert 'string date = recent ? BuildRecentDateForDisplay(port, x)' in source
assert ' + " · " + date;' in source and "CreatedDate = date," in source
date_projection = source.split("internal static string BuildRecentDateForDisplay", 1)[1].split("internal static string BuildWeeklyReportBrowserDefaultTitle", 1)[0]
assert "materials.Min(material => material.ActionDay.Value)" in date_projection
assert "materials.Max(material => material.ActionDay.Value)" in date_projection
assert "materials.All(material => material.ActionDay.HasValue && material.ActionDay.Value >= 0" in date_projection
assert 'IndexOf(" — ", StringComparison.Ordinal) < 0' in date_projection  # Nested historical ranges retain their archived extent.
assert 'return firstDay == lastDay ? first : first + " — " + last;' in date_projection
assert "entry.CreatedDate.Trim()" in date_projection
assert "entry.CreatedDay" not in date_projection  # A single end day cannot replace a range.
assert not re.search(r"entry\.[A-Za-z]+\s*=(?!=)", date_projection)
assert "BodyText = body.Length == 0 ? T(" in source
assert "systemPrompt ?? \"\"" in source and "userPrompt ?? \"\"" in source
weekly_en = ROOT / "content/modules/AF.Module.Weekly/ModuleData/Languages/afweekly_ui_strings.xml"
weekly_values = {node.get("id"): node.get("text") for node in ET.parse(weekly_en).iter("string")}
for key, literal in re.findall(r'T\("(AF_[A-Za-z0-9_]+)",\s*"((?:\\.|[^"\\])*)"', source):
    assert (english | weekly_values)[key] == json.loads('"' + literal + '"'), key
assert not re.search(r"[\u3400-\u9fff]|\\u(?:[3-9a-fA-F][0-9a-fA-F]{3})", source)

date_policy_path = ROOT / "src/modules/AF.Module.Weekly/Panel/WeeklyReportArchivePolicy.cs"
date_policy = date_policy_path.read_text(encoding="utf-8-sig")
date_projection = date_policy.split("internal static string ProjectDateForDisplay", 1)[1].split("internal static void RefreshRecentText", 1)[0]
assert 'Localize("AF_UI_Timeline_UnknownDate", "Unknown date")' in date_projection
assert 'Localize("AF_UI_Timeline_Day", "Day {DAY}"' in date_projection
assert 'const string separator = " — "' in date_projection and "return rawDate;" in date_projection
assert "CreatedDay" not in date_projection and "CampaignTime" not in date_projection

def simulate_date(raw, language):
    if raw is None or not str(raw).strip():
        return language["AF_UI_Timeline_UnknownDate"]
    value = str(raw).strip()
    if value in ("Unknown date", "未知日期"):
        return language["AF_UI_Timeline_UnknownDate"]

    def project_day(candidate):
        if candidate.startswith("Day "):
            number = candidate[4:]
        elif candidate.startswith("第 ") and candidate.endswith(" 日"):
            number = candidate[2:-2]
        elif candidate.startswith("第") and candidate.endswith("天"):
            number = candidate[1:-1]
        else:
            return None
        if not re.fullmatch(r"[0-9]+", number):
            return None
        day = int(number)
        if day > 2147483647 or str(day) != number:
            return None
        return language["AF_UI_Timeline_Day"].replace("{DAY}", str(day))

    projected = project_day(value)
    if projected is not None:
        return projected
    separator = " — "
    if value.count(separator) == 1:
        first, last = value.split(separator)
        first_text, last_text = project_day(first), project_day(last)
        if first_text is not None and last_text is not None:
            return first_text + separator + last_text
    return raw


def day_label(day, language):
    return language["AF_UI_Timeline_Day"].replace("{DAY}", str(day))


date_cases = [
    ("Day 14", english, day_label(14, english)),
    ("Day 14", chinese, day_label(14, chinese)),
    ("第 14 日", english, day_label(14, english)),
    ("第 14 日", chinese, day_label(14, chinese)),
    ("第14天", english, day_label(14, english)),
    ("第14天", chinese, day_label(14, chinese)),
    ("第 0 日", english, day_label(0, english)),
    ("Day 0", chinese, day_label(0, chinese)),
    ("Day 2 — Day 9", chinese, day_label(2, chinese) + " — " + day_label(9, chinese)),
    ("第 2 日 — 第9天", english, day_label(2, english) + " — " + day_label(9, english)),
    ("Day 2 — 第 9 日", english, day_label(2, english) + " — " + day_label(9, english)),
    ("", english, english["AF_UI_Timeline_UnknownDate"]),
    ("   ", chinese, chinese["AF_UI_Timeline_UnknownDate"]),
    ("Unknown date", chinese, chinese["AF_UI_Timeline_UnknownDate"]),
    ("未知日期", english, english["AF_UI_Timeline_UnknownDate"]),
    ("custom date", english, "custom date"),
    ("1084年1月10日 — 1084年1月15日", english, "1084年1月10日 — 1084年1月15日"),
    ("report: 第 4 日", english, "report: 第 4 日"),
    ("Day 2 — custom endpoint", english, "Day 2 — custom endpoint"),
]
for raw, language, expected in date_cases:
    assert simulate_date(raw, language) == expected, (raw, language)

memory_date = (ROOT / "src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs").read_text(encoding="utf-8-sig")
assert "Math.Floor(CampaignTime.Now.ToDays)" in memory_date
safe_date = memory_date.split("internal static string GetCurrentGameDateTextSafe()", 1)[1].split("internal static Vec2?", 1)[0]
scene_raw = json.loads((ROOT / "content/foundation/AF.Foundation.Localization/ModuleData/AdapterSceneCompatibility.json").read_text(encoding="utf-8"))
assert scene_raw["DatePrefix"] == "第 " and scene_raw["DateSuffix"] == " 日"
safe_date = re.sub(r'AdapterSceneTexts.Raw\("(DatePrefix|DateSuffix)"\)', lambda match: json.dumps(scene_raw[match[1]], ensure_ascii=False), safe_date)
assert "CampaignTime.Now.ToString()" in safe_date and '"第 " + GetCurrentGameDayIndexSafe() + " 日"' in safe_date
week_zero = (ROOT / "src/AF.GameAdapter.Bannerlord/Weekly/WeekZeroOpeningSummaryGenerationController.cs").read_text(encoding="utf-8-sig")
prompt_raw = json.loads((ROOT / 'content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json').read_text(encoding='utf-8-sig'))
assert prompt_raw['weekly_memory_compat_e06eabeb34b7'] == '第 0 日', 'original archived week-zero date unchanged'
assert "eventRecordEntry.CreatedDay = 0;" in week_zero and 'eventRecordEntry.CreatedDate = AnimusForge.PromptComposerTexts.Raw("weekly_memory_compat_e06eabeb34b7");' in week_zero
weekly_runtime = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyRuntime.cs").read_text(encoding="utf-8-sig")
assert "CurrentDay = GetCurrentGameDayIndexSafe, CurrentDate = GetCurrentGameDateTextSafe" in weekly_runtime
assert 'Localize("AF_UI_Weekly_Browser_DayNumber", "Day {DAY}"' in date_policy
assert 'entry.CreatedDate=first == last ? first : first + " — " + last;' in date_policy
assert 'entry.CreatedDay=materials.Max(m => m.ActionDay ?? 0);' in date_policy
weekly_import = (ROOT / "src/modules/AF.Module.Weekly/ImportExport/WeeklyEventDataImportOwner.cs").read_text(encoding="utf-8-sig")
assert 'CreatedDate = (item.CreatedDate ?? "").Trim()' in weekly_import and 'CreatedDay = Math.Max(0, item.CreatedDay)' in weekly_import
kingdom_news = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Weekly/KingdomNewsArchiveSource.cs").read_text(encoding="utf-8-sig")
assert 'CreatedDate = FirstNonEmpty(document.GameDate, FormatDay(day))' in kingdom_news
assert 'CreatedDate = FirstNonEmpty(policy.DateText, FormatDay(day))' in kingdom_news
assert 'if (day <= 0) return Localize("AF_UI_Timeline_UnknownDate", "Unknown date")' in kingdom_news
assert 'new TextObject("{=AF_UI_Timeline_Day}Day {DAY}")' in kingdom_news
terminal_vm = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.cs").read_text(encoding="utf-8-sig")
terminal_archive = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.Archive.cs").read_text(encoding="utf-8-sig")
bulletin_presentation = (ROOT / "src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.Presentation.cs").read_text(encoding="utf-8-sig")
terminal_prefab = (ROOT / "content/modules/AF.Module.UI/GUI/Prefabs/AnimusForgeTerminalPopup.xml").read_text(encoding="utf-8-sig")
bulletin_prefab = (ROOT / "content/modules/AF.Module.Weekly/GUI/Prefabs/WorldBulletinPanel.xml").read_text(encoding="utf-8-sig")
assert 'DateText = WeeklyReportArchivePolicy.ProjectDateForDisplay(entry?.CreatedDate);' in terminal_vm
assert 'ReaderMetaText => (_readerItem?.WeekText ?? "") + " · " + (_readerItem?.DateText ?? "")' in terminal_archive
assert 'OrderByDescending(x => x.CreatedDay)' in terminal_vm
assert terminal_prefab.count('Text="@DateText"') >= 1 and 'Text="@ReaderMetaText"' in terminal_prefab
assert bulletin_presentation.count('WeeklyReportArchivePolicy.ProjectDateForDisplay(entry?.CreatedDate)') == 2
assert 'Text="@MastheadText"' in bulletin_prefab and 'Text="@MetaText"' in bulletin_prefab

# Compare saved diagnostic algorithms with the fixed pre-localization revision.
baseline = subprocess.check_output(
    ["git", "show", "76e592ce0:src/AF.GameAdapter.Bannerlord/UI/Editors/WeeklyEditorProjection.cs"],
    cwd=ROOT).decode("utf-8")
def method_body(text, name):
    start = text.index("internal static string " + name + "(")
    start = text.index("\n\t{", start)
    end = text.index("\n\t}", start) + len("\n\t}")
    return text[start:end].replace("\r\n", "\n")

record_raw = json.loads((ROOT / "content/foundation/AF.Foundation.Localization/ModuleData/AdapterRecordCompatibility.json").read_text(encoding="utf-8"))
diagnostic = re.sub(r'AdapterRecordTexts.Raw\("([^"\n]+)"\)', lambda match: json.dumps(record_raw[match[1]], ensure_ascii=False), diagnostic)
for old, new in (("BuildWeeklyReportPromptPreviewText", "BuildSingle"),
                 ("BuildWeeklyBatchPromptPreviewText", "BuildBatch")):
    assert method_body(baseline, old) == method_body(diagnostic, new), old
    assert f"{old}ForDisplay(_port," in controller, old
    assert f"{old}(WeeklyEditorDiagnostic," in composition, old
    assert f"{old}(WeeklyEditorDisplay," not in composition, old
assert "TextObject" not in diagnostic and "T(" not in diagnostic
raw_port = composition.split("WeeklyEditorDiagnostic = new WeeklyEditorDisplayPort", 1)[1].split("};", 1)[0]
assert "PromptProfileLabel = () => GetWeeklyReportPromptProfile().Label" in raw_port
assert "AppendDevNpcActionField = WeeklyDiagnosticPreviewProjection.AppendField" in raw_port
assert "ProfileForDisplay" not in raw_port
assert 'stringBuilder.AppendLine(label + "：" + value.Trim());' in diagnostic
assert composition.count("PromptProfileLabel = () => WeeklyEditorProjection.ProfileForDisplay(") == 2
assert composition.count("TranslateEventMaterialTypeForDev = WeeklyEditorProjection.MaterialTypeForDisplay") == 2
runtime = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyRuntime.cs").read_text(encoding="utf-8-sig")
assert "FullPreview = BuildWeeklyReportPromptPreviewText," in runtime
assert "BatchPreview = BuildWeeklyBatchPromptPreviewText," in runtime
assert "ForDisplay" not in runtime
print(f"PASS weekly projection localization keys={len(ids)}, date cases={len(date_cases)}, source/consumer boundaries, fallback equality and original saved diagnostics")
