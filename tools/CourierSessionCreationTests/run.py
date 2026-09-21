"""Check outbound and inbound Courier session publication order."""
from pathlib import Path
import importlib.util

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionCreation.cs"
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

confirmed = extract.declaration(source, "private void OnLetterConfirmed(")
ordered(confirmed,
        "CreateCourierSession(flow, input.Trim());",
        "_sessions[session.Id] = session;",
        "AddCourierRuntimeIndex(session);",
        "StartCourierReplyGeneration(session, \"created_preflight\");",
        "ResetPendingFlow(\"confirm_done\");",
        "ProcessSession(session);")

outbound = extract.declaration(source, "private CourierSession CreateCourierSession(")
ordered(outbound,
        "Id = id,",
        "Stage = CourierStage.Outbound.ToString(),",
        "MoveRosterFromMainParty(",
        "EnsureCourierCampaignIdentity(courier, session, \"create_session\");",
        "PrepareOutgoingPayload(session, courier);",
        "BuildDeliveryFactText(session, delivered: false")

for signature in (
        "private bool TryCreateNpcInitiatedLetterSession(",
        "private bool TryCreateNpcDiplomacyLetterSession("):
    inbound = extract.declaration(source, signature)
    require(inbound.index("StartInboundLetterGeneration(")
            < inbound.index("ProcessSession(session);"),
            "generated inbound sessions must start their owner before session processing")
    ordered(inbound,
            "HasAnyActiveNpcInitiatedInboundCourier()",
            "Direction = CourierDirectionInboundToPlayer,",
            "EnsureCourierCampaignIdentity(",
            "_sessions[session.Id] = session;",
            "AddCourierRuntimeIndex(session, courier);",
            "StartInboundLetterGeneration(",
            "ProcessSession(session);")

generic = extract.declaration(source, "private bool TryCreateNpcLetterToPlayerSession(")
ordered(generic,
        "Direction = CourierDirectionInboundToPlayer,",
        "ReplyGenerated = true",
        "EnsureCourierCampaignIdentity(",
        "_sessions[session.Id] = session;",
        "AddCourierRuntimeIndex(session, courier);",
        "ProcessSession(session);")
require("StartInboundLetterGeneration(" not in generic,
        "prewritten external inbound letters must not trigger an LLM generation")

print("PASS CourierSessionCreation outboundPublish=1 inboundGenerated=2 inboundPrewritten=1 activeSenderGuard=1 live=NOT_RUN")
