"""Check Courier runtime indexes and loaded-session generation guards."""
from pathlib import Path
import importlib.util

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionRegistry.cs"
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

loaded = extract.declaration(source, "private void OnGameLoadFinished(")
ordered(loaded,
        "NormalizeSession(session);",
        "ResetReplyGenerationAfterLoad(session, \"game_load_finished\");",
        "ResolveCourierParty(session);",
        "ApplyCourierAiOverrides(courier, \"load_restore\");",
        "RebuildCourierRuntimeIndexes();")

rebuild = extract.declaration(source, "private void RebuildCourierRuntimeIndexes(")
ordered(rebuild,
        "lock (_sessionLock)",
        "!IsTerminalStage(session)",
        "_activeCourierPartyIdsSnapshot = partyIds;",
        "_courierRuntimeIndexesReady = true;",
        "UpdateAiCourierPresenceFlag(partyIds, _courierRuntimeIndexesReady);")

add = extract.declaration(source, "private void AddCourierRuntimeIndex(")
remove = extract.declaration(source, "private void RemoveCourierRuntimeIndex(")
require("new HashSet<string>(_activeCourierPartyIdsSnapshot" in add,
        "runtime index add must publish a copied snapshot")
require("new HashSet<string>(_activeCourierPartyIdsSnapshot" in remove,
        "runtime index removal must publish a copied snapshot")

restart = extract.declaration(source, "private static void ResetReplyGenerationAfterLoad(")
ordered(restart,
        "if (IsTerminalStage(session))",
        "if (IsInboundToPlayer(session)",
        "!string.IsNullOrWhiteSpace(session.InboundCompletionReceipt)",
        "session.ReplyGenerationStarted = !session.ReplyGenerated;",
        "if (session.ReplyGenerated)",
        "session.ReplyGenerationStarted = false;")
require(restart.count("return;") >= 5,
        "persisted inbound receipts must stop a second generation path")

lookup = extract.declaration(source, "private CourierSession GetSessionById(")
require("lock (_sessionLock)" in lookup and "_sessions.TryGetValue" in lookup,
        "session lookup must remain synchronized")

print("PASS CourierSessionRegistry loadReset=1 copiedIndexes=2 receiptRestartGuard=1 synchronizedLookup=1 live=NOT_RUN save=NOT_RUN")
