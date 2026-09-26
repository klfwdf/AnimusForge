"""Check Courier return, payload, destroy, and missing-session ordering."""
from pathlib import Path
import importlib.util

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DeliveryLifetime.cs"
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
complete = extract.declaration(source, "private void CompleteReturn(")
ordered(complete,
        "ReturnCourierContentsToPlayer(session, courier);",
        "AddCourierLetterToPlayerInventory(",
        "ShowCourierReplyNotice(",
        "CompleteAndDestroyCourier(session, courier);")

refund = extract.declaration(source, "private void ReturnCourierContentsToPlayer(")
ordered(refund,
        "MoveWholeMemberRoster(",
        "MoveWholePrisonRoster(",
        "MoveWholeItemRoster(",
        "if (!session.DeliveryApplied && session.EscrowGold > 0)",
        "session.EscrowGold = 0;")

destroyed = extract.declaration(source, "private void OnMobilePartyDestroyed(")
ordered(destroyed,
        "if (IsInboundToPlayer(session))",
        "HandleInboundCourierDestroyed(",
        "TryMoveHeroLossesToDestroyer(",
        "session.Stage = CourierStage.Destroyed.ToString();",
        "_sessions.Remove(session.Id);",
        "RemoveCourierRuntimeIndex(session);")

complete_destroy = extract.declaration(source, "private void CompleteAndDestroyCourier(")
ordered(complete_destroy,
        "session.Stage = CourierStage.Completed.ToString();",
        "_sessions.Remove(session.Id);",
        "RemoveCourierRuntimeIndex(session);",
        "DestroyCourierTemporaryShips(",
        "DestroyPartyAction.Apply(")

missing = extract.declaration(source, "private void HandleCourierMissing(")
require("HandleInboundCourierMissing(session);" in missing,
        "inbound missing sessions must retain their channel-specific terminal path")

print("PASS CourierDeliveryLifetime return=1 refund=1 destroy=1 missing=1 terminalIndexCleanup=1 live=NOT_RUN")
