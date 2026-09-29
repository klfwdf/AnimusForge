using System;
using System.Collections.Generic;
using System.Globalization;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Owns post-publication round accounting and the next round transition.
internal static class WorldDiplomacyRoundProgressApplication
{
    internal static void HandleRoundDocumentProcessed(
        WorldDiplomacyDocument document,
        WorldDiplomacyStorage storage,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<int> currentDay,
        Action<WorldDiplomacyRound, WorldDiplomacyDocument, string, string> beginOrExtendResultSettlement,
        Action<WorldDiplomacyRound, WorldDiplomacyDocument> commitEmbeddedRoundPlan,
        Action<WorldDiplomacyRound, WorldDiplomacyDocument> enqueueRoundPlanJob,
        Action<WorldDiplomacyRound> scheduleNextResultSettlementTurn,
        Action<WorldDiplomacyRound, WorldDiplomacyDocument> integratePlayerDeclaration,
        Action<WorldDiplomacyRound> refreshResultSettlementActionSlots,
        Action<string> closeActiveRound,
        Action<WorldDiplomacyRound> advanceRelay,
        Action<string> log)
    {
        if (document == null || document.RoundProgressHandled) return;
        WorldDiplomacyRound round = resolveRound(document.RoundId);
        if (round == null || !WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State)) return;
        int successfulMechanicalActionCount =
            WorldDiplomacyRoundLifecycleRules.CountSuccessfulMechanicalActions(document);
        bool successfulMechanicalAction = successfulMechanicalActionCount > 0;
        bool substantiveProgress = document.MadeDiplomaticProgress;
        bool isRootDocument = WorldDiplomacyRoundLifecycleRules.IsRootRoundDocument(
            round.RootDocumentId, document.DocumentId);
        WorldDiplomacyRoundParticipant participant = null;
        if (!document.RoundAccountingHandled)
        {
            substantiveProgress = WorldDiplomacyRoundLifecycleRules.IsValidatedSubstantiveProgress(
                document, round.PendingOffers, storage.DiplomaticThreats, successfulMechanicalAction);
            int diplomaticActionAttemptCount = WorldDiplomacyRoundLifecycleRules.ComputeDiplomaticActionAttemptCount(
                document.Actions,
                substantiveProgress,
                document.Actions == null
                    && WorldDiplomacyRoundLifecycleRules.IsValidatedDiplomaticActionAttempt(document, round.PendingOffers, storage.DiplomaticThreats, successfulMechanicalAction));
            bool diplomaticActionAttempt = diplomaticActionAttemptCount > 0;
            int accountingDay = currentDay();
            if (!isRootDocument && !document.IsPlayerAuthored && document.IsRelayTurn)
            {
                participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, document.AuthorKingdomId, "active", mandatoryReply: false);
            }
            round.LastActivityDay = accountingDay;
            if (successfulMechanicalAction) round.ExecutedActionCount += successfulMechanicalActionCount;
            document.MadeDiplomaticProgress = substantiveProgress;
            if (substantiveProgress)
            {
                round.SubstantiveProgressCount++;
                round.LastSubstantiveProgressDay = accountingDay;
            }
            if (diplomaticActionAttempt)
            {
                round.DiplomaticActionAttemptCount += diplomaticActionAttemptCount;
                if (WorldDiplomacyRoundLifecycleRules.ShouldExtendHardEndForLateProposal(
                    round.ConsecutiveNoActionPasses, document.Actions, document.Intent))
                {
                    round.HardEndDay = WorldDiplomacyRoundLifecycleRules.ExtendHardEndDay(
                        round.HardEndDay, accountingDay, Math.Max(1, round.RelayPassDurationDays));
                }
            }
            if (string.IsNullOrWhiteSpace(round.RootDocumentId))
            {
                round.RootDocumentId = document.DocumentId;
                round.InitiatorKingdomId = document.AuthorKingdomId;
                isRootDocument = true;
            }
            if (participant != null)
            {
                participant.TurnCount++;
                participant.LastSpokeDay = accountingDay;
                if (substantiveProgress) participant.ContributionMade = true;
                if (string.Equals(document.RoundParticipation, "withdraw", StringComparison.OrdinalIgnoreCase))
                {
                    participant.State = "withdrawn";
                }
            }
            document.RoundAccountingHandled = true;
            if (substantiveProgress)
            {
                log("substantive diplomacy progress accepted round=" + round.RoundId
                    + " document=" + document.DocumentId
                    + " intent=" + WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent)
                    + " count=" + round.SubstantiveProgressCount.ToString(CultureInfo.InvariantCulture));
            }
            if (diplomaticActionAttempt)
            {
                log("diplomatic relation-change attempt accepted round=" + round.RoundId
                    + " document=" + document.DocumentId
                    + " intent=" + WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent)
                    + " count=" + round.DiplomaticActionAttemptCount.ToString(CultureInfo.InvariantCulture));
            }
        }
        if (round.ResultSettlementPending)
        {
            WorldDiplomacyRoundLifecycleRules.ExpireUnansweredSettlementOffersForNoActionDeclaration(round, document, log);
            WorldDiplomacyRoundLifecycleRules.ConsumeResultSettlementSpeaker(round, document, log);
        }
        if (WorldDiplomacyRoundLifecycleRules.TryGetConfirmedRoundResult(document, round, storage?.DiplomaticThreats, out string confirmedCloseReason, out string confirmedRoundStatus))
        {
            beginOrExtendResultSettlement(round, document, confirmedCloseReason, confirmedRoundStatus);
        }
        if (isRootDocument)
        {
            if (document.HasEmbeddedRoundPlan && !round.RelayPlanned)
            {
                commitEmbeddedRoundPlan(round, document);
            }
            if (!ReferenceEquals(storage.ActiveRound, round)
                || !WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State))
            {
                document.RoundProgressHandled = true;
                return;
            }
            if (!round.RelayPlanned) enqueueRoundPlanJob(round, document);
            else if (round.ResultSettlementPending) scheduleNextResultSettlementTurn(round);
            document.RoundProgressHandled = true;
            return;
        }
        if (!round.RelayPlanned)
        {
            enqueueRoundPlanJob(round, resolveDocument(round.RootDocumentId) ?? document);
            document.RoundProgressHandled = true;
            return;
        }
        if (document.IsPlayerAuthored)
        {
            integratePlayerDeclaration(round, document);
            if (round.ResultSettlementPending)
            {
                refreshResultSettlementActionSlots(round);
                scheduleNextResultSettlementTurn(round);
            }
            document.RoundProgressHandled = true;
            return;
        }
        if (round.ResultSettlementPending)
        {
            if (document.IsExternalResponseOnly && participant != null) participant.MandatoryReplyPending = false;
            refreshResultSettlementActionSlots(round);
            scheduleNextResultSettlementTurn(round);
            document.RoundProgressHandled = true;
            return;
        }
        if (!document.IsRelayTurn)
        {
            document.RoundProgressHandled = true;
            return;
        }
        participant ??= WorldDiplomacyRoundLifecycleRules.SelectParticipantByKingdom(
            round.Participants, document.AuthorKingdomId);
        if (document.IsExternalResponseOnly)
        {
            if (participant != null) participant.MandatoryReplyPending = false;
            log("priority player declaration response completed without moving relay cursor round=" + round.RoundId
                + " document=" + document.DocumentId + " author=" + document.AuthorKingdomId);
            document.RoundProgressHandled = true;
            return;
        }
        round.RelayWaiting = false;
        if (WorldDiplomacyIntentVocabulary.IsTerminalNegotiationMove(document.NegotiationMove))
        {
            round.RoundStatus = WorldDiplomacyRoundLifecycleRules.NormalizeTerminalMoveStatus(
                document.NegotiationMove);
            closeActiveRound(WorldDiplomacyRoundLifecycleRules.ResolveTerminalMoveCloseReason(
                round.RoundStatus));
            document.RoundProgressHandled = true;
            return;
        }
        if (WorldDiplomacyRoundLifecycleRules.IsRelayResolvedClose(
            document.RoundStatus, round.ExecutedActionCount))
        {
            round.RoundStatus = "resolved";
            closeActiveRound("relay_resolved");
            document.RoundProgressHandled = true;
            return;
        }
        advanceRelay(round);
        document.RoundProgressHandled = true;
    }

}
