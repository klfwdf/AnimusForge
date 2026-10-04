using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Runs synchronously when formal knowledge reaches a court. Game authority,
// presentation, and generation effects are supplied by the campaign adapter.
internal static class WorldDiplomacyCourtResponseApplication
{
    internal static void Receive(
        WorldDiplomacyStorage storage,
        string receiverId,
        WorldDiplomacyDocument document,
        Func<bool> representsAddressedVassal,
        Func<bool> isPlayerAffiliated,
        Func<bool> hasIndependentAuthority,
        Func<string, WorldDiplomacyRound> resolveRound,
        Action showPlayerDelivery,
        Action<WorldDiplomacyRound, WorldDiplomacyRoundParticipant> scheduleMandatoryResponse,
        Func<int> currentDay,
        Action<string> log)
    {
        if (receiverId == null || document == null
            || string.Equals(receiverId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)) return;
        bool directlyAddressed = (document.AddressedKingdomIds ?? new List<string>())
                .Contains(receiverId, StringComparer.OrdinalIgnoreCase)
            || string.Equals(document.TargetKingdomId, receiverId, StringComparison.OrdinalIgnoreCase)
            || representsAddressedVassal();
        if (isPlayerAffiliated()) document.HasReachedPlayerCourt = true;
        if (document.IsPlayerAuthored && hasIndependentAuthority())
        {
            WorldDiplomacyRound round = resolveRound(document.RoundId);
            bool activeDelivery = round != null && WorldDiplomacyLiveRoundRules.Contains(storage, round)
                && WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State);
            if (!activeDelivery) return;
            showPlayerDelivery();
            bool isPrimaryTarget = string.Equals(document.TargetKingdomId, receiverId, StringComparison.OrdinalIgnoreCase);
            if (directlyAddressed && (isPrimaryTarget
                || WorldDiplomacyStructureRules.DocumentRequiresResponseFrom(document, receiverId)))
            {
                WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(
                    round, receiverId, "active", mandatoryReply: true);
                scheduleMandatoryResponse(round, participant);
            }
        }
        log("court received document=" + document.DocumentId + " receiver=" + receiverId
            + " direct=" + directlyAddressed + " day=" + currentDay().ToString(CultureInfo.InvariantCulture));
    }

    internal static void TryScheduleMandatory(
        WorldDiplomacyStorage storage,
        WorldDiplomacyRound round,
        WorldDiplomacyRoundParticipant participant,
        string receiverId,
        WorldDiplomacyDocument trigger,
        Func<bool> isPlayerKingdom,
        Func<bool> hasIndependentAuthority,
        Func<bool> representsAddressedVassal,
        Func<(bool Blocked, string Reason)> canAiAuthor,
        Func<string, string, WorldDiplomacyDocument, string, bool, string> enqueueResponse,
        Action<string> log,
        int maxPriorityResponses)
    {
        bool isPrimaryTarget = trigger != null
            && string.Equals(trigger.TargetKingdomId, receiverId, StringComparison.OrdinalIgnoreCase);
        bool earlyEligible = round != null && participant != null && receiverId != null && trigger != null
            && !isPlayerKingdom() && hasIndependentAuthority() && trigger.IsPlayerAuthored;
        bool representativeForVassal = earlyEligible && !isPrimaryTarget && representsAddressedVassal();
        bool responseRequiredFrom = earlyEligible && !isPrimaryTarget && !representativeForVassal
            && WorldDiplomacyRoundLifecycleRules.IsResponseRequiredFrom(trigger, receiverId);
        bool alreadyResponded = earlyEligible && (isPrimaryTarget || representativeForVassal || responseRequiredFrom)
            && WorldDiplomacyDocumentFactRules.HasKingdomRespondedToDocument(
                storage.Documents, receiverId, trigger.DocumentId);
        string authorBlockReason = null;
        bool authorBlocked = false;
        if (earlyEligible && (isPrimaryTarget || representativeForVassal || responseRequiredFrom)
            && !alreadyResponded)
        {
            (authorBlocked, authorBlockReason) = canAiAuthor();
        }
        bool settlementPending = round != null && round.ResultSettlementPending;
        bool jobAlreadyQueued = earlyEligible && !alreadyResponded && !authorBlocked && !settlementPending
            && storage.Jobs.Any(x => x != null
                && string.Equals(x.AuthorKingdomId, receiverId, StringComparison.OrdinalIgnoreCase)
                && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.SourceDocumentId, trigger.DocumentId));
        int existingResponses = 0;
        int queuedResponses = 0;
        if (earlyEligible && !alreadyResponded && !authorBlocked && !settlementPending && !jobAlreadyQueued)
        {
            existingResponses = storage.Documents.Count(x => x != null && x.IsReadyForPublication
                && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.SourceDocumentId, trigger.DocumentId));
            queuedResponses = storage.Jobs.Count(x => x != null
                && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.SourceDocumentId, trigger.DocumentId));
        }
        WorldDiplomacyMandatoryReplyAction action = WorldDiplomacyRoundLifecycleRules.EvaluateMandatoryReplyAction(
            new WorldDiplomacyMandatoryReplyInput
            {
                RoundResolved = round != null,
                ParticipantResolved = participant != null,
                ReceiverResolved = receiverId != null,
                TriggerResolved = trigger != null,
                ReceiverIsPlayer = receiverId != null && isPlayerKingdom(),
                ReceiverHasAuthority = receiverId != null && hasIndependentAuthority(),
                TriggerPlayerAuthored = trigger != null && trigger.IsPlayerAuthored,
                IsPrimaryTarget = isPrimaryTarget,
                RepresentativeForAddressedVassal = representativeForVassal,
                ResponseRequiredFrom = responseRequiredFrom,
                AlreadyResponded = alreadyResponded,
                AuthorBlocked = authorBlocked,
                SettlementPending = settlementPending,
                JobAlreadyQueued = jobAlreadyQueued,
                ExistingResponses = existingResponses,
                QueuedResponses = queuedResponses,
                MaxPriorityResponses = maxPriorityResponses
            });
        if (!WorldDiplomacyRoundApplication.AdmitMandatoryReply(
            action, round, participant, receiverId, authorBlockReason, trigger, log)) return;
        string targetId = enqueueResponse?.Invoke(
            receiverId, trigger?.AuthorKingdomId, trigger, round?.RoundId, round?.RelayPlanned == true);
        log("mandatory response queued round=" + round.RoundId + " author=" + receiverId
            + " target=" + (targetId ?? "") + " source=" + trigger.DocumentId);
    }
}
