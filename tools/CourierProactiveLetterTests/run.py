"""Check bounded Courier tick and proactive-letter scan ownership."""
from pathlib import Path
import importlib.util

ROOT = Path(__file__).resolve().parents[2]
CHANNEL = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier"
RUNTIME = CHANNEL / "CourierDeliveryBehavior.RuntimeTick.cs"
PROACTIVE = CHANNEL / "CourierDeliveryBehavior.ProactiveLetters.cs"
spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)


def require(value, message):
    if not value:
        raise AssertionError(message)


def ordered(text, *markers):
    cursor = -1
    for marker in markers:
        next_cursor = text.find(marker, cursor + 1)
        require(next_cursor > cursor, "missing/out-of-order marker: " + marker)
        cursor = next_cursor


runtime = RUNTIME.read_text(encoding="utf-8-sig")
proactive = PROACTIVE.read_text(encoding="utf-8-sig")

tick = extract.declaration(runtime, "private void OnCampaignTick(")
ordered(tick,
        "ProcessNpcInitiatedLetterScan();",
        "CampaignTickThrottleSeconds",
        "ProcessCourierLetterInventoryRestoreRetry();",
        "ProcessOneCourierInboundCompletionReceipt();",
        "snapshot = _sessions.Values.Where",
        "ProcessSession(session);")

hourly = extract.declaration(runtime, "private void OnHourlyTick(")
require("TryStartNpcInitiatedLetterScan();" in hourly,
        "hourly tick must retain the proactive scan owner")

scan = extract.declaration(proactive, "private void ProcessNpcInitiatedLetterScan(")
ordered(scan,
        "Stopwatch stopwatch = Stopwatch.StartNew();",
        "processed < scan.BatchSize",
        "processed++;",
        "stopwatch.Elapsed.TotalMilliseconds >= NpcInitiatedLetterScanFrameBudgetMilliseconds",
        "CompleteNpcInitiatedLetterScan(scan);")
require("if (stopwatch.Elapsed.TotalMilliseconds >= NpcInitiatedLetterScanFrameBudgetMilliseconds)" in scan
        and "if (false" not in scan,
        "proactive scan frame budget must remain active")

complete = extract.declaration(proactive, "private void CompleteNpcInitiatedLetterScan(")
ordered(complete,
        "ReferenceEquals(_npcInitiatedLetterScan, scan)",
        "_npcInitiatedLetterScan = null;",
        "HasAnyActiveNpcInitiatedInboundCourier()",
        "PickWeightedNpcLetterCandidate(",
        "TryCreateNpcInitiatedLetterSession(")

print("PASS CourierProactiveLetters tickOrder=1 boundedScan=1 currentScan=1 activeInboundGuard=1 live=NOT_RUN performance=NOT_RUN")
