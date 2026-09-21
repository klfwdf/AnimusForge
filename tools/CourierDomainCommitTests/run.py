"""Audit the extracted Courier arrival commit and reply-wait owners."""
from __future__ import annotations

import argparse
import importlib.util
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent

spec = importlib.util.spec_from_file_location(
    "extractor", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument(
    "--mutate",
    choices=("drop-delivery-guard", "release-before-active-check"),
)
args = parser.parse_args()


def require(value: bool, message: str) -> None:
    if not value:
        raise AssertionError(message)


def ordered(text: str, *markers: str) -> None:
    cursor = -1
    for marker in markers:
        next_cursor = text.find(marker, cursor + 1)
        require(next_cursor > cursor, "missing/out-of-order marker: " + marker)
        cursor = next_cursor


def main() -> None:
    combined = extractor.courier_source(None)
    domain = (ROOT / extractor.COURIER_DOMAIN_COMMIT_PATH).read_text(
        encoding="utf-8-sig")
    wait = (ROOT / extractor.COURIER_REPLY_WAIT_PATH).read_text(
        encoding="utf-8-sig")

    core = extractor.declaration(
        domain, "private void CommitGeneratedReplyActionsAtRecipientCore(")
    execute = extractor.declaration(
        domain, "private InteractionStatus ExecuteCourierActionPlanForExternal(")
    economy = extractor.declaration(
        domain, "private InteractionStatus GateCourierEconomyActionPlanForExternal(")
    eligible = extractor.declaration(
        domain, "private static bool IsCourierActionSessionEligible(")
    reserve = extractor.declaration(
        domain, "private static bool TryReserveCourierEconomyOnly(")
    persist = extractor.declaration(
        domain, "private void PersistCourierReplyToHistories(")
    begin = extractor.declaration(
        wait, "private void BeginCourierReplyWaitPause(")
    end = extractor.declaration(
        wait, "private void EndCourierReplyWaitPause(")
    active = extractor.declaration(
        wait, "private bool HasActiveCourierReplyWait(")

    if args.mutate == "drop-delivery-guard":
        core = core.replace("\t\tif (!session.DeliveryApplied)\n\t\t{\n\t\t\treturn;\n\t\t}\n", "", 1)
    elif args.mutate == "release-before-active-check":
        end = end.replace(
            "\t\tif (HasActiveCourierReplyWait())\n\t\t{\n\t\t\treturn;\n\t\t}\n",
            "\t\t_courierReplyWaitTimeLocked = false;\n", 1)

    signatures = (
        "private void CommitGeneratedReplyActionsAtRecipientCore(",
        "private InteractionStatus ExecuteCourierActionPlanForExternal(",
        "private InteractionStatus GateCourierEconomyActionPlanForExternal(",
        "private static bool IsCourierActionSessionEligible(",
        "private static bool TryReserveCourierEconomyOnly(",
        "private void PersistCourierReplyToHistories(",
        "private void ShowCourierReplyWaitPopupAndPause(",
        "private void BeginCourierReplyWaitPause(",
        "private void EndCourierReplyWaitPause(",
        "private bool HasActiveCourierReplyWait(",
    )
    for signature in signatures:
        require(combined.count(signature) == 1,
                "Courier owner declaration must exist exactly once: " + signature)

    ordered(core,
            "session.PostprocessConsumed",
            "!session.DeliveryApplied",
            "TeamModuleServices.Policy.TryProcessAcceptedAgendaTag",
            "session.ReplyPostprocessedText = text",
            "session.PostprocessConsumed = true",
            "PersistCourierReplyToHistories(")
    require("!session.DeliveryApplied" in core,
            "Courier domain effects can run before recipient delivery")
    require(core.count("session.PostprocessConsumed = true") == 2,
            "Courier domain owner lost invalid-target or successful terminal consumption")

    ordered(execute,
            "snapshot.Identity.Channel != InteractionChannel.Courier",
            "IsCourierActionSessionEligible(",
            "session.ReplyPostprocessedText = actionPlan.RawPostprocessId",
            "CommitGeneratedReplyActionsAtRecipientCore(")
    require("unsupported_channel" in execute,
            "Courier detached duel dispatch must remain rejected")
    ordered(economy,
            "actionPlan.Actions.All(LegacyEconomyRewardDebtAdapter.IsEconomyAction)",
            "IsCourierActionSessionEligible(",
            "TryReserveCourierEconomyOnly(")
    for marker in (
        "snapshot.Identity.Channel == InteractionChannel.Courier",
        "string.Equals(session.Id, expectedSessionId",
        "!IsInboundToPlayer(session)",
        "!IsTerminalStage(session)",
        "session.DeliveryApplied",
        "!session.PostprocessConsumed",
        "string.Equals(snapshot.Identity.SubjectId, recipientHeroId",
    ):
        require(marker in eligible, "Courier eligibility lost guard: " + marker)
    require("session.PostprocessConsumed = true" in reserve,
            "Courier economy reservation is no longer one-shot")
    require(persist.count("AppendExternalDialogueHistory(") == 1
            and persist.count("RecordNativeConversationNpcLineForExternal(") == 1
            and persist.count("NoteCourierReplyForExternal(") == 1,
            "Courier reply history owner must write each downstream history exactly once")

    ordered(begin,
            "if (!_courierReplyWaitTimeLocked)",
            "_courierReplyWaitPreviousMode = campaign.TimeControlMode",
            "_courierReplyWaitPreviousLock = campaign.TimeControlModeLock",
            "campaign.TimeControlMode = CampaignTimeControlMode.Stop",
            "campaign.SetTimeControlModeLock(true)",
            "_courierReplyWaitTimeLocked = true")
    ordered(end,
            "completedSession.ReplyWaitPopupShown = false",
            "HasActiveCourierReplyWait()",
            "InformationManager.HideInquiry()",
            "campaign.SetTimeControlModeLock(_courierReplyWaitPreviousLock)",
            "campaign.TimeControlMode = _courierReplyWaitPreviousMode",
            "_courierReplyWaitTimeLocked = false")
    require("lock (_sessionLock)" in active
            and "!IsTerminalStage(x)" in active
            and "!x.ReplyGenerated" in active
            and "x.ReplyWaitPopupShown" in active,
            "Courier reply-wait liveness must inspect active sessions under the session lock")

    print("PASS CourierDomainCommit checks=32 declarations=10 deliveryGate=1 "
          "sessionIdentity=1 economyOneShot=1 historySingleWrite=1 waitLockOwner=1 "
          "live=NOT_RUN")


if __name__ == "__main__":
    main()
