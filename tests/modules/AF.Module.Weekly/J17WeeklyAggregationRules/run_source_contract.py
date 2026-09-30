"""Bounded source witness for Weekly's synchronous game-fact preparation path."""

from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[4]
HOST = (ROOT / "MyBehavior.cs").read_text(encoding="utf-8-sig")
AGGREGATION = (ROOT / "src/modules/AF.Module.Weekly/Materials/WeeklyMaterialAggregationOwner.cs").read_text(encoding="utf-8-sig")
PROMPT = (ROOT / "src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.cs").read_text(encoding="utf-8-sig")
LEGACY_DTOS = (ROOT / "src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs").read_text(encoding="utf-8-sig")
EVENT_LINE = (ROOT / "src/modules/AF.Module.Weekly/Materials/WeeklyAggregateEventLineOwner.cs").read_text(encoding="utf-8-sig")


def body(source: str, name: str, signature: str = "") -> str:
    matches = [
        match
        for match in re.finditer(r"(?m)^[ \t]*(?:private|internal|public)\s+(?:static\s+)?[^\n]+\b" + re.escape(name) + r"\(", source)
        if signature in source[match.start() : source.find("\n", match.start())]
    ]
    assert len(matches) == 1, (name, signature, len(matches))
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
includes(HOST, "RunCampaignMemoryMaintenanceCycle", "ProcessDeferredDailyMaintenance(")
includes(HOST, "ProcessDeferredDailyMaintenance", "ProcessPendingAutoWeeklyReportBuildBudget(")
includes(HOST, "ProcessPendingAutoWeeklyReportBuildBudget", "ProcessPendingWeeklyReportAggregationBudget(")
includes(HOST, "ProcessPendingAutoWeeklyReportBuildBudget", "ProcessPendingWeeklyReportPromptMaterialsBudget(")
includes(HOST, "ProcessPendingWeeklyReportAggregationBudget", "ApplyWeeklyPromptMaterialAggregation(")
includes(HOST, "ProcessPendingWeeklyReportPromptMaterialsBudget", "WeeklyPromptMaterialOwner.Prepare(")
includes(HOST, "BuildWeeklyEventMaterialPreviewGroups", "ApplyWeeklyPromptMaterialAggregation(", "int startDay")
includes(HOST, "ApplyWeeklyPromptMaterialAggregation", "WeeklyMaterialAggregationOwner.Apply(")
includes(HOST, "BuildWeeklyPromptAggregateCategoryMaterial", "WeeklyMaterialAggregationOwner.BuildCategoryMaterial(")
includes(HOST, "BuildWeeklyPromptAggregateEventLine", "WeeklyAggregateEventLine.Render(")
includes(HOST, "BuildWeeklyPromptResolvedVillageRaidMaterial", "WeeklyPromptMaterialOwner.BuildResolvedVillageRaidMaterial(")
includes(AGGREGATION, "Apply", "BuildWeeklyPromptAggregateEventKey(")
includes(AGGREGATION, "Apply", "GetWeeklyPromptAggregateCategoryOrder(")
includes(PROMPT, "BuildResolvedVillageRaidMaterial", "resolveSettlementDisplay(")
includes(PROMPT, "BuildShort", "BuildWeeklyPromptShortMaterial(")
includes(EVENT_LINE, "Render", "WeeklyMaterialAggregationOwner.ResolveWeeklyPromptAggregateCategory(")
includes(HOST, "CaptureWeeklyHeroFact", "FindHeroById(")
includes(HOST, "CaptureWeeklySettlementName", "Settlement.Find(")
includes(HOST, "CaptureWeeklySettlementNameWithType", "GetSettlementTypeLabel(")
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
    synchronous(HOST, name, signature)

print(f"PASS J17WeeklyMainThreadSourceContract {checks} checks (source witness, live game NOT_RUN)")
