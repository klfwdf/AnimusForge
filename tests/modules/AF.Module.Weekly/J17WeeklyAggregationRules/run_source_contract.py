"""Bounded source witness for Weekly's synchronous game-fact preparation path."""

from pathlib import Path
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import current_source_path
import re


ROOT = Path(__file__).resolve().parents[4]
HOST = (current_source_path(ROOT, "MyBehavior.cs")).read_text(encoding="utf-8-sig")
AGGREGATION = (ROOT / "src/modules/AF.Module.Weekly/Materials/WeeklyMaterialAggregationOwner.cs").read_text(encoding="utf-8-sig")
PROMPT = (ROOT / "src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.cs").read_text(encoding="utf-8-sig")
LEGACY_DTOS = (ROOT / "src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs").read_text(encoding="utf-8-sig")
EVENT_LINE = (ROOT / "src/modules/AF.Module.Weekly/Materials/WeeklyAggregateEventLineOwner.cs").read_text(encoding="utf-8-sig")
DAILY = (ROOT / "src/AF.GameAdapter.Bannerlord/Weekly/CampaignDailyMaintenanceController.cs").read_text(encoding="utf-8-sig")
RUNTIME = (ROOT / "src/modules/AF.Module.Weekly/Generation/WeeklyReportRuntimeOwner.cs").read_text(encoding="utf-8-sig")
RECORD = (ROOT / "src/AF.GameAdapter.Bannerlord/Records/CampaignCharacterRecordCaptureAdapter.cs").read_text(encoding="utf-8-sig")
IDENTITY = (ROOT / "src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs").read_text(encoding="utf-8-sig")
RUNTIME_ROOT = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyRuntime.cs").read_text(encoding="utf-8-sig")


def body(source: str, name: str, signature: str = "") -> str:
    matches = [
        match
        for match in re.finditer(r"(?m)^[ \t]*(?:private|internal|public)\s+(?:static\s+)?[^\n]+\b" + re.escape(name) + r"\(", source)
        if signature in source[match.start() : source.find("\n", match.start())]
    ]
    assert len(matches) == 1, (name, signature, len(matches))
    line_end = source.find("\n", matches[0].end())
    arrow = source.find("=>", matches[0].start(), line_end)
    if arrow >= 0:
        return source[arrow:source.index(";", arrow) + 1]
    start = source.index("{", matches[0].end())
    depth = 0
    quoted = False
    escaped = False
    for index in range(start, len(source)):
        char = source[index]
        if quoted:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                quoted = False
            continue
        if char == '"':
            quoted = True
        elif char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]
    raise AssertionError(f"unterminated {name}")


checks = 0
for nested_name in ("WeeklyReportOutputMode", "EventMaterialReference", "WeeklyEventMaterialPreviewGroup", "WeeklyReportBatchRequest"):
    assert re.search(r"\b(?:enum|class)\s+" + nested_name + r"\b", LEGACY_DTOS), nested_name
    assert not re.search(r"\b(?:enum|class)\s+" + nested_name + r"\b", HOST), nested_name
    checks += 1


def includes(source: str, name: str, callee: str, signature: str = "") -> None:
    global checks
    assert callee in body(source, name, signature), (name, callee)
    checks += 1


def synchronous(source: str, name: str, signature: str = "") -> None:
    global checks
    value = body(source, name, signature)
    assert not re.search(r"\b(?:await|Task\.Run|Task\.Factory|ContinueWith)\b", value), name
    checks += 1


