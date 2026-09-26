"""Check the relocated Courier route owner without simulating Bannerlord navigation."""
from pathlib import Path
import importlib.util

ROOT = Path(__file__).resolve().parents[2]
ROUTE = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.RouteTransport.cs"
SESSION = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionTransport.cs"
GENERATION = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs"

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


route = ROUTE.read_text(encoding="utf-8-sig")
session = SESSION.read_text(encoding="utf-8-sig")
generation = GENERATION.read_text(encoding="utf-8-sig")

refresh = extract.declaration(route, "private static bool ShouldRefreshRouteCore(")
require("if (monitorProgress && IsCourierRouteStuck(session, key, courier))" in refresh
        and "if (true ||" not in refresh,
        "route stuck refresh must remain opt-in and progress-gated")
ordered(refresh,
        "if (!string.Equals(session.LastRouteKey",
        "session.LastRouteKey = key;",
        "ResetCourierRouteProgress(session, key, courier);",
        "if (monitorProgress && IsCourierRouteStuck(session, key, courier))",
        "expectedDefaultBehaviors.Contains(courier.DefaultBehavior)")

recipient_route = extract.declaration(route, "private void RouteToRecipient(")
ordered(recipient_route,
        "BuildCourierRoutePlan(",
        "EnsureCourierNavalReadiness(",
        "IsCourierRouteTargetMismatched(",
        "ShouldRefreshRouteWithProgress(")

process = extract.declaration(session, "private void ProcessSession(")
ordered(process,
        "TryGetRecipientTarget(",
        "IsAtRecipient(",
        "DeliverToRecipient(session, courier, recipient);")

delivery = extract.declaration(generation, "private void DeliverToRecipient(")
ordered(delivery,
        "ApplyDeliveryPayload(session, courier, recipient);",
        "session.DeliveryApplied = true;",
        "if (!session.ReplyGenerated)",
        "CommitGeneratedReplyAtRecipient(session, recipient);")

print("PASS CourierRouteTransport routeRefresh=1 targetMismatch=1 arrivalBeforeDelivery=1 deliveryBeforeCommit=1 live=NOT_RUN")
