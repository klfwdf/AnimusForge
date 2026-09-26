"""Check Courier letter inventory sanitization and retry ownership."""
from pathlib import Path
import importlib.util

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.LetterInventory.cs"
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


source = SOURCE.read_text(encoding="utf-8-sig")

normalize = extract.declaration(source, "private CourierLetterInventoryRecord NormalizeCourierLetterInventoryRecord(")
ordered(normalize,
        "AnimusForgeTextInputSanitizer.SanitizeMultiline(record.DisplayName",
        "TrySplitCourierLetterInventoryDisplayName(",
        "CourierVisibleLetterSanitizer.Clean(StripCourierActionTags(letterBody))",
        "record.LetterBody = letterBody;")

add = extract.declaration(source, "private static void AddCourierLetterToPlayerInventory(")
ordered(add,
        "CourierVisibleLetterSanitizer.Clean(AnimusForgeTextInputSanitizer.SanitizeMultiline(StripCourierActionTags(letterText ?? \"\")",
        "RewardSystemBehavior.GenerateNamedInventoryItemToRosterForExternal(",
        "Instance?.RememberCourierLetterInventoryRecord(")

schedule = extract.declaration(source, "private void ScheduleCourierLetterInventoryRestoreRetries(")
ordered(schedule,
        "_courierLetterInventoryRestoreRetryRemaining = 0;",
        "_nextCourierLetterInventoryRestoreRetryUtcTicks = 0L;",
        "disabled=discard_guard")

retry = extract.declaration(source, "private void ProcessCourierLetterInventoryRestoreRetry(")
ordered(retry,
        "_courierLetterInventoryRestoreRetryRemaining <= 0",
        "_nextCourierLetterInventoryRestoreRetryUtcTicks > 0L && now < _nextCourierLetterInventoryRestoreRetryUtcTicks",
        "_courierLetterInventoryRestoreRetryRemaining--;",
        "RestoreCourierLetterInventoryItems(reason);")

detail = extract.declaration(source, "public static bool TryGetCourierLetterInventoryDetailForExternal(")
ordered(detail,
        "instance.EnsureCourierLetterInventoryData();",
        "instance._courierLetterInventoryRecords.TryGetValue(",
        "instance.NormalizeCourierLetterInventoryRecord(",
        "letterBody = record.LetterBody;")

print("PASS CourierLetterInventory sanitize=2 discardGuard=1 delayedRetryLegacy=1 detailLookup=1 live=NOT_RUN save=NOT_RUN")