assert "CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick)" in HOST
checks += 1
includes(HOST, "OnCampaignTick", "RunCampaignMemoryMaintenanceCycle(")
includes(HOST, "RunCampaignMemoryMaintenanceCycle", "_dailyMaintenanceController.RunCampaignMemoryMaintenanceCycle(")
includes(DAILY, "RunCampaignMemoryMaintenanceCycle", "ProcessDeferredDailyMaintenance(")
includes(HOST, "ProcessDeferredDailyMaintenance", "_dailyMaintenanceController.ProcessDeferredDailyMaintenance(")
includes(DAILY, "ProcessDeferredDailyMaintenance", "_port.ProcessPendingWeekly(")
assert "ProcessPendingWeekly = (start,budget) => WeeklyRuntime.ProcessPendingAutoWeeklyReportBuildBudget(start,budget)" in HOST
checks += 1
includes(RUNTIME, "ProcessPendingAutoWeeklyReportBuildBudget", "ProcessPendingWeeklyReportAggregationBudget(")
includes(RUNTIME, "ProcessPendingAutoWeeklyReportBuildBudget", "ProcessPendingWeeklyReportPromptMaterialsBudget(")
includes(RUNTIME, "ProcessPendingWeeklyReportAggregationBudget", "_port.AggregateMaterials(")
assert "AggregateMaterials = ApplyWeeklyPromptMaterialAggregation," in RUNTIME_ROOT
checks += 1
includes(RUNTIME, "ProcessPendingWeeklyReportPromptMaterialsBudget", "WeeklyPromptMaterialOwner.Prepare(")
includes(HOST, "BuildWeeklyEventMaterialPreviewGroups", "_campaignCharacterRecordCapture.BuildWeeklyEventMaterialPreviewGroups(", "int startDay")
includes(RECORD, "BuildWeeklyEventMaterialPreviewGroups", "_aggregateWeeklyMaterials(", "int startDay")
assert "() => _weeklyEventRecords, ApplyWeeklyPromptMaterialAggregation, () => MemoryQueueState" in HOST
checks += 1
includes(HOST, "ApplyWeeklyPromptMaterialAggregation", "WeeklyMaterialAggregationOwner.Apply(")
includes(HOST, "BuildWeeklyPromptAggregateCategoryMaterial", "WeeklyMaterialAggregationOwner.BuildCategoryMaterial(")
includes(HOST, "BuildWeeklyPromptAggregateEventLine", "WeeklyAggregateEventLine.Render(")
includes(HOST, "BuildWeeklyPromptResolvedVillageRaidMaterial", "WeeklyPromptMaterialOwner.BuildResolvedVillageRaidMaterial(")
includes(AGGREGATION, "Apply", "BuildWeeklyPromptAggregateEventKey(")
includes(AGGREGATION, "Apply", "GetWeeklyPromptAggregateCategoryOrder(")
includes(PROMPT, "BuildResolvedVillageRaidMaterial", "resolveSettlementDisplay(")
includes(PROMPT, "BuildShort", "BuildWeeklyPromptShortMaterial(")
includes(EVENT_LINE, "Render", "WeeklyMaterialAggregationOwner.ResolveWeeklyPromptAggregateCategory(")
includes(IDENTITY, "CaptureWeeklyHeroFact", "FindHeroById(")
includes(IDENTITY, "CaptureWeeklySettlementName", "Settlement.Find(")
includes(IDENTITY, "CaptureWeeklySettlementNameWithType", "GetSettlementTypeLabel(")
assert "FindHeroById(" not in EVENT_LINE and "Settlement.Find(" not in EVENT_LINE
checks += 1
assert "await " not in EVENT_LINE and "Task.Run" not in EVENT_LINE
checks += 1
assert "MyBehavior.BuildWeeklyPromptShortMaterial" not in PROMPT
checks += 1
assert "MyBehavior.BuildWeeklyPromptShortSettlementStatsMaterial" not in PROMPT
assert "MyBehavior.BuildWeeklyPromptShortVillageRaidMaterial" not in PROMPT
checks += 2

for name, signature in [
    ("ProcessDeferredDailyMaintenance", ""),
    ("ProcessPendingAutoWeeklyReportBuildBudget", ""),
    ("ProcessPendingWeeklyReportAggregationBudget", ""),
    ("ProcessPendingWeeklyReportPromptMaterialsBudget", ""),
    ("BuildWeeklyEventMaterialPreviewGroups", "int startDay"),
    ("ApplyWeeklyPromptMaterialAggregation", ""),
    ("BuildWeeklyPromptAggregateCategoryMaterial", ""),
    ("BuildWeeklyPromptAggregateEventLine", ""),
    ("CaptureWeeklyHeroFact", ""),
    ("CaptureWeeklySettlementName", ""),
    ("CaptureWeeklySettlementNameWithType", ""),
    ("BuildWeeklyPromptResolvedVillageRaidMaterial", ""),
]:
    source = (DAILY if name == "ProcessDeferredDailyMaintenance" else
              RUNTIME if name.startswith("ProcessPending") else
              RECORD if name == "BuildWeeklyEventMaterialPreviewGroups" else
              IDENTITY if name.startswith("CaptureWeekly") else HOST)
    synchronous(HOST, name, signature)
    if source != HOST:
        synchronous(source, name, signature)

print(f"PASS J17WeeklyMainThreadSourceContract {checks} checks (source witness, live game NOT_RUN)")
