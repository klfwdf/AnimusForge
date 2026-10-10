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

# Compare saved diagnostic algorithms with the fixed pre-localization revision.
baseline = subprocess.check_output(
    ["git", "show", "76e592ce0:src/AF.GameAdapter.Bannerlord/UI/Editors/WeeklyEditorProjection.cs"],
    cwd=ROOT).decode("utf-8")
def method_body(text, name):
    start = text.index("internal static string " + name + "(")
    start = text.index("\n\t{", start)
    end = text.index("\n\t}", start) + len("\n\t}")
    return text[start:end].replace("\r\n", "\n")

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
print(f"PASS weekly projection localization keys={len(ids)}, fallback equality, UI/runtime separation and original saved diagnostics")
