using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain;

public static class WorldDiplomacyRoundLifecycleRules
{
    public static WorldDiplomacyRoundReconcileDecision EvaluateReconcileAfterLoad(
        WorldDiplomacyRoundReconcileInput input)
    {
        if (input == null || !input.RoundActive)
        {
            return WorldDiplomacyRoundReconcileDecision.Of(WorldDiplomacyRoundReconcileAction.None);
        }
        if (input.CurrentDay >= input.HardEndDay)
        {
            return WorldDiplomacyRoundReconcileDecision.Of(
                WorldDiplomacyRoundReconcileAction.CloseHardEndAfterLoad);
        }
        if (input.HasPersistedWork || input.PlayerWaiting)
        {
            return WorldDiplomacyRoundReconcileDecision.Of(WorldDiplomacyRoundReconcileAction.None);
        }
        if (input.ResultSettlementPending)
        {
            return WorldDiplomacyRoundReconcileDecision.Of(
                WorldDiplomacyRoundReconcileAction.ScheduleResultSettlementTurn,
                clearOrphanedRelayWait: true);
        }
        if (input.RelayPlanned)
        {
            return WorldDiplomacyRoundReconcileDecision.Of(
                WorldDiplomacyRoundReconcileAction.ScheduleRelayImmediately,
                clearOrphanedRelayWait: true);
        }
        if (!input.HasRootDocument)
        {
            return WorldDiplomacyRoundReconcileDecision.Of(
                WorldDiplomacyRoundReconcileAction.CloseMissingRootDocument,
                clearOrphanedRelayWait: true);
        }
        return WorldDiplomacyRoundReconcileDecision.Of(
            WorldDiplomacyRoundReconcileAction.None,
            clearOrphanedRelayWait: true);
    }

    public static bool IsHardEndReached(int currentDay, int hardEndDay)
    {
        return currentDay >= hardEndDay;
    }

    public static int ComputeSettlementWindowDays(int routeKingdomCount)
    {
        int count = routeKingdomCount < 0 ? 0 : routeKingdomCount;
        return Math.Max(14, (count + 2) * 2);
    }

    public static string NormalizeResultSettlementStatus(string roundStatus)
    {
        return string.Equals(roundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
            ? "deadlocked" : "resolved";
    }

    public static bool IsResolvedRoundStatus(string roundStatus)
    {
        return string.Equals(roundStatus, "resolved", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeTerminalMoveStatus(String negotiationMove)
    {
        return string.Equals(negotiationMove, "declare_deadlock", StringComparison.OrdinalIgnoreCase)
            ? "deadlocked" : "resolved";
    }

    public static bool ShouldForceTerminalMove(int consecutiveNoActionPasses)
    {
        return consecutiveNoActionPasses >= 2;
    }

    public static int ExtendHardEndDay(int currentHardEndDay, int currentDay, int minimumDays)
    {
        return Math.Max(currentHardEndDay, currentDay + minimumDays);
    }

    public static bool IsTerminalRoundStatus(string roundStatus)
    {
        return string.Equals(roundStatus, "resolved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roundStatus, "closed", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRetryableAbort(string roundStatus, bool initiatorHasPublished)
    {
        return string.Equals(roundStatus, "aborted", StringComparison.OrdinalIgnoreCase)
            || !initiatorHasPublished;
    }

    public static int NormalizeRelayCursor(int cursor, int routeCount)
    {
        return cursor < 0 || cursor >= routeCount ? 0 : cursor;
    }

    public static int NormalizeRelayDirection(int direction)
    {
        return direction == 1 || direction == -1 ? direction : 1;
    }

    public static int NextRelayDirection(int direction)
    {
        return direction >= 0 ? -1 : 1;
    }

    public static int ComputeRelayProgressIndex(int routeCount, int nextIndex, int direction)
    {
        return direction > 0 ? nextIndex : routeCount - 1 - nextIndex;
    }

    public static int ComputeRelayArrivalDay(WorldDiplomacyRelayArrivalPlanInput input)
    {
        int edgeCount = Math.Max(1, input.RouteCount - 1);
        int progress = ComputeRelayProgressIndex(input.RouteCount, input.NextIndex, input.RelayDirection);
        int plannedDay = input.RelayPassStartedDay
            + (int)Math.Ceiling(input.PassDurationDays * Math.Max(1, progress) / (double)edgeCount);
        if (input.ScheduleImmediately)
        {
            plannedDay = input.CurrentDay;
        }
        if (input.FinalActionOpportunityIssued && input.SubstantiveProgressCount <= 0)
        {
            plannedDay = Math.Min(plannedDay, input.HardEndDay);
        }
        return plannedDay;
    }

    public static WorldDiplomacyRelayPassAccountingDecision EvaluateRelayPassAccounting(
        WorldDiplomacyRelayPassAccountingInput input)
    {
        if (input == null || input.RelayPassNumber <= 0
            || input.LastAccountedRelayPassNumber >= input.RelayPassNumber)
        {
            return new WorldDiplomacyRelayPassAccountingDecision
            {
                ShouldAccount = false,
                ActionOccurred = false,
                NewConsecutiveNoActionPasses = input?.ConsecutiveNoActionPasses ?? 0
            };
        }
        bool actionOccurred = input.DiplomaticActionAttemptCount > input.ActionAttemptCountAtPassStart;
        return new WorldDiplomacyRelayPassAccountingDecision
        {
            ShouldAccount = true,
            ActionOccurred = actionOccurred,
            NewConsecutiveNoActionPasses = actionOccurred
                ? 0
                : Math.Min(3, input.ConsecutiveNoActionPasses + 1)
        };
    }

    public static int ClampNoActionPassCount(int consecutiveNoActionPasses)
    {
        return Math.Max(0, Math.Min(3, consecutiveNoActionPasses));
    }

    public static int ClampPassStartAttemptCount(int attemptCount, int passStartCount)
    {
        return Math.Max(0, Math.Min(attemptCount, passStartCount));
    }

    public static int ClampLastAccountedPassNumber(int lastAccountedPassNumber)
    {
        return Math.Max(0, lastAccountedPassNumber);
    }

    public static bool CanScheduleResultSettlementTurn(WorldDiplomacySettlementTurnGateInput input)
    {
        return input != null && input.ResultSettlementPending && input.RelayPlanned
            && input.RoundActive && !input.HasPendingArrival && !input.HasPendingJob;
    }

    public static WorldDiplomacySettlementSlotAction EvaluateSettlementSlotAction(
        WorldDiplomacySettlementSlotEvaluationInput input)
    {
        if (input == null || !input.HasSlot)
        {
            return WorldDiplomacySettlementSlotAction.CloseRound;
        }
        if (!input.ReceiverEligible || input.ActionableTargetCount <= 0)
        {
            return WorldDiplomacySettlementSlotAction.SkipSlot;
        }
        return input.ReceiverIsPlayer
            ? WorldDiplomacySettlementSlotAction.WaitForPlayer
            : WorldDiplomacySettlementSlotAction.ScheduleNpc;
    }

    public static string ResolveSettlementCloseReason(string closeReason)
    {
        return string.IsNullOrWhiteSpace(closeReason) ? "result_settled" : closeReason;
    }

    public static string ResolveSettlementPreviousSpeaker(
        IReadOnlyList<string> relatedKingdomIds,
        string lastPublishedAuthorId,
        string initiatorKingdomId)
    {
        if (relatedKingdomIds != null)
        {
            foreach (string kingdomId in relatedKingdomIds)
            {
                if (!string.IsNullOrWhiteSpace(kingdomId))
                {
                    return kingdomId;
                }
            }
        }
        return !string.IsNullOrWhiteSpace(lastPublishedAuthorId)
            ? lastPublishedAuthorId
            : initiatorKingdomId;
    }

    public static WorldDiplomacyRoundTerminalAction EvaluateRoundTerminalClose(
        bool hasRunningJob, bool resultSettlementPending)
    {
        if (hasRunningJob)
        {
            return WorldDiplomacyRoundTerminalAction.WaitForRunningJob;
        }
        return resultSettlementPending
            ? WorldDiplomacyRoundTerminalAction.CloseResultSettlement
            : WorldDiplomacyRoundTerminalAction.CloseRelay;
    }

    public static bool IsPlayerSlotWaitingExpired(
        string slotStatus, int playerWaitingSinceDay, int currentDay)
    {
        return string.Equals(slotStatus, "waiting_player", StringComparison.OrdinalIgnoreCase)
            && playerWaitingSinceDay > 0
            && currentDay >= playerWaitingSinceDay + 5;
    }

    public static bool IsWaitingPlayerSlot(string slotStatus)
    {
        return string.Equals(slotStatus, "waiting_player", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsActiveRelayParticipant(
        bool selectedForRelay, bool isPlayerAsync, string participantState)
    {
        return selectedForRelay && !isPlayerAsync
            && !string.Equals(participantState, "withdrawn", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsArrivalStale(bool roundActive, long arrivalSequence, long currentSequence)
    {
        return !roundActive || arrivalSequence != currentSequence;
    }

    public static WorldDiplomacyRelayArrivalAction EvaluateArrivalAction(
        WorldDiplomacyArrivalEvaluationInput input)
    {
        if (input == null)
        {
            return WorldDiplomacyRelayArrivalAction.IgnoreArrival;
        }
        if (input.SettlementPending)
        {
            if (!input.SettlementSlotFound || !input.CurrentSlotMatches)
            {
                return WorldDiplomacyRelayArrivalAction.RescheduleSettlementTurn;
            }
            if (!input.ReceiverEligible)
            {
                return WorldDiplomacyRelayArrivalAction.SkipSettlementSlotAndReschedule;
            }
            return WorldDiplomacyRelayArrivalAction.DeliverSettlementTurn;
        }
        if (!input.RouteIndexFound || !input.ReceiverEligible)
        {
            return WorldDiplomacyRelayArrivalAction.AdvanceRelay;
        }
        return input.ReceiverIsPlayer
            ? WorldDiplomacyRelayArrivalAction.AdvanceRelayAfterPlayerOpportunity
            : WorldDiplomacyRelayArrivalAction.DeliverRelayTurn;
    }

    public static int NextTechnicalFailureCount(int currentFailures, int maxFailures)
    {
        return Math.Min(maxFailures, Math.Max(0, currentFailures) + 1);
    }

    public static bool ShouldTripTechnicalCircuitBreaker(int failures, int maxFailures)
    {
        return failures >= maxFailures;
    }

    public static WorldDiplomacyRejectedGenerationAction EvaluateRejectedGenerationAction(
        WorldDiplomacyRejectedGenerationInput input)
    {
        if (input == null || !input.IsRelayTurn || !input.RoundActive)
        {
            return WorldDiplomacyRejectedGenerationAction.CompleteExchange;
        }
        if (input.CircuitBreakerTripped)
        {
            return WorldDiplomacyRejectedGenerationAction.CloseRound;
        }
        return input.ResultSettlementPending
            ? WorldDiplomacyRejectedGenerationAction.SkipSettlementSlotAndReschedule
            : WorldDiplomacyRejectedGenerationAction.AdvanceRelayImmediately;
    }

    public static bool IsReminderDue(bool reminderSent, int currentDay, int mandatorySinceDay)
    {
        return !reminderSent && currentDay >= mandatorySinceDay + 3;
    }

    public static bool IsMandatoryTimeoutExpired(int currentDay, int mandatorySinceDay)
    {
        return currentDay >= mandatorySinceDay + 5;
    }

    public static bool IsNoActionExpiryEligible(
        bool resultSettlementPending, bool isNoActionDeclaration, bool hasSlotId, bool hasAuthor)
    {
        return resultSettlementPending && isNoActionDeclaration && hasSlotId && hasAuthor;
    }

    public static bool ShouldExpireSettlementOffer(
        string offerStatus, bool targetsAuthor, bool sourceInSlot)
    {
        return string.Equals(offerStatus, "open", StringComparison.OrdinalIgnoreCase)
            && targetsAuthor && sourceInSlot;
    }

    public static int ComputeNextRoundDay(int baseDay, int intervalDays)
    {
        return baseDay + intervalDays;
    }

    public static int ComputeInitialCompressedYear(int currentDay, int daysPerYear)
    {
        return Math.Max(0, currentDay / Math.Max(1, daysPerYear) - 1);
    }

    public static WorldDiplomacyIntervalRefreshDecision EvaluateIntervalRefresh(
        WorldDiplomacyIntervalRefreshInput input)
    {
        if (input == null || input.PreviousInterval <= 0)
        {
            return new WorldDiplomacyIntervalRefreshDecision
            {
                Action = WorldDiplomacyIntervalRefreshAction.Initialize
            };
        }
        if (input.PreviousInterval == input.CurrentInterval)
        {
            return new WorldDiplomacyIntervalRefreshDecision
            {
                Action = WorldDiplomacyIntervalRefreshAction.Unchanged
            };
        }
        if (input.HasActiveRound || input.NextNormalRoundDay <= 0)
        {
            return new WorldDiplomacyIntervalRefreshDecision
            {
                Action = WorldDiplomacyIntervalRefreshAction.UpdateStampOnly
            };
        }
        int scheduleBaseDay = input.NextNormalRoundDay - input.PreviousInterval;
        return new WorldDiplomacyIntervalRefreshDecision
        {
            Action = WorldDiplomacyIntervalRefreshAction.Rebase,
            RebasedNextDay = Math.Max(input.CurrentDay, scheduleBaseDay + input.CurrentInterval)
        };
    }

    public static bool IsNormalRoundDue(int currentDay, int nextNormalRoundDay)
    {
        return currentDay >= nextNormalRoundDay;
    }

    public static int NormalizeRotationStartIndex(int rotationIndex, int candidateCount)
    {
        return candidateCount <= 0 ? 0 : Math.Abs(rotationIndex) % candidateCount;
    }

    public static int NextRotationIndex(int selectedIndex, int candidateCount)
    {
        return candidateCount <= 0 ? 0 : (selectedIndex + 1) % candidateCount;
    }

    public static bool IsExchangeReminderDue(bool reminderSent, int currentDay, int responseDueDay)
    {
        return !reminderSent && currentDay >= responseDueDay;
    }

    public static bool IsExchangeCloseDue(int currentDay, int closeDueDay)
    {
        return currentDay >= closeDueDay;
    }

    public static int ComputeSuspendedPauseDays(int currentDay, int suspendedDay)
    {
        return Math.Max(0, currentDay - suspendedDay);
    }

    public static string NormalizeRestoredExchangeState(string stateBeforeSuspension)
    {
        return string.IsNullOrWhiteSpace(stateBeforeSuspension) ? "waiting" : stateBeforeSuspension;
    }

    public static bool IsActiveRoundState(string roundState)
    {
        return string.Equals(roundState, "active", StringComparison.OrdinalIgnoreCase);
    }

    public static WorldDiplomacyRelayAdvanceAction EvaluateRelayAdvanceAction(
        bool roundStateActive, bool resultSettlementPending, bool hardEndReached)
    {
        if (!roundStateActive)
        {
            return WorldDiplomacyRelayAdvanceAction.None;
        }
        if (resultSettlementPending)
        {
            return WorldDiplomacyRelayAdvanceAction.ScheduleSettlementTurn;
        }
        return hardEndReached
            ? WorldDiplomacyRelayAdvanceAction.CloseHardEnd
            : WorldDiplomacyRelayAdvanceAction.ScheduleRelayHop;
    }

    public static bool ShouldRefreshParticipantState(
        string currentState, bool mandatoryReply, string requestedState)
    {
        return !string.Equals(currentState, "withdrawn", StringComparison.OrdinalIgnoreCase)
            || mandatoryReply
            || string.Equals(requestedState, "active", StringComparison.OrdinalIgnoreCase);
    }

    public static WorldDiplomacyRouteAdmission EvaluateRouteAdmission(
        bool relayPlanned, bool hasKingdomId, bool alreadyOnRoute, int routeCount, int participantLimit)
    {
        if (!relayPlanned || !hasKingdomId)
        {
            return WorldDiplomacyRouteAdmission.Denied;
        }
        if (alreadyOnRoute)
        {
            return WorldDiplomacyRouteAdmission.AlreadyOnRoute;
        }
        return routeCount < participantLimit
            ? WorldDiplomacyRouteAdmission.Admitted
            : WorldDiplomacyRouteAdmission.Denied;
    }

    public static bool IsAbortiveCloseReason(string closeReason)
    {
        return (closeReason ?? "").StartsWith("technical_", StringComparison.OrdinalIgnoreCase)
            || string.Equals(closeReason, "automatic_request_circuit_breaker", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOfferCooldownSkippingCloseReason(string closeReason)
    {
        string normalized = (closeReason ?? "").Trim();
        return normalized.StartsWith("technical_", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("round_plan_", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("canonical_history_migration_", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "relay_has_no_participants", StringComparison.OrdinalIgnoreCase);
    }

    public static WorldDiplomacyClosedRoundStatus EvaluateClosedRoundStatus(
        string closeReason, string currentRoundStatus, int executedActionCount, int diplomaticActionAttemptCount)
    {
        if (IsAbortiveCloseReason(closeReason))
        {
            return WorldDiplomacyClosedRoundStatus.Aborted;
        }
        if (!IsActiveRoundState(currentRoundStatus))
        {
            return WorldDiplomacyClosedRoundStatus.KeepCurrent;
        }
        if (executedActionCount > 0)
        {
            return WorldDiplomacyClosedRoundStatus.Resolved;
        }
        return diplomaticActionAttemptCount > 0
            ? WorldDiplomacyClosedRoundStatus.Deadlocked
            : WorldDiplomacyClosedRoundStatus.Closed;
    }

    public static string ClosedRoundStatusName(WorldDiplomacyClosedRoundStatus status)
    {
        switch (status)
        {
            case WorldDiplomacyClosedRoundStatus.Aborted:
                return "aborted";
            case WorldDiplomacyClosedRoundStatus.Resolved:
                return "resolved";
            case WorldDiplomacyClosedRoundStatus.Deadlocked:
                return "deadlocked";
            case WorldDiplomacyClosedRoundStatus.Closed:
                return "closed";
            default:
                return "";
        }
    }

    public static bool IsOpenLifecycleStatus(string status)
    {
        return string.Equals(status, "open", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanScheduleRelayHop(
        bool relayPlanned, bool relayWaiting, bool automaticCircuitBreakerTripped, bool roundActive)
    {
        return relayPlanned && !relayWaiting && !automaticCircuitBreakerTripped && roundActive;
    }

    public static bool HasMinimumRelayRoute(int routeCount)
    {
        return routeCount >= 2;
    }

    public static int NormalizeRelayPassDurationDays(int roundPassDurationDays, int fallbackDays)
    {
        return roundPassDurationDays > 0 ? roundPassDurationDays : fallbackDays;
    }

    public static bool IsOfferPartyInvalid(
        bool proposerResolved,
        bool targetResolved,
        bool sameParty,
        bool proposerEliminated,
        bool targetEliminated,
        bool proposerHasAuthority,
        bool targetHasAuthority)
    {
        return !proposerResolved || !targetResolved || sameParty
            || proposerEliminated || targetEliminated
            || !proposerHasAuthority || !targetHasAuthority;
    }

    public static bool IsOfferInvalidForIntent(
        string intent,
        bool atWar,
        bool allianceBehaviorAvailable,
        bool alreadyAllied,
        bool tradeBehaviorAvailable,
        bool hasTradeAgreement,
        bool peaceTermsExecutable)
    {
        switch (intent)
        {
            case "propose_peace":
                return !atWar || !peaceTermsExecutable;
            case "propose_alliance":
                return !allianceBehaviorAvailable || atWar || alreadyAllied;
            case "propose_trade":
                return !tradeBehaviorAvailable || atWar || hasTradeAgreement;
            default:
                return true;
        }
    }

    public static string ComposeOfferDeduplicationKey(string intent, string proposerKingdomId, string targetKingdomId)
    {
        return (intent ?? "") + "|" + (proposerKingdomId ?? "") + "|" + (targetKingdomId ?? "");
    }

    public static bool IsOpenOfferBetweenPair(
        string offerStatus,
        string proposerKingdomId,
        string targetKingdomId,
        string firstKingdomId,
        string secondKingdomId)
    {
        return IsOpenLifecycleStatus(offerStatus)
            && ((string.Equals(proposerKingdomId, firstKingdomId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(targetKingdomId, secondKingdomId, StringComparison.OrdinalIgnoreCase))
                || (string.Equals(proposerKingdomId, secondKingdomId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(targetKingdomId, firstKingdomId, StringComparison.OrdinalIgnoreCase)));
    }

    public static WorldDiplomacyExternalFactJoinAction EvaluateExternalFactJoin(
        WorldDiplomacyExternalFactJoinInput input)
    {
        if (input == null || !input.RoundActive)
        {
            return WorldDiplomacyExternalFactJoinAction.Rejected;
        }
        if (input.InitiatorOnRoute && input.TargetOnRoute)
        {
            return WorldDiplomacyExternalFactJoinAction.JoinViaRoutePair;
        }
        if (input.SettlementPending && input.InitiatorOnRoute && input.SettlementTargetUsable)
        {
            return WorldDiplomacyExternalFactJoinAction.JoinViaSettlementTarget;
        }
        return WorldDiplomacyExternalFactJoinAction.CheckOpenOffers;
    }

    public static bool SettlementSlotKindContains(string slotKind, string kind)
    {
        return !string.IsNullOrWhiteSpace(kind)
            && (slotKind ?? "").Split('+').Any(x => string.Equals(x, kind, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsWarResponseSlotAuthorized(
        bool resultSettlementPending,
        bool authorResolved,
        bool targetResolved,
        bool roundActive,
        bool hasSlotId,
        bool slotIdMatchesCurrent)
    {
        return resultSettlementPending && authorResolved && targetResolved
            && roundActive && hasSlotId && slotIdMatchesCurrent;
    }

    public static bool IsWarDeclarationAction(
        bool changedDiplomaticState, string intent, string actionTargetKingdomId, string authorKingdomId)
    {
        return changedDiplomaticState
            && string.Equals(intent, "declare_war", StringComparison.Ordinal)
            && string.Equals(actionTargetKingdomId, authorKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsWarDeclarationDocument(
        bool hasActions, bool changedDiplomaticState, string intent, string targetKingdomId, string authorKingdomId)
    {
        return !hasActions
            && IsWarDeclarationAction(changedDiplomaticState, intent, targetKingdomId, authorKingdomId);
    }

    public static bool IsSettlementTargetUsable(
        bool resultSettlementPending,
        bool targetResolved,
        bool sameParty,
        bool targetEliminated,
        bool targetHasAuthority,
        bool targetOnRoute,
        int routeCount,
        int maxParticipants)
    {
        if (!resultSettlementPending || !targetResolved || sameParty
            || targetEliminated || !targetHasAuthority)
        {
            return false;
        }
        return targetOnRoute || routeCount < maxParticipants;
    }

    public static WorldDiplomacyRouteAdmission EvaluateSettlementTargetAdmission(
        bool resultSettlementPending,
        bool hasKingdomId,
        bool alreadyOnRoute,
        bool kingdomEligible,
        int routeCount,
        int maxParticipants)
    {
        if (!resultSettlementPending || !hasKingdomId)
        {
            return WorldDiplomacyRouteAdmission.Denied;
        }
        if (alreadyOnRoute)
        {
            return WorldDiplomacyRouteAdmission.AlreadyOnRoute;
        }
        return kingdomEligible && routeCount < maxParticipants
            ? WorldDiplomacyRouteAdmission.Admitted
            : WorldDiplomacyRouteAdmission.Denied;
    }

    public static bool IsNoActionAuthorizationEligible(
        bool authorResolved,
        bool targetResolved,
        bool sameParty,
        bool authorIsPlayer,
        bool authorEliminated,
        bool targetEliminated,
        bool authorHasAuthority,
        bool targetHasAuthority,
        bool roundActive)
    {
        return authorResolved && targetResolved && !sameParty && !authorIsPlayer
            && !authorEliminated && !targetEliminated
            && authorHasAuthority && targetHasAuthority && roundActive;
    }

    public static bool EvaluateExternalNoActionAuthorization(
        WorldDiplomacyExternalNoActionInput input)
    {
        if (input == null || !IsNoActionAuthorizationEligible(
                input.AuthorResolved, input.TargetResolved, input.SameParty,
                input.AuthorIsPlayer, input.AuthorEliminated, input.TargetEliminated,
                input.AuthorHasAuthority, input.TargetHasAuthority, input.RoundActive))
        {
            return false;
        }
        if (!input.RootReady || !input.RootActionable)
        {
            return false;
        }
        if (!input.ResponseReady || !input.ResponsePlayerAuthored || !input.ResponseHasDocumentId
            || !input.ResponseSameRound || !input.ResponseAuthoredByTarget)
        {
            return false;
        }
        bool isDirectlyAddressed = input.IsPrimaryTarget || input.IsRepresentativeTarget || input.IsInAddressedList;
        if (!isDirectlyAddressed
            || (!input.IsPrimaryTarget && !input.IsRepresentativeTarget && !input.ResponseRequiresResponse)
            || !input.MandatoryReplyPending || !input.LastTriggeredMatches)
        {
            return false;
        }
        if (input.SettlementPending)
        {
            return false;
        }
        if (!input.RelayPlanned)
        {
            return !input.IsRelayTurn;
        }
        return input.IsRelayTurn && input.AuthorOnRoute && input.TargetOnRoute;
    }

    public static bool EvaluateRelayNoActionAuthorization(
        WorldDiplomacyRelayNoActionInput input)
    {
        if (input == null || !IsNoActionAuthorizationEligible(
                input.AuthorResolved, input.TargetResolved, input.SameParty,
                input.AuthorIsPlayer, input.AuthorEliminated, input.TargetEliminated,
                input.AuthorHasAuthority, input.TargetHasAuthority, input.RoundActive))
        {
            return false;
        }
        if (!input.RootReady || !input.RootActionable || !input.IsRelayTurn)
        {
            return false;
        }
        if (input.SettlementPending)
        {
            if (!input.HasSlotId || !input.SlotIdIsCurrent || !input.SettlementTargetUsable || !input.SlotFound)
            {
                return false;
            }
            return input.SlotHasRelatedKingdom ? input.TargetInRelatedKingdoms : input.TargetOnRoute;
        }
        if (!input.RelayPlanned || !input.RelayWaiting || input.HasSlotId
            || !input.AuthorOnRoute || !input.TargetOnRoute)
        {
            return false;
        }
        return input.AuthorIsCurrentCursor;
    }

    public static bool IsResponseRequiredFrom(WorldDiplomacyDocument document, string kingdomId)
    {
        if (document == null || string.IsNullOrWhiteSpace(kingdomId)) return false;
        if (document.Actions?.Count > 0)
        {
            return document.Actions.Any(x => x != null && x.RequiresResponse
                && string.Equals(x.TargetKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
        }
        return document.RequiresResponse;
    }

    public static List<string> CollectDocumentTargetIds(WorldDiplomacyDocument document, bool changedOnly)
    {
        if (document == null) return new List<string>();
        if (document.Actions?.Count > 0)
        {
            return document.Actions
                .Where(x => x != null && (!changedOnly || x.ChangedDiplomaticState))
                .Select(x => x.TargetKingdomId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        IEnumerable<string> targets = string.IsNullOrWhiteSpace(document.TargetKingdomId)
            ? Enumerable.Empty<string>()
            : new[] { document.TargetKingdomId };
        if (!changedOnly) targets = targets.Concat(document.AddressedKingdomIds ?? new List<string>());
        return targets.Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static bool DocumentRespondsToSource(WorldDiplomacyDocument document, string sourceDocumentId)
    {
        if (document == null || string.IsNullOrWhiteSpace(sourceDocumentId)) return false;
        if (string.Equals(document.SourceDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(document.RespondingToOfferDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(document.RespondingToThreatDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase)) return true;
        return document.Actions?.Any(x => x != null
            && (string.Equals(x.RespondingToOfferDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.RespondingToThreatDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase))) == true;
    }

    public static WorldDiplomacyMandatoryReplyAction EvaluateMandatoryReplyAction(
        WorldDiplomacyMandatoryReplyInput input)
    {
        if (input == null
            || !input.RoundResolved || !input.ParticipantResolved || !input.ReceiverResolved
            || !input.TriggerResolved || input.ReceiverIsPlayer || !input.ReceiverHasAuthority
            || !input.TriggerPlayerAuthored
            || (!input.IsPrimaryTarget && !input.RepresentativeForAddressedVassal
                && !input.ResponseRequiredFrom))
        {
            return WorldDiplomacyMandatoryReplyAction.Ineligible;
        }
        if (input.AlreadyResponded)
        {
            return WorldDiplomacyMandatoryReplyAction.AlreadyResponded;
        }
        if (input.AuthorBlocked)
        {
            return WorldDiplomacyMandatoryReplyAction.AuthorBlocked;
        }
        if (input.SettlementPending)
        {
            return WorldDiplomacyMandatoryReplyAction.SettlementOwned;
        }
        if (input.JobAlreadyQueued)
        {
            return WorldDiplomacyMandatoryReplyAction.JobQueued;
        }
        if (input.ExistingResponses + input.QueuedResponses >= input.MaxPriorityResponses)
        {
            return WorldDiplomacyMandatoryReplyAction.ResponseCapReached;
        }
        return WorldDiplomacyMandatoryReplyAction.Schedule;
    }

    public static bool IsValidatedSubstantiveProgress(
        WorldDiplomacyDocument document,
        List<WorldDiplomacyRoundOffer> pendingOffers,
        List<WorldDiplomacyThreat> diplomaticThreats,
        bool successfulMechanicalAction)
    {
        if (document == null) return false;
        if (successfulMechanicalAction) return true;
        if (document.Actions?.Count > 0)
        {
            if ((diplomaticThreats ?? new List<WorldDiplomacyThreat>()).Any(x => x != null
                && string.Equals(x.StageDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase))) return true;
            if ((pendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
                && string.Equals(x.SourceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase))) return true;
            foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null))
            {
                string proposal = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(action.Intent);
                if (string.IsNullOrWhiteSpace(proposal)) continue;
                string expected = action.Intent.StartsWith("accept_", StringComparison.OrdinalIgnoreCase) ? "accepted" : "rejected";
                if ((pendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
                    && string.Equals(x.SourceDocumentId, action.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(action.RespondingToOfferActionId)
                        || string.Equals(x.SourceActionId, action.RespondingToOfferActionId, StringComparison.OrdinalIgnoreCase))
                    && string.Equals(x.Status, expected, StringComparison.OrdinalIgnoreCase))) return true;
            }
            return false;
        }
        string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent);
        if (intent == "warning" || intent == "ultimatum")
        {
            return (diplomaticThreats ?? new List<WorldDiplomacyThreat>()).Any(x => x != null
                && string.Equals(x.StageDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase));
        }
        if (intent == "apology" || intent == "concession")
        {
            return !string.IsNullOrWhiteSpace(document.TargetKingdomId)
                && !string.Equals(document.AuthorKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase);
        }
        if (WorldDiplomacyIntentVocabulary.IsProposalIntent(intent))
        {
            return (pendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
                && string.Equals(x.SourceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ProposerKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase));
        }
        string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent);
        if (string.IsNullOrWhiteSpace(proposalIntent)) return false;
        string expectedStatus = intent.StartsWith("accept_", StringComparison.OrdinalIgnoreCase) ? "accepted" : "rejected";
        return (pendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Any(x => x != null
            && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(document.TargetKingdomId)
                || string.Equals(x.ProposerKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId)
                || string.Equals(x.SourceDocumentId, document.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase))
            && string.Equals(x.Status, expectedStatus, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsValidatedDiplomaticActionAttempt(
        WorldDiplomacyDocument document,
        List<WorldDiplomacyRoundOffer> pendingOffers,
        List<WorldDiplomacyThreat> diplomaticThreats,
        bool successfulMechanicalAction)
    {
        if (document == null) return false;
        if (successfulMechanicalAction) return true;
        string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent);
        if (!WorldDiplomacyIntentVocabulary.IsRoundDiplomaticBehaviorIntent(intent)) return false;
        return IsValidatedSubstantiveProgress(document, pendingOffers, diplomaticThreats, successfulMechanicalAction: false);
    }

    public static bool NeedsCanonicalHistoryRetry(WorldDiplomacyDocument document)
    {
        if (document == null || !document.IsReadyForPublication || string.IsNullOrWhiteSpace(document.DocumentId)) return false;
        bool externalResolvedFact = string.Equals(document.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase);
        bool declarationPending = !externalResolvedFact
            && !document.HistoryDeclarationRecorded
            && !string.IsNullOrWhiteSpace(document.Body);
        bool resultPending = !document.HistoryResultRecorded
            && (document.ChangedDiplomaticState || externalResolvedFact)
            && !string.IsNullOrWhiteSpace(document.MechanicalResult);
        return declarationPending || resultPending;
    }

    public static int NextDeferredRetryAttempt(int attempts)
    {
        return Math.Min(30, attempts + 1);
    }

    public static int ComputeDeferredRetryDelayHours(int attempts)
    {
        return Math.Min(24, 1 << Math.Min(4, Math.Max(0, attempts - 1)));
    }

    public static bool IsDeferredRetryDue(int currentHour, int retryAfterHour)
    {
        return currentHour >= retryAfterHour;
    }

    public static int ComputeDeferredRetryBatchSize(int maxAttempts, int queueCount)
    {
        return Math.Min(Math.Max(0, maxAttempts), queueCount);
    }

    public static string NormalizeDeferredRetryDocumentId(string documentId)
    {
        return (documentId ?? "").Trim();
    }
    public static bool CanonicalDeltaContainsSourceKey(HashSet<string> sourceKeys, string sourceKey)
    {
        return !string.IsNullOrWhiteSpace(sourceKey) && (sourceKeys?.Contains(sourceKey) ?? false);
    }

    public static long EstimateHistoryTokens(string text, Func<string, int> estimateTokens)
    {
        return Math.Max(0L, estimateTokens?.Invoke(text ?? "") ?? 0);
    }

    public static void EnqueueDeferredCanonicalHistoryRetry(
        HashSet<string> documentIdSet, Queue<string> documentIds, string documentId)
    {
        string normalizedId = WorldDiplomacyRoundLifecycleRules.NormalizeDeferredRetryDocumentId(documentId);
        if (normalizedId.Length == 0 || documentIdSet == null || documentIds == null) return;
        if (!documentIdSet.Add(normalizedId)) return;
        documentIds.Enqueue(normalizedId);
    }

    public static void ScheduleDeferredCanonicalHistoryRetry(
        Dictionary<string, int> retryAttempts,
        Dictionary<string, int> retryAfterHours,
        HashSet<string> documentIdSet,
        Queue<string> documentIds,
        string documentId,
        int currentHour)
    {
        string normalizedId = WorldDiplomacyRoundLifecycleRules.NormalizeDeferredRetryDocumentId(documentId);
        if (normalizedId.Length == 0 || retryAttempts == null || retryAfterHours == null) return;
        retryAttempts.TryGetValue(normalizedId, out int attempts);
        attempts = WorldDiplomacyRoundLifecycleRules.NextDeferredRetryAttempt(attempts);
        retryAttempts[normalizedId] = attempts;
        int delayHours = WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryDelayHours(attempts);
        retryAfterHours[normalizedId] = currentHour + delayHours;
        WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(documentIdSet, documentIds, normalizedId);
    }

    public static void RetryDeferredCanonicalHistoryEntries(
        Queue<string> documentIds,
        HashSet<string> documentIdSet,
        Dictionary<string, int> retryAttempts,
        Dictionary<string, int> retryAfterHours,
        List<WorldDiplomacyThreat> threats,
        int currentHour,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Action<WorldDiplomacyDocument> appendDocumentEvents,
        Action<WorldDiplomacyThreat> appendThreatHistoryResult,
        Action<WorldDiplomacyThreat> appendThreatDomesticPenaltyResult,
        Action<WorldDiplomacyThreat> appendThreatIssuerRewardResult,
        Action<WorldDiplomacyThreat, WorldDiplomacyThreatNonComplianceEvent> appendNonComplianceResult,
        Action<string> log,
        int maxAttempts = 16)
    {
        if (documentIds == null || documentIdSet == null) return;
        int attempts = WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryBatchSize(maxAttempts, documentIds.Count);
        for (int i = 0; i < attempts; i++)
        {
            string documentId = documentIds.Dequeue();
            documentIdSet.Remove(documentId);
            WorldDiplomacyDocument document = resolveDocument?.Invoke(documentId);
            if (!WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry(document))
            {
                retryAttempts?.Remove(documentId);
                retryAfterHours?.Remove(documentId);
                continue;
            }
            if (retryAfterHours != null
                && retryAfterHours.TryGetValue(documentId, out int retryAfterHour)
                && !WorldDiplomacyRoundLifecycleRules.IsDeferredRetryDue(currentHour, retryAfterHour))
            {
                WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(documentIdSet, documentIds, documentId);
                continue;
            }
            try
            {
                appendDocumentEvents?.Invoke(document);
                WorldDiplomacyRoundLifecycleRules.FinalizeDiplomaticThreatHistoryAfterDocument(document, threats,
                    appendThreatHistoryResult, appendThreatDomesticPenaltyResult, appendThreatIssuerRewardResult);
                WorldDiplomacyRoundLifecycleRules.FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(document,
                    threats, appendNonComplianceResult);
            }
            catch (Exception ex)
            {
                WorldDiplomacyRoundLifecycleRules.ScheduleDeferredCanonicalHistoryRetry(
                    retryAttempts, retryAfterHours, documentIdSet, documentIds, documentId, currentHour);
                log?.Invoke("deferred canonical history retry failed document=" + documentId + " error=" + ex.Message);
                continue;
            }
            if (WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry(document))
            {
                WorldDiplomacyRoundLifecycleRules.ScheduleDeferredCanonicalHistoryRetry(
                    retryAttempts, retryAfterHours, documentIdSet, documentIds, documentId, currentHour);
            }
            else
            {
                retryAttempts?.Remove(documentId);
                retryAfterHours?.Remove(documentId);
            }
        }
    }



    public static bool HasStaleDiplomaticActionPresentation(
        WorldDiplomacyJob job, Func<WorldDiplomacyJob, string> buildLegalActionSignature)
    {
        if (job == null || !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return false;
        return !string.Equals(
            job.PresentedLegalActionSignature ?? "",
            buildLegalActionSignature?.Invoke(job),
            StringComparison.Ordinal);
    }

    public static int GetRoundParticipantLimit(int activityLevel, int maxRelayParticipants)
    {
        return Math.Min(maxRelayParticipants, activityLevel switch
        {
            0 => 2,
            2 => 5,
            _ => 3
        });
    }

    public static bool IsOpenDiplomaticThreatStatus(string status)
    {
        return string.Equals(status, "open", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRetainedActiveDiplomaticThreatStatus(string status)
    {
        return IsOpenDiplomaticThreatStatus(status)
            || string.Equals(status, "compliance_pending", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsThreatRelevantToResultSettlement(
        WorldDiplomacyThreat threat, string roundId)
    {
        return threat != null && !string.IsNullOrWhiteSpace(roundId)
            && IsOpenDiplomaticThreatStatus(threat.Status)
            && (string.Equals(threat.StageRoundId, roundId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(threat.TargetDecisionRoundId, roundId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(threat.ObligationRoundId, roundId, StringComparison.OrdinalIgnoreCase));
    }

    public static WorldDiplomacyThreatCloseDecision EvaluateThreatTerminalClose(
        WorldDiplomacyDocument document,
        List<WorldDiplomacyThreat> diplomaticThreats)
    {
        WorldDiplomacyThreatCloseDecision decision = new WorldDiplomacyThreatCloseDecision();
        if (document == null) return decision;
        List<WorldDiplomacyThreat> linked = (diplomaticThreats ?? new List<WorldDiplomacyThreat>())
            .Where(x => x != null
                && (string.Equals(x.ComplianceDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.ResolutionDocumentId, document.DocumentId, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (linked.Any(x => string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)))
        {
            decision.Resolved = true;
            decision.Reason = "threat_target_complied";
            return decision;
        }
        if (linked.Any(x => string.Equals(x.Status, "enforced", StringComparison.OrdinalIgnoreCase)))
        {
            decision.Resolved = true;
            decision.Reason = "threat_followed_by_war";
            return decision;
        }
        if (!linked.Any(x => string.Equals(x.Status, "breached", StringComparison.OrdinalIgnoreCase))) return decision;
        bool successfulWar = document.Actions?.Any(x => x != null && x.ChangedDiplomaticState
            && WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent) == "declare_war") == true
            || (document.ChangedDiplomaticState
                && WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent) == "declare_war");
        if (successfulWar)
        {
            decision.Resolved = true;
            decision.Reason = "threat_warning_skipped_but_war_started";
            return decision;
        }
        decision.Reason = "threat_next_declaration_breached";
        return decision;
    }

    public static WorldDiplomacyConfirmedRoundResult EvaluateConfirmedRoundResult(
        WorldDiplomacyDocument document,
        List<WorldDiplomacyRoundOffer> pendingOffers,
        List<WorldDiplomacyThreat> diplomaticThreats)
    {
        WorldDiplomacyConfirmedRoundResult result = new WorldDiplomacyConfirmedRoundResult();
        if (document == null) return result;
        WorldDiplomacyThreatCloseDecision threatClose = EvaluateThreatTerminalClose(document, diplomaticThreats);
        if (!string.IsNullOrWhiteSpace(threatClose.Reason))
        {
            result.Confirmed = true;
            result.CloseReason = threatClose.Reason;
            result.RoundStatus = threatClose.Resolved ? "resolved" : "deadlocked";
            return result;
        }
        if (document.Actions?.Count > 0)
        {
            List<string> confirmedReasons = new List<string>();
            foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null))
            {
                string actionIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent);
                string proposal = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(actionIntent);
                WorldDiplomacyRoundOffer offer = string.IsNullOrWhiteSpace(proposal)
                    ? null
                    : (pendingOffers ?? new List<WorldDiplomacyRoundOffer>()).FirstOrDefault(x => x != null
                        && string.Equals(x.SourceDocumentId, action.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrWhiteSpace(action.RespondingToOfferActionId)
                            || string.Equals(x.SourceActionId, action.RespondingToOfferActionId, StringComparison.OrdinalIgnoreCase)));
                WorldDiplomacyConfirmedResultKind kind = WorldDiplomacyResultSettlementRules.EvaluateConfirmedResult(
                    new WorldDiplomacyResultObservation(actionIntent, action.ChangedDiplomaticState,
                        offer != null, offer?.Status, linkedThreatStatus: "", isExternallyResolvedFact: false));
                if (!WorldDiplomacyResultSettlementRules.IsConfirmedResult(kind)
                    && !(actionIntent == "comply_ultimatum" && action.ChangedDiplomaticState)) continue;
                string actionReason = kind switch
                {
                    WorldDiplomacyConfirmedResultKind.OfferAccepted => "offer_accepted",
                    WorldDiplomacyConfirmedResultKind.OfferRejected => "offer_rejected",
                    _ => actionIntent switch
                    {
                        "declare_war" => "war_declared",
                        "break_alliance" => "alliance_broken",
                        "cancel_trade" => "trade_cancelled",
                        "comply_ultimatum" => "threat_target_complied",
                        "accept_peace" or "accept_alliance" or "accept_trade" => "offer_accepted",
                        _ => "diplomatic_result"
                    }
                };
                if (!confirmedReasons.Contains(actionReason, StringComparer.OrdinalIgnoreCase)) confirmedReasons.Add(actionReason);
            }
            if (confirmedReasons.Count == 0) return result;
            result.Confirmed = true;
            result.CloseReason = confirmedReasons.Count == 1 ? confirmedReasons[0] : "multiple_diplomatic_results";
            result.RoundStatus = "resolved";
            return result;
        }

        string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent);
        string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent);
        WorldDiplomacyRoundOffer matchedOffer = string.IsNullOrWhiteSpace(proposalIntent)
            ? null
            : (pendingOffers ?? new List<WorldDiplomacyRoundOffer>())
                .FirstOrDefault(x => x != null
                    && string.Equals(x.SourceDocumentId, document.RespondingToOfferDocumentId, StringComparison.OrdinalIgnoreCase));
        WorldDiplomacyConfirmedResultKind resultKind = WorldDiplomacyResultSettlementRules.EvaluateConfirmedResult(
            new WorldDiplomacyResultObservation(
                intent,
                document.ChangedDiplomaticState,
                matchedOffer != null,
                matchedOffer?.Status,
                linkedThreatStatus: "",
                isExternallyResolvedFact: string.Equals(document.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase)));
        if (!WorldDiplomacyResultSettlementRules.IsConfirmedResult(resultKind))
        {
            if (intent == "comply_ultimatum" && document.ChangedDiplomaticState)
            {
                result.Confirmed = true;
                result.CloseReason = "threat_target_complied";
                return result;
            }
            return result;
        }
        if (resultKind == WorldDiplomacyConfirmedResultKind.OfferAccepted)
        {
            result.Confirmed = true;
            result.CloseReason = "offer_accepted";
            return result;
        }
        if (resultKind == WorldDiplomacyConfirmedResultKind.OfferRejected)
        {
            result.Confirmed = true;
            result.CloseReason = "offer_rejected";
            return result;
        }
        result.CloseReason = intent switch
        {
            "declare_war" => "war_declared",
            "break_alliance" => "alliance_broken",
            "cancel_trade" => "trade_cancelled",
            "accept_peace" or "accept_alliance" or "accept_trade" => "offer_accepted",
            _ => ""
        };
        result.Confirmed = !string.IsNullOrWhiteSpace(result.CloseReason);
        return result;
    }

    public static bool IsThreatIntent(string normalizedIntent)
    {
        return normalizedIntent == "warning" || normalizedIntent == "ultimatum" || normalizedIntent == "comply_ultimatum";
    }

    public static string EvaluateThreatPartyEligibility(bool hasAuthor, bool hasTarget, bool sameParty)
    {
        return !hasAuthor || !hasTarget || sameParty ? "threat_action_has_no_eligible_parties" : "";
    }

    public static string EvaluateComplyUltimatumViolation(
        bool atWar,
        WorldDiplomacyThreat incomingThreat,
        string claimedThreatDocumentId)
    {
        if (atWar) return "comply_ultimatum_after_war_started";
        if (incomingThreat == null) return "comply_ultimatum_without_open_threat";
        if (!string.Equals(incomingThreat.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase))
        {
            return "comply_ultimatum_after_target_decision";
        }
        if (string.IsNullOrWhiteSpace(claimedThreatDocumentId)
            || !string.Equals(incomingThreat.StageDocumentId, claimedThreatDocumentId, StringComparison.OrdinalIgnoreCase))
        {
            return "comply_ultimatum_source_mismatch";
        }
        return "";
    }

    public static string EvaluateThreatEscalationViolation(
        string normalizedIntent,
        bool atWar,
        bool canEnforce,
        string enforcementBlockReason,
        WorldDiplomacyThreat outboundThreat,
        string targetKingdomId)
    {
        if (atWar) return "threat_intent_between_kingdoms_already_at_war";
        if (!canEnforce) return "threat_cannot_be_enforced:" + enforcementBlockReason;
        if (normalizedIntent == "warning")
        {
            return outboundThreat != null ? "issuer_already_has_open_threat" : "";
        }
        if (outboundThreat == null) return "";
        if (!string.Equals(outboundThreat.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase))
        {
            return "issuer_open_threat_targets_another_kingdom";
        }
        if (!string.Equals(outboundThreat.Stage, "warning", StringComparison.OrdinalIgnoreCase))
        {
            return "duplicate_open_ultimatum";
        }
        if (!string.Equals(outboundThreat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase))
        {
            return "warning_escalation_requires_target_noncompliance";
        }
        return "";
    }

    public static bool TryResolveUniqueOpenProposal(
        List<WorldDiplomacyRoundOffer> pendingOffers,
        string responderKingdomId,
        string proposerKingdomId,
        string proposalIntent,
        out string sourceDocumentId,
        out string sourceActionId)
    {
        sourceDocumentId = "";
        sourceActionId = "";
        if (string.IsNullOrWhiteSpace(responderKingdomId)
            || string.IsNullOrWhiteSpace(proposerKingdomId)
            || !WorldDiplomacyIntentVocabulary.IsProposalIntent(proposalIntent)) return false;
        List<WorldDiplomacyRoundOffer> matches = (pendingOffers ?? new List<WorldDiplomacyRoundOffer>())
            .Where(x => x != null
                && string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase)
                && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ProposerKingdomId, proposerKingdomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.TargetKingdomId, responderKingdomId, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(x.SourceDocumentId))
            .GroupBy(x => (x.SourceDocumentId ?? "") + "\n" + (x.SourceActionId ?? ""), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(2)
            .ToList();
        if (matches.Count != 1) return false;
        sourceDocumentId = matches[0].SourceDocumentId ?? "";
        sourceActionId = matches[0].SourceActionId ?? "";
        return true;
    }

    public static string MergeSettlementSlotKind(string existingKind, string addedKind)
    {
        if (string.IsNullOrWhiteSpace(addedKind)) return existingKind ?? "";
        if (string.IsNullOrWhiteSpace(existingKind)) return addedKind;
        return SettlementSlotKindContains(existingKind, addedKind)
            ? existingKind
            : existingKind + "+" + addedKind;
    }

    public static string DefaultSettlementSlotKind(string kind)
    {
        return string.IsNullOrWhiteSpace(kind) ? "route" : kind;
    }

    public static HashSet<string> CollectSpokenAuthorIds(
        List<WorldDiplomacyDocument> documents,
        string roundId)
    {
        return new HashSet<string>((documents ?? new List<WorldDiplomacyDocument>())
            .Where(x => x != null && x.IsReadyForPublication
                && string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.AuthorKingdomId)
            .Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
    }

    public static WorldDiplomacyThreatSettlementSlotDecision EvaluateThreatSettlementSlot(
        WorldDiplomacyThreat threat)
    {
        WorldDiplomacyThreatSettlementSlotDecision decision = new WorldDiplomacyThreatSettlementSlotDecision();
        if (threat == null) return decision;
        if (string.Equals(threat.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase))
        {
            decision.Applies = true;
            decision.KingdomId = threat.TargetKingdomId;
            decision.Kind = "threat_response";
            decision.SourceDocumentId = threat.StageDocumentId;
            decision.RelatedKingdomId = threat.IssuerKingdomId;
            return decision;
        }
        if (string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase))
        {
            decision.Applies = true;
            decision.KingdomId = threat.IssuerKingdomId;
            decision.Kind = "threat_followthrough";
            decision.SourceDocumentId = threat.StageDocumentId;
            decision.RelatedKingdomId = threat.TargetKingdomId;
        }
        return decision;
    }

    public static bool IsWarResponseSlotAction(
        bool changedDiplomaticState, string intent, string targetKingdomId)
    {
        return changedDiplomaticState
            && string.Equals(intent, "declare_war", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(targetKingdomId);
    }

    public static string ComposeWarResponseActionKey(string documentId, string actionId)
    {
        return (documentId ?? "") + "#" + (actionId ?? "");
    }

    public static WorldDiplomacyResultSettlementSlot SelectSettlementSlot(
        List<WorldDiplomacyResultSettlementSlot> slots,
        string slotId,
        string kingdomId)
    {
        return (slots ?? new List<WorldDiplomacyResultSettlementSlot>()).FirstOrDefault(x => x != null
            && ((!string.IsNullOrWhiteSpace(slotId)
                    && string.Equals(x.SlotId, slotId, StringComparison.OrdinalIgnoreCase))
                || (string.IsNullOrWhiteSpace(slotId)
                    && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase))));
    }

    public static WorldDiplomacyResultSettlementSlot SelectSettlementSlotBySlotAndKingdom(
        List<WorldDiplomacyResultSettlementSlot> slots,
        string slotId,
        string kingdomId)
    {
        return (slots ?? new List<WorldDiplomacyResultSettlementSlot>()).FirstOrDefault(x => x != null
            && string.Equals(x.SlotId, slotId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsThreatRequiredSpeaker(WorldDiplomacyThreat threat, string kingdomId)
    {
        return !string.IsNullOrWhiteSpace(kingdomId)
            && string.Equals(ResolveThreatRequiredSpeakerId(threat), kingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static int CountSuccessfulMechanicalActions(WorldDiplomacyDocument document)
    {
        if (document == null) return 0;
        return document.Actions?.Count(x => x != null && x.ChangedDiplomaticState)
            ?? (document.ChangedDiplomaticState ? 1 : 0);
    }

    public static bool IsRootRoundDocument(string rootDocumentId, string documentId)
    {
        return string.IsNullOrWhiteSpace(rootDocumentId)
            || string.Equals(rootDocumentId, documentId, StringComparison.OrdinalIgnoreCase);
    }

    public static int ComputeDiplomaticActionAttemptCount(
        List<WorldDiplomacyDocumentAction> actions,
        bool substantiveProgress,
        bool validatedSingleAttempt)
    {
        if (actions == null) return validatedSingleAttempt ? 1 : 0;
        if (!substantiveProgress) return 0;
        return actions.Count(x => x != null
            && WorldDiplomacyIntentVocabulary.IsRoundDiplomaticBehaviorIntent(x.Intent));
    }

    public static bool ShouldExtendHardEndForLateProposal(
        int consecutiveNoActionPasses,
        List<WorldDiplomacyDocumentAction> actions,
        string documentIntent)
    {
        return ShouldForceTerminalMove(consecutiveNoActionPasses)
            && (actions?.Any(x => x != null
                    && WorldDiplomacyIntentVocabulary.IsProposalIntent(x.Intent)) == true
                || WorldDiplomacyIntentVocabulary.IsProposalIntent(documentIntent));
    }

    public static string ResolveTerminalMoveCloseReason(string roundStatus)
    {
        return string.Equals(roundStatus, "deadlocked", StringComparison.OrdinalIgnoreCase)
            ? "negotiation_declared_deadlock" : "negotiation_ended";
    }

    public static bool IsRelayResolvedClose(string documentRoundStatus, int executedActionCount)
    {
        return string.Equals(documentRoundStatus, "resolved", StringComparison.OrdinalIgnoreCase)
            && executedActionCount > 0;
    }

    public static string SelectLastPublishedAuthorId(
        List<WorldDiplomacyDocument> documents,
        string roundId)
    {
        return OrderDocumentsByRecency((documents ?? new List<WorldDiplomacyDocument>())
            .Where(x => x != null && x.IsReadyForPublication
                && string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.AuthorKingdomId).FirstOrDefault();
    }

    public static WorldDiplomacyRoundParticipant SelectParticipantByKingdom(
        List<WorldDiplomacyRoundParticipant> participants,
        string kingdomId)
    {
        return (participants ?? new List<WorldDiplomacyRoundParticipant>())
            .FirstOrDefault(x => x != null
                && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
    }

    public static WorldDiplomacyThreatDocumentDispatch EvaluateThreatDocumentDispatch(
        string normalizedIntent,
        bool hasTarget,
        bool sameParty,
        bool changedDiplomaticState)
    {
        if (!hasTarget || sameParty) return WorldDiplomacyThreatDocumentDispatch.None;
        if (normalizedIntent == "warning" || normalizedIntent == "ultimatum")
        {
            return WorldDiplomacyThreatDocumentDispatch.RegisterOrAdvance;
        }
        if (normalizedIntent == "comply_ultimatum")
        {
            return WorldDiplomacyThreatDocumentDispatch.ResolveCompliance;
        }
        if (normalizedIntent != "declare_war" || !changedDiplomaticState)
        {
            return WorldDiplomacyThreatDocumentDispatch.None;
        }
        return WorldDiplomacyThreatDocumentDispatch.ProcessWarEnforcement;
    }

    public static bool IsThreatComplianceAction(
        WorldDiplomacyDocumentAction action,
        WorldDiplomacyThreat threat)
    {
        if (action == null || threat == null) return false;
        return string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent), "comply_ultimatum", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action.TargetKingdomId, threat.IssuerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(action.RespondingToThreatDocumentId, threat.StageDocumentId, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(threat.StageActionId)
                ? string.IsNullOrWhiteSpace(action.RespondingToThreatActionId)
                : string.Equals(action.RespondingToThreatActionId, threat.StageActionId, StringComparison.OrdinalIgnoreCase));
    }

    public static WorldDiplomacyDocumentAction SelectThreatDecisionAction(
        List<WorldDiplomacyDocumentAction> actions,
        string actionTargetKingdomId)
    {
        return (actions ?? new List<WorldDiplomacyDocumentAction>())
            .FirstOrDefault(x => x != null
                && string.Equals(x.TargetKingdomId, actionTargetKingdomId, StringComparison.OrdinalIgnoreCase));
    }

    public static WorldDiplomacyThreat SelectEnforceableUltimatumThreat(
        List<WorldDiplomacyThreat> threats,
        string issuerKingdomId,
        string targetKingdomId)
    {
        return (threats ?? new List<WorldDiplomacyThreat>())
            .Where(x => IsRejectedUltimatumEnforceable(x)
                && string.Equals(x.IssuerKingdomId, issuerKingdomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
                && IsOpenDiplomaticThreatStatus(x.Status))
            .OrderByDescending(x => x.UpdatedDay).ThenByDescending(x => x.CreatedDay)
            .FirstOrDefault();
    }

    public static string ResolveThreatDecisionSourceDocumentId(
        string normalizedIntent,
        string respondingToThreatDocumentId)
    {
        return string.Equals(normalizedIntent, "comply_ultimatum", StringComparison.OrdinalIgnoreCase)
            ? (respondingToThreatDocumentId ?? "")
            : "";
    }

    public static WorldDiplomacyThreat SelectUniquePresentedPendingThreat(
        List<WorldDiplomacyThreat> threats,
        string issuerKingdomId,
        string targetKingdomId,
        HashSet<string> presentedStageDocumentIds)
    {
        List<WorldDiplomacyThreat> matches = (threats ?? new List<WorldDiplomacyThreat>())
            .Where(x => x != null
                && IsOpenDiplomaticThreatStatus(x.Status)
                && string.Equals(x.IssuerKingdomId, issuerKingdomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(x.StageDocumentId)
                && (presentedStageDocumentIds?.Contains(x.StageDocumentId) ?? false))
            .GroupBy(x => (x.StageDocumentId ?? "") + "\n" + (x.StageActionId ?? ""), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(2)
            .ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    public static WorldDiplomacyThreat SelectOpenThreatIssuedBy(
        List<WorldDiplomacyThreat> threats,
        string issuerKingdomId)
    {
        string issuerId = (issuerKingdomId ?? "").Trim();
        if (issuerId.Length == 0) return null;
        return (threats ?? new List<WorldDiplomacyThreat>()).FirstOrDefault(x => x != null
            && IsOpenDiplomaticThreatStatus(x.Status)
            && string.Equals(x.IssuerKingdomId, issuerId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsThreatTargetDecisionCandidate(
        WorldDiplomacyThreat threat,
        string targetKingdomId,
        HashSet<string> presentedStageDocumentIds)
    {
        return threat != null
            && IsOpenDiplomaticThreatStatus(threat.Status)
            && string.Equals(threat.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
            && (presentedStageDocumentIds?.Contains(threat.StageDocumentId) ?? false);
    }

    public static bool IsThreatRegistrationIdentityMatch(
        string normalizedStage,
        string documentIntent,
        string documentAuthorKingdomId,
        string documentTargetKingdomId,
        string issuerKingdomId,
        string targetKingdomId)
    {
        if (normalizedStage is not "warning" and not "ultimatum") return false;
        return string.Equals(documentIntent, normalizedStage, StringComparison.Ordinal)
            && string.Equals(documentAuthorKingdomId, issuerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(documentTargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static WorldDiplomacyThreatRegistrationDecision EvaluateThreatRegistration(
        WorldDiplomacyThreat existing,
        string normalizedStage,
        string documentId,
        string targetKingdomId)
    {
        if (existing != null
            && string.Equals(existing.StageDocumentId, documentId, StringComparison.OrdinalIgnoreCase))
        {
            return WorldDiplomacyThreatRegistrationDecision.AlreadyRegistered;
        }
        if (string.Equals(normalizedStage, "warning", StringComparison.OrdinalIgnoreCase))
        {
            return existing == null
                ? WorldDiplomacyThreatRegistrationDecision.CreateWarning
                : WorldDiplomacyThreatRegistrationDecision.Reject;
        }
        if (!string.Equals(normalizedStage, "ultimatum", StringComparison.OrdinalIgnoreCase))
        {
            return WorldDiplomacyThreatRegistrationDecision.Reject;
        }
        if (existing == null)
        {
            return WorldDiplomacyThreatRegistrationDecision.CreateUltimatum;
        }
        return CanEscalateThreatToUltimatum(existing, targetKingdomId)
            ? WorldDiplomacyThreatRegistrationDecision.EscalateToUltimatum
            : WorldDiplomacyThreatRegistrationDecision.Reject;
    }

    public static bool CanEscalateThreatToUltimatum(
        WorldDiplomacyThreat existing,
        string targetKingdomId)
    {
        return existing != null
            && string.Equals(existing.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(existing.Stage, "warning", StringComparison.OrdinalIgnoreCase)
            && string.Equals(existing.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase);
    }

    public static WorldDiplomacyThreat SelectOpenThreatBetween(
        List<WorldDiplomacyThreat> threats,
        string issuerKingdomId,
        string targetKingdomId)
    {
        string issuerId = (issuerKingdomId ?? "").Trim();
        string targetId = (targetKingdomId ?? "").Trim();
        if (issuerId.Length == 0 || targetId.Length == 0) return null;
        return (threats ?? new List<WorldDiplomacyThreat>()).FirstOrDefault(x => x != null
            && IsOpenDiplomaticThreatStatus(x.Status)
            && string.Equals(x.IssuerKingdomId, issuerId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));
    }

    public static WorldDiplomacyThreat SelectComplianceRecordedThreat(
        List<WorldDiplomacyThreat> threats,
        string issuerKingdomId,
        string targetKingdomId,
        string documentId,
        string processingActionId)
    {
        return (threats ?? new List<WorldDiplomacyThreat>()).FirstOrDefault(x => x != null
            && string.Equals(x.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.IssuerKingdomId, issuerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.ComplianceDocumentId, documentId, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(processingActionId)
                ? string.IsNullOrWhiteSpace(x.ComplianceActionId)
                : string.Equals(x.ComplianceActionId, processingActionId, StringComparison.OrdinalIgnoreCase)));
    }

    public static bool IsThreatComplianceAlreadyRecorded(
        WorldDiplomacyThreat threat,
        string documentId)
    {
        return threat != null
            && string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && string.Equals(threat.ComplianceDocumentId, documentId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsThreatComplianceStageMatch(
        WorldDiplomacyThreat threat,
        string respondingToThreatDocumentId,
        string respondingToThreatActionId)
    {
        return threat != null
            && string.Equals(threat.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase)
            && string.Equals(threat.StageDocumentId, respondingToThreatDocumentId, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(threat.StageActionId)
                ? string.IsNullOrWhiteSpace(respondingToThreatActionId)
                : string.Equals(threat.StageActionId, respondingToThreatActionId, StringComparison.OrdinalIgnoreCase));
    }

    public static int ResolveCompliancePrestigeDelta(
        string stage,
        int ultimatumDelta,
        int warningDelta)
    {
        return string.Equals(stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
            ? ultimatumDelta
            : warningDelta;
    }

    public static string DescribeThreatStage(string stage)
    {
        return stage == "warning" ? "谴责" : "最后通牒";
    }

    public static string DescribeThreatStageDiplomaticLabel(string stage)
    {
        return stage == "warning" ? "外交谴责" : "最后通牒";
    }

    public static bool IsThreatBetweenParties(
        WorldDiplomacyThreat threat,
        string firstKingdomId,
        string secondKingdomId)
    {
        if (threat == null) return false;
        return (string.Equals(threat.IssuerKingdomId, firstKingdomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(threat.TargetKingdomId, secondKingdomId, StringComparison.OrdinalIgnoreCase))
            || (string.Equals(threat.IssuerKingdomId, secondKingdomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(threat.TargetKingdomId, firstKingdomId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ShouldKeepWarningOpenAfterWar(
        WorldDiplomacyThreat threat,
        string issuerKingdomId,
        string targetKingdomId)
    {
        return threat != null
            && string.Equals(threat.IssuerKingdomId, issuerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(threat.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(threat.Stage, "warning", StringComparison.OrdinalIgnoreCase)
            && string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsThreatFollowThroughObligationPending(
        WorldDiplomacyThreat threat,
        List<string> presentedFollowThroughDocumentIds)
    {
        return threat != null
            && string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
            && (presentedFollowThroughDocumentIds ?? new List<string>())
                .Contains(threat.StageDocumentId, StringComparer.OrdinalIgnoreCase);
    }

    public static bool ThreatDeclarationTargetsThreatTarget(
        bool hasMatchingAction,
        string threatTargetKingdomId,
        string documentTargetKingdomId)
    {
        return hasMatchingAction
            || string.Equals(threatTargetKingdomId, documentTargetKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsThreatLinkedToDocument(
        WorldDiplomacyThreat threat,
        string documentId)
    {
        return threat != null && !string.IsNullOrWhiteSpace(documentId)
            && (string.Equals(threat.ComplianceDocumentId, documentId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(threat.ResolutionDocumentId, documentId, StringComparison.OrdinalIgnoreCase));
    }

    public static WorldDiplomacyThreatHistoryFinalization EvaluateThreatHistoryFinalization(
        WorldDiplomacyThreat threat,
        bool historyDeclarationRecorded,
        bool documentHistoryResultRecorded)
    {
        if (threat == null) return WorldDiplomacyThreatHistoryFinalization.None;
        if (string.Equals(threat.Status, "breached", StringComparison.OrdinalIgnoreCase)
            && historyDeclarationRecorded
            && !threat.HistoryResultRecorded)
        {
            return WorldDiplomacyThreatHistoryFinalization.AppendBreachResult;
        }
        if (documentHistoryResultRecorded)
        {
            return WorldDiplomacyThreatHistoryFinalization.MarkResultRecorded;
        }
        return WorldDiplomacyThreatHistoryFinalization.None;
    }

    public static bool IsThreatNonComplianceLinkedToDocument(
        WorldDiplomacyThreat threat,
        string documentId)
    {
        if (threat == null || string.IsNullOrWhiteSpace(documentId)) return false;
        return (threat.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>()).Any(decision => decision != null
                && !decision.HistoryRecorded
                && string.Equals(decision.DecisionDocumentId, documentId, StringComparison.OrdinalIgnoreCase))
            || (!threat.NonComplianceHistoryRecorded
                && string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
                && string.Equals(threat.TargetDecisionDocumentId, documentId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool NeedsNonComplianceHistoryRetry(WorldDiplomacyThreat threat)
    {
        return threat != null
            && ((threat.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>())
                    .Any(decision => decision != null && !decision.HistoryRecorded)
                || (string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
                    && !threat.NonComplianceHistoryRecorded));
    }

    public static bool NeedsThreatResultHistoryRetry(WorldDiplomacyThreat threat)
    {
        return threat != null && !threat.HistoryResultRecorded
            && (string.Equals(threat.Status, "breached", StringComparison.OrdinalIgnoreCase)
                || string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(threat.Status, "enforced", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(threat.ResolutionDocumentId)));
    }

    public static bool NeedsDomesticPenaltyHistoryRetry(WorldDiplomacyThreat threat)
    {
        return threat != null
            && string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && threat.DomesticPenaltyCompleted
            && !threat.DomesticPenaltyHistoryRecorded;
    }

    public static bool NeedsIssuerRewardHistoryRetry(WorldDiplomacyThreat threat)
    {
        return threat != null
            && string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && threat.IssuerRewardCompleted
            && !threat.IssuerRewardHistoryRecorded;
    }

    public static IOrderedEnumerable<WorldDiplomacyThreat> OrderThreatsByUpdatedDay(
        IEnumerable<WorldDiplomacyThreat> threats)
    {
        return (threats ?? Enumerable.Empty<WorldDiplomacyThreat>())
            .OrderBy(x => x.UpdatedDay)
            .ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase);
    }

    public static List<WorldDiplomacyThreat> SelectThreatHistoryRetryBatch(
        IEnumerable<WorldDiplomacyThreat> threats,
        Func<WorldDiplomacyThreat, bool> needsRetry,
        int take)
    {
        if (needsRetry == null || take <= 0) return new List<WorldDiplomacyThreat>();
        return OrderThreatsByUpdatedDay((threats ?? Enumerable.Empty<WorldDiplomacyThreat>())
                .Where(x => x != null && needsRetry(x)))
            .Take(take)
            .ToList();
    }

    public static bool NeedsDomesticPenaltySettlementRetry(WorldDiplomacyThreat threat)
    {
        return threat != null
            && (string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
                || string.Equals(threat.Status, "compliance_pending", StringComparison.OrdinalIgnoreCase))
            && !threat.DomesticPenaltyCompleted;
    }

    public static bool NeedsPolicyCancellationRetry(WorldDiplomacyThreat threat)
    {
        return threat != null
            && string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && !threat.PolicyConditionCancellationCompleted;
    }

    public static bool NeedsIssuerRewardSettlementRetry(WorldDiplomacyThreat threat)
    {
        return threat != null
            && string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && !threat.IssuerRewardCompleted;
    }

    public static bool CanAppendDomesticPenaltyHistory(WorldDiplomacyThreat threat)
    {
        return threat != null
            && !threat.DomesticPenaltyHistoryRecorded
            && threat.DomesticPenaltyCompleted
            && string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanAppendIssuerRewardHistory(WorldDiplomacyThreat threat)
    {
        return threat != null
            && !threat.IssuerRewardHistoryRecorded
            && threat.IssuerRewardCompleted
            && string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanAppendThreatResultHistory(WorldDiplomacyThreat threat)
    {
        return threat != null
            && !threat.HistoryResultRecorded
            && string.Equals(threat.Status, "breached", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanAppendNonComplianceEventHistory(WorldDiplomacyThreatNonComplianceEvent decision)
    {
        return decision != null
            && !decision.HistoryRecorded
            && !string.IsNullOrWhiteSpace(decision.StageDocumentId)
            && !string.IsNullOrWhiteSpace(decision.DecisionDocumentId);
    }

    public static bool CanCaptureNonComplianceEvent(WorldDiplomacyThreat threat)
    {
        return threat != null
            && string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(threat.StageDocumentId)
            && !string.IsNullOrWhiteSpace(threat.TargetDecisionDocumentId);
    }

    public static List<WorldDiplomacyThreatNonComplianceEvent> SelectUnrecordedNonComplianceEvents(
        WorldDiplomacyThreat threat)
    {
        return (threat?.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>())
            .Where(x => x != null && !x.HistoryRecorded)
            .OrderBy(x => x.DecisionDay)
            .ThenBy(x => x.StageDocumentId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static WorldDiplomacyThreatNonComplianceEvent SelectCurrentNonComplianceEvent(
        WorldDiplomacyThreat threat)
    {
        if (threat == null) return null;
        return (threat.NonComplianceEvents ?? new List<WorldDiplomacyThreatNonComplianceEvent>())
            .FirstOrDefault(x => x != null
                && string.Equals(x.StageDocumentId, threat.StageDocumentId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsNonComplianceEventForCurrentStage(
        WorldDiplomacyThreat threat,
        WorldDiplomacyThreatNonComplianceEvent decision)
    {
        return threat != null && decision != null
            && string.Equals(decision.StageDocumentId, threat.StageDocumentId, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeThreatEventStage(string stage)
    {
        return string.Equals(stage, "ultimatum", StringComparison.OrdinalIgnoreCase) ? "ultimatum" : "warning";
    }

    public static string DescribeThreatStageFormal(string stage)
    {
        return string.Equals(stage, "ultimatum", StringComparison.OrdinalIgnoreCase) ? "战争最后通牒" : "谴责";
    }

    public static string DescribeThreatExpectedFollowThrough(string stage)
    {
        return stage == "ultimatum" ? "宣战" : "升级为最后通牒";
    }

    public static string ResolveThreatCommitmentLevel(string stage)
    {
        return string.Equals(stage, "warning", StringComparison.OrdinalIgnoreCase) ? "non_binding" : "binding";
    }

    public static bool IsThreatLinkedToRound(
        WorldDiplomacyThreat threat,
        string roundId)
    {
        return threat != null && !string.IsNullOrWhiteSpace(roundId)
            && (string.Equals(threat.StageRoundId, roundId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(threat.TargetDecisionRoundId, roundId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsThreatCancellationStatusCancelled(string cancellationStatus)
    {
        return string.Equals(cancellationStatus, "cancelled", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsIssuerRewardHistoryEmpty(int rewardAmount, int appliedCount)
    {
        return rewardAmount <= 0 || appliedCount <= 0;
    }

    public static string LimitText(string value, int maxChars)
    {
        string text = value ?? "";
        return text.Length <= maxChars ? text : text.Substring(0, Math.Max(0, maxChars));
    }

    public static List<string> NormalizeThreatIdList(IEnumerable<string> ids)
    {
        return (ids ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<WorldDiplomacyThreatNonComplianceEvent> NormalizeThreatNonComplianceEvents(
        IEnumerable<WorldDiplomacyThreatNonComplianceEvent> events)
    {
        return (events ?? new List<WorldDiplomacyThreatNonComplianceEvent>())
            .Where(x => x != null)
            .Select(x =>
            {
                x.Stage = WorldDiplomacyIntentVocabulary.NormalizeToken(x.Stage) == "ultimatum" ? "ultimatum" : "warning";
                x.StageDocumentId = (x.StageDocumentId ?? "").Trim();
                x.StageActionId = (x.StageActionId ?? "").Trim();
                x.DecisionDocumentId = (x.DecisionDocumentId ?? "").Trim();
                x.DecisionActionId = (x.DecisionActionId ?? "").Trim();
                x.DecisionRoundId = (x.DecisionRoundId ?? "").Trim();
                x.DecisionDay = Math.Max(0, x.DecisionDay);
                return x;
            })
            .Where(x => x.StageDocumentId.Length > 0 && x.DecisionDocumentId.Length > 0)
            .GroupBy(x => x.StageDocumentId + "\n" + x.StageActionId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(x => x.HistoryRecorded).ThenByDescending(x => x.DecisionDay).First())
            .OrderBy(x => x.DecisionDay)
            .ThenBy(x => x.StageDocumentId, StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();
    }

    public static void CaptureThreatNonComplianceEvent(WorldDiplomacyThreat threat)
    {
        if (!CanCaptureNonComplianceEvent(threat)) return;
        threat.NonComplianceEvents ??= new List<WorldDiplomacyThreatNonComplianceEvent>();
        WorldDiplomacyThreatNonComplianceEvent decision = SelectCurrentNonComplianceEvent(threat);
        if (decision == null)
        {
            decision = new WorldDiplomacyThreatNonComplianceEvent();
            threat.NonComplianceEvents.Add(decision);
        }
        decision.Stage = NormalizeThreatEventStage(threat.Stage);
        decision.StageDocumentId = threat.StageDocumentId ?? "";
        decision.StageActionId = threat.StageActionId ?? "";
        decision.DecisionDocumentId = threat.TargetDecisionDocumentId ?? "";
        decision.DecisionActionId = threat.TargetDecisionActionId ?? "";
        decision.DecisionRoundId = threat.TargetDecisionRoundId ?? "";
        decision.DecisionDay = Math.Max(0, threat.TargetDecisionDay);
        if (threat.NonComplianceHistoryRecorded) decision.HistoryRecorded = true;
    }

    public static void NormalizeThreatRecord(
        WorldDiplomacyThreat threat,
        Func<string> newThreatId,
        int issuerRewardMax)
    {
        if (threat == null) return;
        threat.ThreatId = string.IsNullOrWhiteSpace(threat.ThreatId)
            ? (newThreatId?.Invoke() ?? threat.ThreatId)
            : threat.ThreatId.Trim();
        threat.IssuerKingdomId = (threat.IssuerKingdomId ?? "").Trim();
        threat.TargetKingdomId = (threat.TargetKingdomId ?? "").Trim();
        string normalizedStage = WorldDiplomacyIntentVocabulary.NormalizeToken(threat.Stage);
        threat.Stage = normalizedStage == "ultimatum" || !string.IsNullOrWhiteSpace(threat.UltimatumDocumentId)
            ? "ultimatum"
            : "warning";
        threat.Status = WorldDiplomacyIntentVocabulary.NormalizeToken(threat.Status);
        if (string.IsNullOrWhiteSpace(threat.Status)) threat.Status = "open";
        if (string.Equals(threat.Status, "compliance_pending", StringComparison.OrdinalIgnoreCase))
        {
            threat.Status = "complied";
        }
        threat.WarningDocumentId = (threat.WarningDocumentId ?? "").Trim();
        threat.WarningActionId = (threat.WarningActionId ?? "").Trim();
        threat.UltimatumDocumentId = (threat.UltimatumDocumentId ?? "").Trim();
        threat.UltimatumActionId = (threat.UltimatumActionId ?? "").Trim();
        threat.StageDocumentId = (threat.StageDocumentId ?? "").Trim();
        threat.StageActionId = (threat.StageActionId ?? "").Trim();
        threat.StageRoundId = (threat.StageRoundId ?? "").Trim();
        threat.TargetDecision = WorldDiplomacyIntentVocabulary.NormalizeToken(threat.TargetDecision);
        if (string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)) threat.TargetDecision = "complied";
        else if (threat.TargetDecision != "noncomplied") threat.TargetDecision = "pending";
        threat.TargetDecisionDocumentId = (threat.TargetDecisionDocumentId ?? "").Trim();
        threat.TargetDecisionActionId = (threat.TargetDecisionActionId ?? "").Trim();
        threat.TargetDecisionRoundId = (threat.TargetDecisionRoundId ?? "").Trim();
        threat.TargetDecisionDay = Math.Max(0, threat.TargetDecisionDay);
        threat.NonComplianceEvents = NormalizeThreatNonComplianceEvents(threat.NonComplianceEvents);
        CaptureThreatNonComplianceEvent(threat);
        WorldDiplomacyThreatNonComplianceEvent currentNonCompliance = SelectCurrentNonComplianceEvent(threat);
        if (currentNonCompliance != null) threat.NonComplianceHistoryRecorded = currentNonCompliance.HistoryRecorded;
        threat.ObligationRoundId = "";
        threat.ComplianceDocumentId = (threat.ComplianceDocumentId ?? "").Trim();
        threat.ComplianceActionId = (threat.ComplianceActionId ?? "").Trim();
        threat.ResolutionRoundId = (threat.ResolutionRoundId ?? "").Trim();
        threat.ResolutionDocumentId = (threat.ResolutionDocumentId ?? "").Trim();
        threat.ResolutionActionId = (threat.ResolutionActionId ?? "").Trim();
        threat.ResolutionReason = LimitText((threat.ResolutionReason ?? "").Trim(), 180);
        threat.DomesticPenaltyRulingClanId = (threat.DomesticPenaltyRulingClanId ?? "").Trim();
        threat.CreatedDay = Math.Max(0, threat.CreatedDay);
        threat.StageIssuedDay = Math.Max(threat.CreatedDay, threat.StageIssuedDay);
        threat.UpdatedDay = Math.Max(threat.StageIssuedDay, threat.UpdatedDay);
        threat.ObligationClaimedDay = 0;
        threat.ReputationPenaltyAmount = Math.Max(0, threat.ReputationPenaltyAmount);
        if (string.IsNullOrWhiteSpace(threat.StageDocumentId))
        {
            threat.StageDocumentId = threat.Stage == "ultimatum" ? threat.UltimatumDocumentId : threat.WarningDocumentId;
        }
        if (threat.Stage == "ultimatum" && string.IsNullOrWhiteSpace(threat.UltimatumDocumentId))
        {
            threat.UltimatumDocumentId = threat.StageDocumentId;
        }
        else if (threat.Stage == "warning" && string.IsNullOrWhiteSpace(threat.WarningDocumentId))
        {
            threat.WarningDocumentId = threat.StageDocumentId;
        }
        if (!IsOpenDiplomaticThreatStatus(threat.Status))
        {
            threat.ObligationRoundId = "";
            threat.ObligationClaimedDay = 0;
        }
        threat.DomesticPenaltyEligibleClanIds = NormalizeThreatIdList(threat.DomesticPenaltyEligibleClanIds);
        threat.DomesticPenaltyAppliedClanIds = NormalizeThreatIdList(threat.DomesticPenaltyAppliedClanIds);
        threat.DomesticPenaltySkippedClanIds = NormalizeThreatIdList(threat.DomesticPenaltySkippedClanIds);
        foreach (string settledClanId in threat.DomesticPenaltyAppliedClanIds.Concat(threat.DomesticPenaltySkippedClanIds))
        {
            if (!threat.DomesticPenaltyEligibleClanIds.Contains(settledClanId, StringComparer.OrdinalIgnoreCase))
            {
                threat.DomesticPenaltyEligibleClanIds.Add(settledClanId);
            }
        }
        if (threat.DomesticPenaltyEligibleClanIds.Count > 0 || threat.DomesticPenaltyAppliedClanIds.Count > 0
            || threat.DomesticPenaltySkippedClanIds.Count > 0 || threat.DomesticPenaltyCompleted)
        {
            threat.DomesticPenaltySnapshotCaptured = true;
        }
        if (threat.DomesticPenaltySnapshotCaptured
            && threat.DomesticPenaltyEligibleClanIds.All(id => threat.DomesticPenaltyAppliedClanIds.Contains(id, StringComparer.OrdinalIgnoreCase)
                || threat.DomesticPenaltySkippedClanIds.Contains(id, StringComparer.OrdinalIgnoreCase)))
        {
            threat.DomesticPenaltyCompleted = true;
        }

        threat.PolicyConditionSignalKey = (threat.PolicyConditionSignalKey ?? "").Trim();
        threat.PolicyConditionPolicyId = (threat.PolicyConditionPolicyId ?? "").Trim();
        threat.PolicyConditionPolicyName = LimitText((threat.PolicyConditionPolicyName ?? "").Trim(), 80);
        threat.PolicyConditionOwnerKingdomId = (threat.PolicyConditionOwnerKingdomId ?? "").Trim();
        threat.PolicyConditionAffectedKingdomId = (threat.PolicyConditionAffectedKingdomId ?? "").Trim();
        threat.PolicyConditionBoundDay = Math.Max(0, threat.PolicyConditionBoundDay);
        threat.PolicyConditionCancellationStatus = WorldDiplomacyIntentVocabulary.NormalizeToken(threat.PolicyConditionCancellationStatus);
        threat.PolicyConditionCancellationDay = Math.Max(0, threat.PolicyConditionCancellationDay);
        if (threat.PolicyConditionPolicyId.Length == 0 || threat.PolicyConditionOwnerKingdomId.Length == 0)
        {
            threat.PolicyConditionCancellationCompleted = true;
            threat.PolicyConditionCancellationStatus = "not_bound";
        }
        else if (threat.PolicyConditionCancellationStatus is "cancelled" or "already_inactive")
        {
            threat.PolicyConditionCancellationCompleted = true;
        }
        else if (!threat.PolicyConditionCancellationCompleted)
        {
            threat.PolicyConditionCancellationStatus = "pending";
        }

        threat.IssuerRewardRulingClanId = (threat.IssuerRewardRulingClanId ?? "").Trim();
        threat.IssuerRewardAmount = ClampThreatIssuerRewardAmount(threat.IssuerRewardAmount, issuerRewardMax);
        threat.IssuerRewardEligibleClanIds = NormalizeThreatIdList(threat.IssuerRewardEligibleClanIds);
        threat.IssuerRewardAppliedClanIds = NormalizeThreatIdList(threat.IssuerRewardAppliedClanIds);
        threat.IssuerRewardSkippedClanIds = NormalizeThreatIdList(threat.IssuerRewardSkippedClanIds);
        foreach (string settledClanId in threat.IssuerRewardAppliedClanIds.Concat(threat.IssuerRewardSkippedClanIds))
        {
            if (!threat.IssuerRewardEligibleClanIds.Contains(settledClanId, StringComparer.OrdinalIgnoreCase))
            {
                threat.IssuerRewardEligibleClanIds.Add(settledClanId);
            }
        }
        if (threat.IssuerRewardEligibleClanIds.Count > 0 || threat.IssuerRewardAppliedClanIds.Count > 0
            || threat.IssuerRewardSkippedClanIds.Count > 0 || threat.IssuerRewardCompleted)
        {
            threat.IssuerRewardSnapshotCaptured = true;
        }
        if (threat.IssuerRewardSnapshotCaptured
            && threat.IssuerRewardEligibleClanIds.All(id => threat.IssuerRewardAppliedClanIds.Contains(id, StringComparer.OrdinalIgnoreCase)
                || threat.IssuerRewardSkippedClanIds.Contains(id, StringComparer.OrdinalIgnoreCase)))
        {
            threat.IssuerRewardCompleted = true;
        }
        if (threat.IssuerRewardCompleted && threat.IssuerRewardAmount <= 0)
        {
            threat.IssuerRewardHistoryRecorded = true;
        }
    }

    public static Dictionary<string, int> NormalizeKingdomIntDictionary(
        IEnumerable<KeyValuePair<string, int>> entries,
        int minValue,
        int maxValue)
    {
        return (entries ?? Enumerable.Empty<KeyValuePair<string, int>>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => Math.Max(minValue, Math.Min(maxValue, x.Last().Value)),
                StringComparer.OrdinalIgnoreCase);
    }

    public static List<WorldDiplomacyPrestigeRelationModifier> SelectRetainedPrestigeRelationModifiers(
        IEnumerable<WorldDiplomacyPrestigeRelationModifier> modifiers)
    {
        return (modifiers ?? Enumerable.Empty<WorldDiplomacyPrestigeRelationModifier>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId)
                && !string.IsNullOrWhiteSpace(x.RulerHeroId) && !string.IsNullOrWhiteSpace(x.VassalLeaderHeroId))
            .GroupBy(x => x.KingdomId.Trim() + "|" + x.RulerHeroId.Trim() + "|" + x.VassalLeaderHeroId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Last())
            .ToList();
    }

    public static bool HasValidThreatParties(WorldDiplomacyThreat threat)
    {
        return threat != null
            && !string.IsNullOrWhiteSpace(threat.IssuerKingdomId)
            && !string.IsNullOrWhiteSpace(threat.TargetKingdomId)
            && !string.Equals(threat.IssuerKingdomId, threat.TargetKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static WorldDiplomacyThreat SelectRetainedOpenThreatForIssuer(
        IEnumerable<WorldDiplomacyThreat> issuerThreats)
    {
        return (issuerThreats ?? Enumerable.Empty<WorldDiplomacyThreat>())
            .OrderByDescending(x => x.UpdatedDay)
            .ThenByDescending(x => x.StageIssuedDay)
            .ThenByDescending(x => string.Equals(x.Stage, "ultimatum", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenByDescending(x => x.CreatedDay)
            .ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static bool NeedsThreatSettlementRetention(WorldDiplomacyThreat threat)
    {
        if (threat == null) return false;
        bool domesticPenaltyPending = (string.Equals(threat.Status, "compliance_pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase))
            && !threat.DomesticPenaltyCompleted;
        bool domesticHistoryPending = string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && threat.DomesticPenaltyCompleted && !threat.DomesticPenaltyHistoryRecorded;
        bool complianceConsequencePending = string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && (!threat.PolicyConditionCancellationCompleted || !threat.IssuerRewardCompleted);
        bool issuerRewardHistoryPending = string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && threat.IssuerRewardCompleted && !threat.IssuerRewardHistoryRecorded;
        return domesticPenaltyPending || domesticHistoryPending || complianceConsequencePending
            || issuerRewardHistoryPending || NeedsNonComplianceHistoryRetry(threat) || !threat.HistoryResultRecorded;
    }

    public static List<WorldDiplomacyThreat> SelectThreatRetentionSet(
        IEnumerable<WorldDiplomacyThreat> threats,
        int cutoffDay,
        int maxStored)
    {
        List<WorldDiplomacyThreat> source = (threats ?? new List<WorldDiplomacyThreat>())
            .Where(x => x != null)
            .ToList();
        List<WorldDiplomacyThreat> active = source
            .Where(x => IsRetainedActiveDiplomaticThreatStatus(x.Status))
            .ToList();
        HashSet<WorldDiplomacyThreat> activeSet = new HashSet<WorldDiplomacyThreat>(active);
        List<WorldDiplomacyThreat> protectedTerminal = source
            .Where(x => !activeSet.Contains(x) && NeedsThreatSettlementRetention(x))
            .OrderByDescending(x => x.UpdatedDay)
            .ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        HashSet<WorldDiplomacyThreat> protectedSet = new HashSet<WorldDiplomacyThreat>(protectedTerminal);
        int ordinarySlots = Math.Max(0, maxStored - protectedTerminal.Count);
        List<WorldDiplomacyThreat> ordinaryTerminal = source
            .Where(x => !activeSet.Contains(x) && !protectedSet.Contains(x) && x.UpdatedDay >= cutoffDay)
            .OrderByDescending(x => x.UpdatedDay)
            .ThenByDescending(x => x.CreatedDay)
            .ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
            .Take(ordinarySlots)
            .ToList();
        return active
            .Concat(protectedTerminal)
            .Concat(ordinaryTerminal)
            .OrderBy(x => x.CreatedDay)
            .ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // This is storage repair rather than a gameplay result and must not create a history event.
    public static void InvalidateThreatForNormalization(
        WorldDiplomacyThreat threat,
        string reason,
        int currentDay)
    {
        if (threat == null) return;
        threat.Status = "invalidated";
        threat.ResolutionReason = LimitText(reason, 180);
        threat.UpdatedDay = Math.Max(threat.UpdatedDay, Math.Max(0, currentDay));
        threat.ObligationRoundId = "";
        threat.ObligationClaimedDay = 0;
        threat.HistoryResultRecorded = true;
    }

    public static string ResolveThreatRequiredSpeakerId(WorldDiplomacyThreat threat)
    {
        if (threat == null) return "";
        if (string.Equals(threat.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase))
        {
            return threat.IssuerKingdomId ?? "";
        }
        return string.Equals(threat.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase)
            ? threat.TargetKingdomId ?? ""
            : "";
    }

    public static WorldDiplomacyPolicyCancellationDispatch EvaluateThreatPolicyCancellationDispatch(
        WorldDiplomacyThreat threat)
    {
        if (threat == null
            || !string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase))
        {
            return WorldDiplomacyPolicyCancellationDispatch.Reject;
        }
        if (threat.PolicyConditionCancellationCompleted)
        {
            return WorldDiplomacyPolicyCancellationDispatch.AlreadyComplete;
        }
        if (string.IsNullOrWhiteSpace(threat.PolicyConditionPolicyId)
            || string.IsNullOrWhiteSpace(threat.PolicyConditionOwnerKingdomId))
        {
            return WorldDiplomacyPolicyCancellationDispatch.MarkNotBound;
        }
        return WorldDiplomacyPolicyCancellationDispatch.AttemptCancellation;
    }

    public static bool IsPolicySignalEligibleForThreatBinding(WorldDiplomacyPolicySignal signal)
    {
        return signal != null
            && !string.IsNullOrWhiteSpace(signal.PolicyId)
            && (string.IsNullOrWhiteSpace(signal.PolicyKind)
                || string.Equals(signal.PolicyKind.Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(signal.IssuerKingdomId)
            && !string.IsNullOrWhiteSpace(signal.TargetKingdomId);
    }

    public static bool IsThreatPolicyPartyMatch(
        string policyOwnerRepresentativeId,
        string affectedRepresentativeId,
        string threatTargetId,
        string threatIssuerId)
    {
        return !string.IsNullOrWhiteSpace(policyOwnerRepresentativeId)
            && !string.IsNullOrWhiteSpace(affectedRepresentativeId)
            && string.Equals(policyOwnerRepresentativeId, threatTargetId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(affectedRepresentativeId, threatIssuerId, StringComparison.OrdinalIgnoreCase);
    }

    public static WorldDiplomacyPolicySignal SelectUniquePolicySignal(
        IEnumerable<WorldDiplomacyPolicySignal> matches)
    {
        List<WorldDiplomacyPolicySignal> candidates = (matches ?? new List<WorldDiplomacyPolicySignal>())
            .Where(x => x != null)
            .GroupBy(x => (x.PolicyId ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(x => x.PublishedDay).First())
            .Take(2)
            .ToList();
        return candidates.Count == 1 ? candidates[0] : null;
    }

    public static void InitializeThreatPolicyCondition(
        WorldDiplomacyThreat threat,
        WorldDiplomacyPolicySignal signal,
        int day)
    {
        if (threat == null) return;
        if (signal == null)
        {
            threat.PolicyConditionCancellationCompleted = true;
            threat.PolicyConditionCancellationStatus = "not_bound";
            return;
        }
        threat.PolicyConditionSignalKey = (signal.SignalKey ?? "").Trim();
        threat.PolicyConditionPolicyId = (signal.PolicyId ?? "").Trim();
        threat.PolicyConditionPolicyName = LimitText((signal.PolicyName ?? "").Trim(), 80);
        threat.PolicyConditionOwnerKingdomId = (signal.IssuerKingdomId ?? "").Trim();
        threat.PolicyConditionAffectedKingdomId = (signal.TargetKingdomId ?? "").Trim();
        threat.PolicyConditionBoundDay = Math.Max(0, day);
        threat.PolicyConditionCancellationCompleted = false;
        threat.PolicyConditionCancellationStatus = "pending";
    }

    public static bool IsPolicySignalBoundTo(
        WorldDiplomacyPolicySignal signal,
        string policyId,
        string ownerKingdomId)
    {
        return signal != null
            && string.Equals(signal.PolicyId, policyId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(signal.IssuerKingdomId, ownerKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsThreatBoundToPolicy(
        WorldDiplomacyThreat threat,
        string policyId,
        string ownerKingdomId)
    {
        return threat != null
            && IsOpenDiplomaticThreatStatus(threat.Status)
            && string.Equals(threat.PolicyConditionPolicyId, policyId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(threat.PolicyConditionOwnerKingdomId, ownerKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static void InvalidateThreatForPolicyCancellation(WorldDiplomacyThreat threat, int day)
    {
        if (threat == null) return;
        threat.Status = "invalidated";
        threat.ResolutionReason = "bound_policy_cancelled_by_another_complied_threat";
        threat.ResolutionRoundId = "";
        threat.ResolutionDocumentId = "";
        threat.ObligationRoundId = "";
        threat.ObligationClaimedDay = 0;
        threat.UpdatedDay = Math.Max(threat.UpdatedDay, day);
        threat.HistoryResultRecorded = true;
    }

    public static string ResolvePolicyCancellationStatus(string result)
    {
        return string.IsNullOrWhiteSpace(result) ? "cancelled" : result.Trim().ToLowerInvariant();
    }

    public static void MigrateThreatComplianceConsequencesV3(WorldDiplomacyThreat threat)
    {
        if (threat == null) return;
        threat.PolicyConditionSignalKey = "";
        threat.PolicyConditionPolicyId = "";
        threat.PolicyConditionPolicyName = "";
        threat.PolicyConditionOwnerKingdomId = "";
        threat.PolicyConditionAffectedKingdomId = "";
        threat.PolicyConditionCancellationCompleted = true;
        threat.PolicyConditionCancellationStatus = "legacy_not_bound";
        if (string.Equals((threat.Status ?? "").Trim(), "open", StringComparison.OrdinalIgnoreCase)) return;
        // Existing terminal threats predate the issuer reward. Never award them retroactively.
        threat.IssuerRewardAmount = 0;
        threat.IssuerRewardSnapshotCaptured = true;
        threat.IssuerRewardCompleted = true;
        threat.IssuerRewardHistoryRecorded = true;
    }

    public static bool IsThreatDomesticPenaltyEligible(
        WorldDiplomacyThreat threat,
        string compliantKingdomId)
    {
        return threat != null
            && (string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
                || string.Equals(threat.Status, "compliance_pending", StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(threat.StageDocumentId)
            && !string.IsNullOrWhiteSpace(threat.ComplianceDocumentId)
            && string.Equals(threat.TargetKingdomId, compliantKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsThreatIssuerRewardEligible(
        WorldDiplomacyThreat threat,
        string issuerKingdomId)
    {
        return threat != null
            && string.Equals(threat.Status, "complied", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(threat.ComplianceDocumentId)
            && string.Equals(threat.IssuerKingdomId, issuerKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static int CountThreatConsequenceAppliedClans(IEnumerable<string> appliedIds)
    {
        return (appliedIds ?? Enumerable.Empty<string>()).Count(x => !string.IsNullOrWhiteSpace(x));
    }

    public static void CaptureThreatDomesticPenaltySnapshot(
        WorldDiplomacyThreat threat,
        string rulingClanId,
        IEnumerable<string> eligibleClanIds)
    {
        if (threat == null) return;
        threat.DomesticPenaltyRulingClanId = rulingClanId;
        threat.DomesticPenaltyEligibleClanIds = OrderThreatConsequenceClanIds(eligibleClanIds);
        threat.DomesticPenaltyAppliedClanIds ??= new List<string>();
        threat.DomesticPenaltyAppliedClanIds.Clear();
        threat.DomesticPenaltySkippedClanIds ??= new List<string>();
        threat.DomesticPenaltySkippedClanIds.Clear();
        threat.DomesticPenaltySnapshotCaptured = true;
    }

    public static void CaptureThreatIssuerRewardSnapshot(
        WorldDiplomacyThreat threat,
        string rulingClanId,
        IEnumerable<string> eligibleClanIds)
    {
        if (threat == null) return;
        threat.IssuerRewardRulingClanId = rulingClanId;
        threat.IssuerRewardEligibleClanIds = OrderThreatConsequenceClanIds(eligibleClanIds);
        threat.IssuerRewardAppliedClanIds ??= new List<string>();
        threat.IssuerRewardAppliedClanIds.Clear();
        threat.IssuerRewardSkippedClanIds ??= new List<string>();
        threat.IssuerRewardSkippedClanIds.Clear();
        threat.IssuerRewardSnapshotCaptured = true;
    }

    public static void CompleteThreatDomesticPenaltyWithoutEligible(WorldDiplomacyThreat threat)
    {
        if (threat == null) return;
        threat.DomesticPenaltyAppliedClanIds.Clear();
        threat.DomesticPenaltySkippedClanIds.Clear();
        threat.DomesticPenaltyCompleted = true;
    }

    public static void CompleteThreatIssuerRewardWithoutEligible(WorldDiplomacyThreat threat)
    {
        if (threat == null) return;
        threat.IssuerRewardCompleted = true;
        threat.IssuerRewardHistoryRecorded = true;
    }

    public static void CompleteThreatIssuerRewardWithoutAmount(WorldDiplomacyThreat threat)
    {
        if (threat == null) return;
        threat.IssuerRewardSnapshotCaptured = true;
        threat.IssuerRewardCompleted = true;
        threat.IssuerRewardHistoryRecorded = true;
    }

    public static List<string> SelectUnresolvedClanIds(
        IEnumerable<string> eligibleIds,
        ISet<string> appliedIds)
    {
        return (eligibleIds ?? Enumerable.Empty<string>())
            .Where(id => !(appliedIds ?? new HashSet<string>()).Contains(id))
            .ToList();
    }

    public static void CompleteThreatDomesticPenaltyAsSkipped(
        WorldDiplomacyThreat threat,
        IEnumerable<string> skippedIds)
    {
        if (threat == null) return;
        threat.DomesticPenaltySkippedClanIds = OrderThreatConsequenceClanIds(skippedIds);
        threat.DomesticPenaltyCompleted = true;
    }

    public static void CompleteThreatIssuerRewardAsSkipped(
        WorldDiplomacyThreat threat,
        IEnumerable<string> skippedIds)
    {
        if (threat == null) return;
        threat.IssuerRewardSkippedClanIds = OrderThreatConsequenceClanIds(skippedIds);
        threat.IssuerRewardCompleted = true;
    }

    public static bool IsThreatConsequenceClanSettled(
        string clanId,
        ISet<string> appliedIds,
        ISet<string> skippedIds)
    {
        return (appliedIds != null && appliedIds.Contains(clanId))
            || (skippedIds != null && skippedIds.Contains(clanId));
    }

    public static bool IsThreatConsequenceSettled(
        IEnumerable<string> eligibleIds,
        ISet<string> appliedIds,
        ISet<string> skippedIds)
    {
        return (eligibleIds ?? Enumerable.Empty<string>())
            .All(id => IsThreatConsequenceClanSettled(id, appliedIds, skippedIds));
    }

    public static List<string> OrderThreatConsequenceClanIds(IEnumerable<string> ids)
    {
        return (ids ?? Enumerable.Empty<string>())
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static int ClampThreatIssuerRewardAmount(int amount, int rewardMax)
    {
        return Math.Max(0, Math.Min(rewardMax, amount));
    }

    public static int ComputeThreatConsequenceRelationTarget(int relationBefore, int delta)
    {
        return Math.Max(-100, Math.Min(100, relationBefore + delta));
    }

    public static bool ShouldApplyThreatRelationReward(int relationBefore)
    {
        return relationBefore < 100;
    }

    public static bool ShouldApplyThreatRelationPenalty(int relationBefore)
    {
        return relationBefore > -100;
    }

    public static bool IsThreatRelationRewardApplied(int relationAfter, int expectedRelation)
    {
        return relationAfter >= expectedRelation;
    }

    public static bool IsThreatRelationPenaltyApplied(int relationAfter, int expectedRelation)
    {
        return relationAfter <= expectedRelation;
    }

    public static void FinalizeUnresolvedThreatDomesticPenalty(
        WorldDiplomacyThreat threat,
        IEnumerable<string> skippedIds,
        int day)
    {
        if (threat == null) return;
        threat.DomesticPenaltySkippedClanIds = OrderThreatConsequenceClanIds(skippedIds);
        threat.DomesticPenaltySnapshotCaptured = true;
        threat.DomesticPenaltyCompleted = true;
        threat.UpdatedDay = day;
    }

    public static void FinalizeUnresolvedThreatIssuerReward(
        WorldDiplomacyThreat threat,
        IEnumerable<string> skippedIds,
        int day)
    {
        if (threat == null) return;
        threat.IssuerRewardSkippedClanIds = OrderThreatConsequenceClanIds(skippedIds);
        threat.IssuerRewardSnapshotCaptured = true;
        threat.IssuerRewardCompleted = true;
        threat.UpdatedDay = day;
    }

    public static bool IsThreatDecisionNoncomplied(WorldDiplomacyThreat threat)
    {
        return string.Equals(threat?.TargetDecision, "noncomplied", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsThreatDecisionPending(WorldDiplomacyThreat threat)
    {
        return string.Equals(threat?.TargetDecision, "pending", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsThreatAtStage(WorldDiplomacyThreat threat, string stage)
    {
        return string.Equals(threat?.Stage, stage, StringComparison.OrdinalIgnoreCase);
    }

    public static string SelectThreatStageDocumentId(WorldDiplomacyThreat threat)
    {
        return IsThreatAtStage(threat, "ultimatum")
            ? threat.UltimatumDocumentId
            : threat.WarningDocumentId;
    }

    public static string ResolveCanonicalThreatStage(string stage, string ultimatumDocumentId)
    {
        return WorldDiplomacyIntentVocabulary.NormalizeToken(stage) == "ultimatum"
            || !string.IsNullOrWhiteSpace(ultimatumDocumentId)
                ? "ultimatum"
                : "warning";
    }

    public static List<WorldDiplomacyThreat> SelectPendingIncomingThreats(
        IEnumerable<WorldDiplomacyThreat> threats,
        string targetKingdomId)
    {
        return (threats ?? new List<WorldDiplomacyThreat>())
            .Where(x => IsOpenDiplomaticThreatStatus(x?.Status)
                && string.Equals(x.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
                && IsThreatDecisionPending(x))
            .OrderByDescending(x => x.UpdatedDay)
            .ToList();
    }

    public static bool IsRejectedUltimatumEnforceable(WorldDiplomacyThreat threat)
    {
        return IsThreatAtStage(threat, "ultimatum")
            && IsThreatDecisionNoncomplied(threat);
    }

    public static bool IsEscalatableWarningThreat(
        WorldDiplomacyThreat threat,
        string targetKingdomId)
    {
        return threat != null
            && string.Equals(threat.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase)
            && IsThreatAtStage(threat, "warning")
            && IsThreatDecisionNoncomplied(threat);
    }

    public static List<string> SelectNoncompliedThreatStageDocumentIds(
        IEnumerable<WorldDiplomacyThreat> threats,
        string issuerKingdomId)
    {
        string issuerId = (issuerKingdomId ?? "").Trim();
        if (issuerId.Length == 0) return new List<string>();
        return (threats ?? new List<WorldDiplomacyThreat>())
            .Where(x => IsOpenDiplomaticThreatStatus(x?.Status)
                && string.Equals(x.IssuerKingdomId, issuerId, StringComparison.OrdinalIgnoreCase)
                && IsThreatDecisionNoncomplied(x)
                && !string.IsNullOrWhiteSpace(x.StageDocumentId))
            .OrderBy(x => x.StageIssuedDay)
            .ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.StageDocumentId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> SelectPresentedThreatStageDocumentIds(
        IEnumerable<WorldDiplomacyThreat> threats,
        string authorKingdomId)
    {
        string authorId = (authorKingdomId ?? "").Trim();
        if (authorId.Length == 0) return new List<string>();
        return (threats ?? new List<WorldDiplomacyThreat>())
            .Where(x => IsOpenDiplomaticThreatStatus(x?.Status)
                && !string.IsNullOrWhiteSpace(x.StageDocumentId)
                && (string.Equals(x.IssuerKingdomId, authorId, StringComparison.OrdinalIgnoreCase)
                    || (string.Equals(x.TargetKingdomId, authorId, StringComparison.OrdinalIgnoreCase)
                        && IsThreatDecisionPending(x))))
            .OrderBy(x => x.StageIssuedDay)
            .ThenBy(x => x.ThreatId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.StageDocumentId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsSettlementSlotRelatedTo(
        WorldDiplomacyResultSettlementSlot slot,
        string kingdomId)
    {
        return slot?.RelatedKingdomIds != null
            && slot.RelatedKingdomIds.Contains(kingdomId, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsThreatStageSourceDocument(
        WorldDiplomacyDocument source,
        WorldDiplomacyThreat threat,
        bool stageDocumentIndexed)
    {
        return source?.IsReadyForPublication == true
            && threat != null
            && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(source.Intent), threat.Stage,
                StringComparison.Ordinal)
            && string.Equals(source.AuthorKingdomId, threat.IssuerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(source.TargetKingdomId, threat.TargetKingdomId, StringComparison.OrdinalIgnoreCase)
            && stageDocumentIndexed;
    }

    public static bool IsWarResponseSourceDocument(
        WorldDiplomacyDocument document,
        string roundId,
        string authorKingdomId)
    {
        return document?.IsReadyForPublication == true
            && string.Equals(document.RoundId, roundId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(document.AuthorKingdomId, authorKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool CannotCaptureThreatConsequenceSnapshot(
        bool kingdomEliminated,
        bool kingdomRulingClanMissing,
        bool snapshotCaptured)
    {
        return kingdomEliminated && kingdomRulingClanMissing && !snapshotCaptured;
    }

    public static bool IsThreatFollowThroughEscalation(
        string stage,
        string followThroughIntent,
        bool targetsThreatTarget)
    {
        return string.Equals(stage, "warning", StringComparison.OrdinalIgnoreCase)
            && followThroughIntent == "ultimatum"
            && targetsThreatTarget;
    }

    public static bool IsThreatFollowThroughEnforcement(
        string stage,
        string followThroughIntent,
        bool targetsThreatTarget,
        bool changedDiplomaticState)
    {
        return string.Equals(stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
            && followThroughIntent == "declare_war"
            && targetsThreatTarget
            && changedDiplomaticState;
    }

    public static bool IsThreatBreachPenaltyApplicable(WorldDiplomacyThreat threat)
    {
        return threat != null && !threat.ReputationPenaltyApplied;
    }

    public static int ResolveThreatBreachPrestigePenalty(
        string stage,
        int ultimatumPenalty,
        int warningPenalty)
    {
        return string.Equals(stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
            ? ultimatumPenalty
            : warningPenalty;
    }

    public static int ResolveThreatBreachRelationPenalty(
        string stage,
        int ultimatumPenalty,
        int warningPenalty)
    {
        return string.Equals(stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
            ? ultimatumPenalty
            : warningPenalty;
    }

    public static string DescribeThreatBreachPrestigeReason(string stage)
    {
        return string.Equals(stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
            ? "最后通牒遭拒后没有在下一篇宣言中宣战"
            : "外交谴责遭拒后没有在下一篇宣言中升级最后通牒";
    }

    public static string DescribeThreatBreachResolutionReason(string stage)
    {
        return string.Equals(stage, "ultimatum", StringComparison.OrdinalIgnoreCase)
            ? "ultimatum_not_followed_by_war_in_next_declaration"
            : "warning_not_followed_by_ultimatum_in_next_declaration";
    }

    public static void ApplyThreatBreachSettlement(
        WorldDiplomacyThreat threat,
        int penaltyAmount,
        string resolutionRoundId,
        string resolutionDocumentId,
        int currentDay)
    {
        threat.Status = "breached";
        threat.ReputationPenaltyApplied = true;
        threat.ReputationPenaltyAmount = Math.Max(0, penaltyAmount);
        threat.ResolutionRoundId = resolutionRoundId ?? "";
        threat.ResolutionDocumentId = resolutionDocumentId ?? "";
        threat.ResolutionReason = DescribeThreatBreachResolutionReason(threat.Stage);
        threat.UpdatedDay = currentDay;
        threat.ObligationRoundId = "";
        threat.ObligationClaimedDay = 0;
    }

    public static bool IsIssuerResolutionNoticePendingFor(
        WorldDiplomacyThreat threat,
        string issuerKingdomId)
    {
        return threat != null
            && threat.IssuerResolutionNoticePending
            && string.Equals(threat.IssuerKingdomId, issuerKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsIssuerResolutionNoticeOrphaned(
        WorldDiplomacyThreat notice,
        string resolutionRoundId,
        string issuerKingdomId)
    {
        return IsIssuerResolutionNoticePendingFor(notice, issuerKingdomId)
            && !string.Equals(notice.ResolutionRoundId, resolutionRoundId, StringComparison.OrdinalIgnoreCase);
    }

    public static List<WorldDiplomacyThreat> SelectIssuerResolutionNotices(
        IEnumerable<WorldDiplomacyThreat> threats,
        string issuerKingdomId,
        string excludedRoundId,
        int limit)
    {
        return (threats ?? new List<WorldDiplomacyThreat>())
            .Where(x => IsIssuerResolutionNoticePendingFor(x, issuerKingdomId)
                && (string.IsNullOrEmpty(excludedRoundId)
                    || !string.Equals(x.ResolutionRoundId, excludedRoundId, StringComparison.OrdinalIgnoreCase)))
            .Take(limit)
            .ToList();
    }

    public static bool HasThreatPresentationDrift(
        IEnumerable<string> storedPresentedIds,
        IEnumerable<string> currentPresentedIds,
        IEnumerable<string> storedFollowThroughIds,
        IEnumerable<string> currentFollowThroughIds)
    {
        return !(storedPresentedIds ?? new List<string>()).SequenceEqual(
                currentPresentedIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase)
            || !(storedFollowThroughIds ?? new List<string>()).SequenceEqual(
                currentFollowThroughIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsCourtPropagationArrival(WorldDiplomacyPropagationArrival arrival)
    {
        return string.Equals(arrival?.Scope, "court", StringComparison.OrdinalIgnoreCase);
    }

    public static List<WorldDiplomacyPropagationArrival> NormalizePropagationArrivalList(
        IEnumerable<WorldDiplomacyPropagationArrival> arrivals)
    {
        foreach (WorldDiplomacyPropagationArrival arrival in
            (arrivals ?? new List<WorldDiplomacyPropagationArrival>()).Where(x => x != null))
        {
            if (string.IsNullOrWhiteSpace(arrival.Scope)) arrival.Scope = "civilian";
        }
        return OrderPropagationArrivalsByDueDate((arrivals ?? new List<WorldDiplomacyPropagationArrival>())
                .Where(x => x != null
                    && !string.IsNullOrWhiteSpace(x.DocumentId)
                    && (!string.IsNullOrWhiteSpace(x.SettlementId)
                        || (IsCourtPropagationArrival(x) && !string.IsNullOrWhiteSpace(x.KingdomId)))))
            .ToList();
    }

    public static List<WorldDiplomacyPolicySignal> NormalizePendingPolicySignals(
        IEnumerable<WorldDiplomacyPolicySignal> signals,
        int maxPending)
    {
        return (signals ?? new List<WorldDiplomacyPolicySignal>())
            .Where(x => x != null
                && !string.IsNullOrWhiteSpace(x.SignalKey)
                && !string.IsNullOrWhiteSpace(x.IssuerKingdomId)
                && !string.IsNullOrWhiteSpace(x.TargetKingdomId))
            .GroupBy(x => x.SignalKey, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.OrderByDescending(y => y.PublishedDay).First())
            .OrderByDescending(x => x.PublishedDay)
            .Take(maxPending)
            .ToList();
    }

    public static List<WorldDiplomacyPolicySignal> SelectRetainedPolicySignals(
        IEnumerable<WorldDiplomacyPolicySignal> signals,
        int day,
        int retentionDays,
        int maxPending)
    {
        return (signals ?? new List<WorldDiplomacyPolicySignal>())
            .Where(x => x != null
                && !string.IsNullOrWhiteSpace(x.SignalKey)
                && day - x.PublishedDay <= retentionDays)
            .OrderBy(x => x.PublishedDay)
            .ThenBy(x => x.SignalKey, StringComparer.OrdinalIgnoreCase)
            .Take(maxPending)
            .ToList();
    }

    public static List<string> NormalizeProcessedSignalKeys(
        IEnumerable<string> keys,
        int maxKeys)
    {
        List<string> normalized = (keys ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return normalized.Skip(Math.Max(0, normalized.Count - maxKeys)).ToList();
    }

    public static void CapToMostRecentEntries<T>(List<T> items, int maxCount)
    {
        if (items == null || items.Count <= maxCount) return;
        items.RemoveRange(0, items.Count - maxCount);
    }

    public static void NormalizeWarLedgerList(List<WorldDiplomacyWarLedger> ledgers)
    {
        if (ledgers == null) return;
        ledgers.RemoveAll(x => x == null
            || string.IsNullOrWhiteSpace(x.FirstKingdomId)
            || string.IsNullOrWhiteSpace(x.SecondKingdomId));
        foreach (WorldDiplomacyWarLedger ledger in ledgers)
        {
            ledger.SettlementChanges ??= new List<WorldDiplomacySettlementChange>();
            ledger.SettlementChanges.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.SettlementId));
        }
    }

    public static void NormalizeBattleRecords(List<WorldDiplomacyBattleFact> battles)
    {
        if (battles == null) return;
        battles.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.BattleId));
        foreach (WorldDiplomacyBattleFact battle in battles)
        {
            battle.AttackerKingdomIds ??= new List<string>();
            battle.DefenderKingdomIds ??= new List<string>();
            battle.AttackerLeaderNames ??= new List<string>();
            battle.DefenderLeaderNames ??= new List<string>();
        }
    }

    public static void NormalizeJobRecord(WorldDiplomacyJob job)
    {
        if (job == null) return;
        job.CandidateKingdomIds ??= new List<string>();
        job.TriggerDocumentIds ??= new List<string>();
        job.PresentedThreatDocumentIds = NormalizeThreatIdList(job.PresentedThreatDocumentIds);
        job.PresentedThreatFollowThroughDocumentIds = NormalizeThreatIdList(job.PresentedThreatFollowThroughDocumentIds);
        job.PresentedLegalActionSignature ??= "";
        job.ResultSettlementSlotId ??= "";
        job.LlmMessages ??= new List<WorldDiplomacyLlmMessage>();
        job.CompressionRoundIds ??= new List<string>();
        job.ForcedIntent = "";
    }

    public static bool IsJobOfKind(WorldDiplomacyJob job, string kind)
    {
        return string.Equals(job?.Kind, kind, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRecordInRound(string recordRoundId, string roundId)
    {
        return string.Equals(recordRoundId, roundId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasJobId(WorldDiplomacyJob job, string jobId)
    {
        return string.Equals(job?.JobId, jobId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasJobIdInSet(WorldDiplomacyJob job, ICollection<string> jobIds)
    {
        return job != null && jobIds != null && jobIds.Contains(job.JobId ?? "");
    }

    public static bool MatchesDocumentId(string recordDocumentId, string documentId)
    {
        return string.Equals(recordDocumentId, documentId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRetiredJob(WorldDiplomacyJob job)
    {
        return job == null
            || string.IsNullOrWhiteSpace(job.JobId)
            || IsJobOfKind(job, "participate")
            || IsJobOfKind(job, "round_compress")
            || (job.IsRelayTurn && job.Priority == 98 && !string.IsNullOrWhiteSpace(job.ForcedIntent))
            || (IsJobOfKind(job, "compress") && job.CompressionTargetTokens <= 0);
    }

    public static List<WorldDiplomacyDocument> SelectRoundCompressionDocuments(
        IEnumerable<WorldDiplomacyDocument> documents,
        string roundId,
        IEnumerable<string> compressionDocumentIds)
    {
        List<string> explicitIds = (compressionDocumentIds ?? new List<string>()).ToList();
        return (documents ?? new List<WorldDiplomacyDocument>())
            .Where(x => x != null
                && (string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase)
                    || explicitIds.Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase)))
            .ToList();
    }

    public static IOrderedEnumerable<WorldDiplomacyDocument> OrderDocumentsChronologically(
        IEnumerable<WorldDiplomacyDocument> documents)
    {
        return (documents ?? new List<WorldDiplomacyDocument>())
            .OrderBy(x => x.Day)
            .ThenBy(x => x.CreatedUtcTicks);
    }

    public static IOrderedEnumerable<WorldDiplomacyDocument> OrderDocumentsByRecency(
        IEnumerable<WorldDiplomacyDocument> documents)
    {
        return (documents ?? new List<WorldDiplomacyDocument>())
            .OrderByDescending(x => x.Day)
            .ThenByDescending(x => x.CreatedUtcTicks);
    }

    public static IOrderedEnumerable<WorldDiplomacyDocument> ThenOrderDocumentsByRecency(
        IOrderedEnumerable<WorldDiplomacyDocument> documents)
    {
        return (documents ?? new List<WorldDiplomacyDocument>().OrderBy(x => x.Day))
            .ThenByDescending(x => x.Day)
            .ThenByDescending(x => x.CreatedUtcTicks);
    }

    public static List<WorldDiplomacyDocument> SelectNewestDocumentsChronologically(
        IEnumerable<WorldDiplomacyDocument> documents,
        int maxCount)
    {
        return OrderDocumentsChronologically(
            OrderDocumentsByRecency(documents).Take(Math.Max(1, maxCount))).ToList();
    }

    public static List<WorldDiplomacyDocument> SelectRetainedDocuments(
        IEnumerable<WorldDiplomacyDocument> documents,
        Func<WorldDiplomacyDocument, bool> keepFirst,
        int maxCount)
    {
        return OrderDocumentsChronologically(
            ThenOrderDocumentsByRecency((documents ?? new List<WorldDiplomacyDocument>())
                    .Where(x => x != null)
                    .OrderByDescending(x => keepFirst != null && keepFirst(x)))
                .Take(Math.Max(1, maxCount))).ToList();
    }

    public static IOrderedEnumerable<WorldDiplomacyRelayArrival> OrderRelayArrivalsByDueDate(
        IEnumerable<WorldDiplomacyRelayArrival> arrivals)
    {
        return (arrivals ?? new List<WorldDiplomacyRelayArrival>())
            .OrderBy(x => x.DueDay)
            .ThenBy(x => x.Sequence);
    }

    public static IOrderedEnumerable<WorldDiplomacyPropagationArrival> OrderPropagationArrivalsByDueDate(
        IEnumerable<WorldDiplomacyPropagationArrival> arrivals)
    {
        return (arrivals ?? new List<WorldDiplomacyPropagationArrival>())
            .OrderBy(x => x.DueDay)
            .ThenBy(x => IsCourtPropagationArrival(x) ? 0 : 1)
            .ThenBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase);
    }

    public static IOrderedEnumerable<WorldDiplomacyCanonicalProtectedFact> OrderProtectedFactsBySequence(
        IEnumerable<WorldDiplomacyCanonicalProtectedFact> facts)
    {
        return (facts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
            .OrderBy(x => x.Sequence)
            .ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ThenBy(x => x.SourceKey, StringComparer.OrdinalIgnoreCase);
    }

    public static IOrderedEnumerable<WorldDiplomacyCanonicalProtectedFact> OrderProtectedFactsBySequenceDescending(
        IEnumerable<WorldDiplomacyCanonicalProtectedFact> facts)
    {
        return (facts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
            .OrderByDescending(x => x.Sequence)
            .ThenByDescending(x => x.Kind, StringComparer.Ordinal)
            .ThenByDescending(x => x.SourceKey, StringComparer.OrdinalIgnoreCase);
    }

    public static IOrderedEnumerable<WorldDiplomacyCanonicalHistoryEntry> OrderDeltaEntriesBySequence(
        IEnumerable<WorldDiplomacyCanonicalHistoryEntry> entries)
    {
        return (entries ?? new List<WorldDiplomacyCanonicalHistoryEntry>())
            .OrderBy(x => x.Sequence);
    }

    public static IEnumerable<WorldDiplomacyCanonicalHistoryEntry> SelectDeltaEntriesThrough(
        IEnumerable<WorldDiplomacyCanonicalHistoryEntry> entries,
        long throughSequence)
    {
        return OrderDeltaEntriesBySequence((entries ?? new List<WorldDiplomacyCanonicalHistoryEntry>())
            .Where(x => x != null && x.Sequence <= throughSequence));
    }

    public static List<string> NormalizeIdListPreserveOrder(IEnumerable<string> ids)
    {
        return (ids ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> NormalizeTrimmedIdListPreserveOrder(IEnumerable<string> ids)
    {
        return (ids ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> NormalizeOrderedIdList(IEnumerable<string> ids)
    {
        return (ids ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static Dictionary<string, long> NormalizeRevisionMapLong(
        IEnumerable<KeyValuePair<string, long>> entries)
    {
        return (entries ?? Enumerable.Empty<KeyValuePair<string, long>>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => Math.Max(0L, x.Last().Value), StringComparer.OrdinalIgnoreCase);
    }

    public static Dictionary<string, string> NormalizeRevisionMapString(
        IEnumerable<KeyValuePair<string, string>> entries)
    {
        return (entries ?? Enumerable.Empty<KeyValuePair<string, string>>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().Value ?? "", StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsDeltaEntryRetired(
        WorldDiplomacyCanonicalHistoryEntry entry,
        long coveredThroughSequence)
    {
        return entry == null
            || entry.Sequence <= coveredThroughSequence
            || string.IsNullOrWhiteSpace(entry.Kind);
    }

    public static long ResolveNextDeltaSequence(
        long coveredThroughSequence,
        long declaredNextSequence,
        IReadOnlyList<WorldDiplomacyCanonicalHistoryEntry> deltaEntries)
    {
        long lastSequence = coveredThroughSequence;
        if (deltaEntries != null && deltaEntries.Count > 0)
        {
            lastSequence = Math.Max(coveredThroughSequence, deltaEntries[deltaEntries.Count - 1].Sequence);
        }
        return Math.Max(Math.Max(1L, declaredNextSequence), lastSequence + 1L);
    }

    public static void NormalizeDeltaEntryFields(WorldDiplomacyCanonicalHistoryEntry entry)
    {
        if (entry == null) return;
        entry.TargetKingdomIds = NormalizeIdListPreserveOrder(entry.TargetKingdomIds);
        entry.RespondingToOfferDocumentId = (entry.RespondingToOfferDocumentId ?? "").Trim();
        entry.RespondingToThreatDocumentId = (entry.RespondingToThreatDocumentId ?? "").Trim();
        if (entry.ActionFacts != null)
        {
            entry.ActionFacts = NormalizeIdListPreserveOrder(entry.ActionFacts);
        }
    }

    public static bool IsAutonomousOpeningJob(WorldDiplomacyJob job)
    {
        return job != null
            && IsJobOfKind(job, "generate")
            && !job.IsResponse
            && job.AllowUntargeted
            && string.IsNullOrWhiteSpace(job.TargetKingdomId);
    }

    public static string FirstNonEmpty(params string[] values)
    {
        return (values ?? Array.Empty<string>()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
    }

    public static List<WorldDiplomacyDocument> SelectPublishedRoundDocuments(
        IEnumerable<WorldDiplomacyDocument> documents,
        string roundId)
    {
        return OrderDocumentsChronologically((documents ?? new List<WorldDiplomacyDocument>())
            .Where(x => x != null
                && x.IsReadyForPublication
                && string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public static bool IsSuccessfulWarDocumentAgainst(
        WorldDiplomacyDocument document,
        string targetKingdomId)
    {
        if (document == null || string.IsNullOrWhiteSpace(targetKingdomId)) return false;
        if (document.Actions?.Count > 0)
        {
            return document.Actions.Any(x => x != null
                && IsWarDeclarationAction(x.ChangedDiplomaticState,
                    WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent),
                    x.TargetKingdomId, targetKingdomId));
        }
        return IsWarDeclarationDocument(false, document.ChangedDiplomaticState,
            WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent),
            document.TargetKingdomId, targetKingdomId);
    }

    public static bool IsWarResponseSlotAnswered(
        WorldDiplomacyResultSettlementSlot slot,
        IReadOnlyCollection<WorldDiplomacyDocument> published)
    {
        if (slot == null || published == null || published.Count == 0) return false;
        List<WorldDiplomacyDocument> wars = published
            .Where(x => x != null
                && (slot.SourceDocumentIds ?? new List<string>())
                    .Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase)
                && IsSuccessfulWarDocumentAgainst(x, slot.KingdomId))
            .ToList();
        if (wars.Count == 0) return false;
        return wars.All(war => published.Any(response => response != null
            && response.IsReadyForPublication
            && string.Equals(response.AuthorKingdomId, slot.KingdomId, StringComparison.OrdinalIgnoreCase)
            && (response.Day > war.Day
                || (response.Day == war.Day && response.CreatedUtcTicks > war.CreatedUtcTicks))));
    }

    public static List<string> SelectSlotKindsExcept(string kind, string excludedKind)
    {
        return (kind ?? "").Split('+')
            .Where(x => !string.IsNullOrWhiteSpace(x)
                && !string.Equals(x, excludedKind, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static void RemoveAnsweredWarResponseSlots(
        WorldDiplomacyRound round,
        IReadOnlyCollection<WorldDiplomacyDocument> published)
    {
        if (round == null || published == null || published.Count == 0) return;
        foreach (WorldDiplomacyResultSettlementSlot slot in
            (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>()).ToList())
        {
            if (slot == null || !SettlementSlotKindContains(slot.Kind, "war_response")) continue;
            if (!IsWarResponseSlotAnswered(slot, published)) continue;
            List<string> remainingKinds = SelectSlotKindsExcept(slot.Kind, "war_response");
            if (remainingKinds.Count == 0) round.ResultSettlementSlots.Remove(slot);
            else slot.Kind = string.Join("+", remainingKinds);
        }
    }

    public static WorldDiplomacyPolicySignal ClonePolicySignalRecord(WorldDiplomacyPolicySignal signal)
    {
        if (signal == null) return null;
        return new WorldDiplomacyPolicySignal
        {
            SignalKey = signal.SignalKey ?? "",
            PolicyId = signal.PolicyId ?? "",
            PolicyKind = string.IsNullOrWhiteSpace(signal.PolicyKind) ? "kingdom" : signal.PolicyKind.Trim(),
            PolicyName = signal.PolicyName ?? "",
            PolicySummary = signal.PolicySummary ?? "",
            IssuerKingdomId = signal.IssuerKingdomId ?? "",
            IssuerKingdomName = signal.IssuerKingdomName ?? "",
            TargetKingdomId = signal.TargetKingdomId ?? "",
            TargetKingdomName = signal.TargetKingdomName ?? "",
            DirectEffect = signal.DirectEffect ?? "",
            PublishedDay = Math.Max(0, signal.PublishedDay),
        };
    }

    public static bool IsSettlementSlotAtStatus(
        WorldDiplomacyResultSettlementSlot slot, string status)
    {
        return string.Equals(slot?.Status, status, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOpenSettlementSlotStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            || string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "inflight", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "scheduled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "waiting_player", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeSettlementSlotStatus(string status)
    {
        return string.Equals(status, "waiting_player", StringComparison.OrdinalIgnoreCase) ? "waiting_player"
            : string.Equals(status, "inflight", StringComparison.OrdinalIgnoreCase) ? "inflight"
            : string.Equals(status, "scheduled", StringComparison.OrdinalIgnoreCase) ? "scheduled"
            : "pending";
    }

    public static List<string> MergeDistinctIds(IEnumerable<string> ids)
    {
        return (ids ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string MergeSlotKinds(IEnumerable<WorldDiplomacyResultSettlementSlot> slots)
    {
        return string.Join("+", (slots ?? new List<WorldDiplomacyResultSettlementSlot>())
            .Where(x => x != null)
            .SelectMany(x => (x.Kind ?? "").Split('+'))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public static List<WorldDiplomacyResultSettlementSlot> MergeResultSettlementSlots(
        IEnumerable<WorldDiplomacyResultSettlementSlot> slots,
        string currentSlotId,
        out string normalizedCurrentSlotId)
    {
        normalizedCurrentSlotId = "";
        List<WorldDiplomacyResultSettlementSlot> normalized = new List<WorldDiplomacyResultSettlementSlot>();
        foreach (IGrouping<string, WorldDiplomacyResultSettlementSlot> group in
            (slots ?? new List<WorldDiplomacyResultSettlementSlot>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId))
            .GroupBy(x => x.KingdomId, StringComparer.OrdinalIgnoreCase))
        {
            List<WorldDiplomacyResultSettlementSlot> pending = group
                .Where(x => IsOpenSettlementSlotStatus(x.Status))
                .ToList();
            if (pending.Count == 0) continue;
            WorldDiplomacyResultSettlementSlot slot = (!string.IsNullOrWhiteSpace(currentSlotId)
                ? pending.FirstOrDefault(x => string.Equals(x.SlotId, currentSlotId, StringComparison.OrdinalIgnoreCase))
                : null)
                ?? pending.FirstOrDefault(x => IsSettlementSlotAtStatus(x, "waiting_player"))
                ?? pending.FirstOrDefault(x => IsSettlementSlotAtStatus(x, "inflight"))
                ?? pending.FirstOrDefault(x => IsSettlementSlotAtStatus(x, "scheduled"))
                ?? pending[0];
            slot.SlotId = string.IsNullOrWhiteSpace(slot.SlotId) ? "diplomacy_result_slot:" + group.Key : slot.SlotId;
            slot.Kind = MergeSlotKinds(pending);
            if (string.IsNullOrWhiteSpace(slot.Kind)) slot.Kind = "route";
            slot.Status = NormalizeSettlementSlotStatus(slot.Status);
            slot.SourceDocumentIds = MergeDistinctIds(
                pending.SelectMany(x => x.SourceDocumentIds ?? new List<string>()));
            slot.RelatedKingdomIds = MergeDistinctIds(
                pending.SelectMany(x => x.RelatedKingdomIds ?? new List<string>()));
            if (!string.IsNullOrWhiteSpace(currentSlotId)
                && pending.Any(x => string.Equals(x.SlotId, currentSlotId, StringComparison.OrdinalIgnoreCase)))
            {
                normalizedCurrentSlotId = slot.SlotId;
            }
            normalized.Add(slot);
        }
        return normalized;
    }

    public static List<string> NormalizeIntentList(IEnumerable<string> intents)
    {
        return (intents ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(WorldDiplomacyIntentVocabulary.NormalizeIntent)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<WorldDiplomacyPolicySignal> NormalizeAttachedPolicySignals(
        IEnumerable<WorldDiplomacyPolicySignal> signals,
        int maxPending)
    {
        return (signals ?? new List<WorldDiplomacyPolicySignal>())
            .Where(x => x != null
                && !string.IsNullOrWhiteSpace(x.SignalKey)
                && !string.IsNullOrWhiteSpace(x.PolicyId)
                && !string.IsNullOrWhiteSpace(x.IssuerKingdomId)
                && !string.IsNullOrWhiteSpace(x.TargetKingdomId))
            .GroupBy(x => x.SignalKey.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => ClonePolicySignalRecord(group.OrderByDescending(x => x.PublishedDay).First()))
            .OrderByDescending(x => x.PublishedDay)
            .ThenBy(x => x.SignalKey, StringComparer.OrdinalIgnoreCase)
            .Take(maxPending)
            .ToList();
    }

    public static void NormalizeRoundRecordCollections(
        WorldDiplomacyRound round,
        int maxPendingSignals,
        int maxTechnicalFailures)
    {
        if (round == null) return;
        round.Participants ??= new List<WorldDiplomacyRoundParticipant>();
        round.RelayRouteKingdomIds ??= new List<string>();
        round.PendingOffers ??= new List<WorldDiplomacyRoundOffer>();
        round.ResultSettlementSlots ??= new List<WorldDiplomacyResultSettlementSlot>();
        round.ResultSettlementWarDocumentIds ??= new List<string>();
        round.ResultSettlementTriggerDocumentId ??= "";
        round.ResultSettlementCloseReason ??= "";
        round.ResultSettlementRoundStatus = NormalizeResultSettlementStatus(round.ResultSettlementRoundStatus);
        round.ResultSettlementCurrentSlotId ??= "";
        round.ResultSettlementWarDocumentIds = MergeDistinctIds(round.ResultSettlementWarDocumentIds);
        string normalizedCurrentSettlementSlotId;
        round.ResultSettlementSlots = MergeResultSettlementSlots(
            round.ResultSettlementSlots, round.ResultSettlementCurrentSlotId,
            out normalizedCurrentSettlementSlotId);
        round.ResultSettlementCurrentSlotId = normalizedCurrentSettlementSlotId;
        if (string.IsNullOrWhiteSpace(normalizedCurrentSettlementSlotId))
        {
            round.ResultSettlementPlayerWaitingSinceDay = 0;
        }
        round.LlmTranscript ??= new List<WorldDiplomacyLlmMessage>();
        round.LlmTranscript.Clear();
        round.LlmProfiledKingdomIds ??= new List<string>();
        round.ExternalSignalKeys ??= new List<string>();
        round.AttachedPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
        round.PotentialActionIntents ??= new List<string>();
        round.CommonContractSnapshot = "";
        round.CommonContractSnapshotInitialized = false;
        round.CachePrefix = "";
        round.PotentialActionIntents = NormalizeIntentList(round.PotentialActionIntents);
        round.ExternalSignalKeys = MergeDistinctIds(round.ExternalSignalKeys);
        round.AttachedPolicySignals = NormalizeAttachedPolicySignals(
            round.AttachedPolicySignals, maxPendingSignals);
        round.ExternalOpeningContext ??= "";
        round.EventSourceType ??= "";
        round.EventMotif ??= "";
        round.EventLocation ??= "";
        round.AllowedFiction ??= "";
        round.ForbiddenFiction ??= "";
        round.LlmProfiledKingdomIds.Clear();
        round.LlmLastStateSignatureByKingdom ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        round.LlmLastStateSignatureByKingdom.Clear();
        round.ConsecutiveTechnicalGenerationFailures = Math.Max(0,
            Math.Min(maxTechnicalFailures, round.ConsecutiveTechnicalGenerationFailures));
        round.PendingOffers.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.SourceDocumentId));
        foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers)
        {
            offer.SourceDocumentId = (offer.SourceDocumentId ?? "").Trim();
            offer.SourceActionId = (offer.SourceActionId ?? "").Trim();
        }
    }

    public static bool IsOfferOfStatus(WorldDiplomacyRoundOffer offer, string status)
    {
        return string.Equals(offer?.Status, status, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOpenOfferToTarget(WorldDiplomacyRoundOffer offer, string targetKingdomId)
    {
        return offer != null
            && IsOfferOfStatus(offer, "open")
            && string.Equals(offer.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOpenDirectedOffer(
        WorldDiplomacyRoundOffer offer,
        string proposerKingdomId,
        string targetKingdomId)
    {
        return offer != null
            && IsOfferOfStatus(offer, "open")
            && string.Equals(offer.ProposerKingdomId, proposerKingdomId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(offer.TargetKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesOfferSource(
        WorldDiplomacyRoundOffer offer,
        string sourceDocumentId,
        string sourceActionId)
    {
        return offer != null
            && string.Equals(offer.SourceDocumentId, sourceDocumentId, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(sourceActionId)
                || string.Equals(offer.SourceActionId, sourceActionId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsPlayerOpportunityOfStatus(WorldDiplomacyPlayerOpportunity opportunity, string status)
    {
        return string.Equals(opportunity?.Status, status, StringComparison.OrdinalIgnoreCase);
    }

    public static bool NormalizePendingOfferSourceActionBinding(
        WorldDiplomacyRoundOffer offer,
        IReadOnlyDictionary<string, WorldDiplomacyDocument> documentsById)
    {
        if (offer == null || documentsById == null) return false;
        bool isOpen = IsOfferOfStatus(offer, "open");
        if (!documentsById.TryGetValue(offer.SourceDocumentId ?? "", out WorldDiplomacyDocument source)
            || source == null
            || !source.IsReadyForPublication
            || !string.Equals(source.AuthorKingdomId, offer.ProposerKingdomId, StringComparison.OrdinalIgnoreCase))
        {
            if (!isOpen) return false;
            offer.Status = "invalidated";
            return true;
        }
        if (source.Actions == null || source.Actions.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(offer.SourceActionId)) return false;
            offer.SourceActionId = "";
            return true;
        }
        List<WorldDiplomacyDocumentAction> matches = source.Actions
            .Where(x => x != null
                && string.Equals(x.TargetKingdomId, offer.TargetKingdomId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent),
                    WorldDiplomacyIntentVocabulary.NormalizeIntent(offer.Intent),
                    StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToList();
        if (matches.Count == 1 && !string.IsNullOrWhiteSpace(matches[0].ActionId))
        {
            string normalizedActionId = matches[0].ActionId.Trim();
            if (string.Equals(offer.SourceActionId, normalizedActionId, StringComparison.OrdinalIgnoreCase)) return false;
            offer.SourceActionId = normalizedActionId;
            return true;
        }
        if (!isOpen) return false;
        offer.Status = "invalidated";
        return true;
    }

    public static int CountNonExpiredPendingOffers(IEnumerable<WorldDiplomacyRoundOffer> offers)
    {
        return (offers ?? new List<WorldDiplomacyRoundOffer>())
            .Count(x => x != null && !IsOfferOfStatus(x, "expired"));
    }

    public static void NormalizeRoundAttemptCount(WorldDiplomacyRound round)
    {
        if (round == null || round.DiplomaticActionAttemptCount > 0) return;
        round.DiplomaticActionAttemptCount = CountNonExpiredPendingOffers(round.PendingOffers);
        if (round.ExecutedActionCount > round.DiplomaticActionAttemptCount)
        {
            round.DiplomaticActionAttemptCount = round.ExecutedActionCount;
        }
    }

    public static int ResolveStoredRoundDurationDays(int softEndDay, int startedDay, int fallbackDuration)
    {
        return softEndDay > startedDay ? softEndDay - startedDay : fallbackDuration;
    }

    public static void NormalizeRoundScheduleDays(
        WorldDiplomacyRound round,
        int storedTargetDurationDays,
        int relayPassDays,
        int hardDurationDays)
    {
        if (round == null) return;
        if (round.SoftEndDay <= round.StartedDay) round.SoftEndDay = round.StartedDay + storedTargetDurationDays;
        if (round.RelayPassDurationDays <= 0) round.RelayPassDurationDays = relayPassDays;
        if (round.HardEndDay <= 0)
        {
            round.HardEndDay = Math.Max(round.SoftEndDay, round.StartedDay + hardDurationDays);
        }
    }

    public static bool IsClosedRoundWithoutFinalDocument(WorldDiplomacyRound round)
    {
        return string.Equals(round?.State, "closed", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(round.FinalDocumentId);
    }

    public static string SelectFinalRoundDocumentId(
        IEnumerable<WorldDiplomacyDocument> documents,
        string roundId)
    {
        return OrderDocumentsChronologically((documents ?? new List<WorldDiplomacyDocument>())
            .Where(x => x != null && string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase)))
            .LastOrDefault()?.DocumentId ?? "";
    }

    public static WorldDiplomacyResultSettlementSlot SelectWaitingPlayerSettlementSlot(
        IEnumerable<WorldDiplomacyResultSettlementSlot> slots,
        string currentSlotId,
        string kingdomId)
    {
        return (slots ?? new List<WorldDiplomacyResultSettlementSlot>()).FirstOrDefault(x => x != null
            && string.Equals(x.SlotId, currentSlotId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
            && IsSettlementSlotAtStatus(x, "waiting_player"));
    }

    public static bool IsParticipantWaitingSettlementSlot(
        bool settlementPending,
        IEnumerable<WorldDiplomacyResultSettlementSlot> slots,
        string currentSlotId,
        string participantKingdomId)
    {
        return settlementPending
            && SelectWaitingPlayerSettlementSlot(slots, currentSlotId, participantKingdomId) != null;
    }

    public static void ReleaseNonWaitingSettlementReplies(WorldDiplomacyRound round)
    {
        if (round?.Participants == null) return;
        foreach (WorldDiplomacyRoundParticipant participant in round.Participants.Where(x => x != null))
        {
            if (!IsParticipantWaitingSettlementSlot(
                round.ResultSettlementPending, round.ResultSettlementSlots,
                round.ResultSettlementCurrentSlotId, participant.KingdomId))
            {
                participant.MandatoryReplyPending = false;
            }
        }
    }

    public static int CountAutomaticRoundDocuments(
        IEnumerable<WorldDiplomacyDocument> documents,
        string roundId)
    {
        return (documents ?? new List<WorldDiplomacyDocument>())
            .Count(x => x != null && !x.IsPlayerAuthored
                && string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase));
    }

    public static List<WorldDiplomacyJob> SelectRetiredArchitectureJobs(
        IEnumerable<WorldDiplomacyJob> jobs)
    {
        return (jobs ?? new List<WorldDiplomacyJob>())
            .Where(x => x != null
                && (IsJobOfKind(x, "generate")
                    || IsJobOfKind(x, "round_plan")
                    || !string.IsNullOrWhiteSpace(x.ForcedIntent)))
            .ToList();
    }

    public static List<WorldDiplomacyJob> SelectRoundJobsOfKinds(
        IEnumerable<WorldDiplomacyJob> jobs,
        string roundId,
        params string[] kinds)
    {
        return (jobs ?? new List<WorldDiplomacyJob>())
            .Where(x => x != null
                && string.Equals(x.RoundId, roundId, StringComparison.OrdinalIgnoreCase)
                && (kinds ?? new string[0]).Any(kind => IsJobOfKind(x, kind)))
            .ToList();
    }

    public static void NormalizeSettlementKnowledgeRecords(
        IEnumerable<WorldDiplomacySettlementKnowledge> knowledge)
    {
        foreach (WorldDiplomacySettlementKnowledge entry in
            (knowledge ?? new List<WorldDiplomacySettlementKnowledge>()).Where(x => x != null))
        {
            entry.DocumentIds ??= new List<string>();
        }
    }

    public static void NormalizeKingdomKnowledgeRecords(
        IEnumerable<WorldDiplomacyKingdomKnowledge> knowledge)
    {
        foreach (WorldDiplomacyKingdomKnowledge entry in
            (knowledge ?? new List<WorldDiplomacyKingdomKnowledge>()).Where(x => x != null))
        {
            entry.DocumentIds ??= new List<string>();
        }
    }

    public static void NormalizeParticipationRequestRecords(
        IEnumerable<WorldDiplomacyParticipationRequest> requests)
    {
        foreach (WorldDiplomacyParticipationRequest request in
            (requests ?? new List<WorldDiplomacyParticipationRequest>()).Where(x => x != null))
        {
            request.TriggerDocumentIds ??= new List<string>();
        }
    }

    public static List<WorldDiplomacyRelayArrival> NormalizeRelayArrivalList(
        IEnumerable<WorldDiplomacyRelayArrival> arrivals)
    {
        return OrderRelayArrivalsByDueDate((arrivals ?? new List<WorldDiplomacyRelayArrival>())
                .Where(x => x != null
                    && !string.IsNullOrWhiteSpace(x.RoundId)
                    && !string.IsNullOrWhiteSpace(x.ToKingdomId)))
            .ToList();
    }

    public static List<WorldDiplomacyPlayerOpportunity> NormalizePlayerOpportunityList(
        IEnumerable<WorldDiplomacyPlayerOpportunity> opportunities,
        int maxEntries)
    {
        foreach (WorldDiplomacyPlayerOpportunity opportunity in
            (opportunities ?? new List<WorldDiplomacyPlayerOpportunity>()).Where(x => x != null))
        {
            opportunity.KnownDocumentIds ??= new List<string>();
        }
        return (opportunities ?? new List<WorldDiplomacyPlayerOpportunity>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.RoundId))
            .OrderByDescending(x => x.ArrivedDay)
            .Take(maxEntries)
            .ToList();
    }

    public static void NormalizeRoundSummaryRecord(
        WorldDiplomacyRoundSummary summary,
        IEnumerable<WorldDiplomacyDocument> documents)
    {
        if (summary == null) return;
        summary.SourceDocumentIds ??= new List<string>();
        summary.Facts ??= new List<WorldDiplomacyRoundFact>();
        summary.KingdomIds ??= new List<string>();
        foreach (WorldDiplomacyRoundFact fact in summary.Facts.Where(x => x != null))
        {
            fact.Kind = string.IsNullOrWhiteSpace(fact.Kind) ? "declaration" : fact.Kind;
            fact.SourceDocumentIds ??= new List<string>();
            fact.KingdomIds ??= new List<string>();
        }
        if (summary.KingdomIds.Count == 0)
        {
            summary.KingdomIds = (documents ?? new List<WorldDiplomacyDocument>())
                .Where(x => x != null
                    && summary.SourceDocumentIds.Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase))
                .SelectMany(x => new[] { x.AuthorKingdomId, x.TargetKingdomId }
                    .Concat(x.AddressedKingdomIds ?? new List<string>()))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public static void NormalizeCompressionSummaryRecord(WorldDiplomacyCompressionSummary summary)
    {
        if (summary == null) return;
        summary.SourceRoundIds ??= new List<string>();
        summary.KingdomIds ??= new List<string>();
        summary.ConfirmedResults ??= new List<string>();
    }

    public static List<WorldDiplomacyRound> SelectRetainedCompletedRounds(
        IEnumerable<WorldDiplomacyRound> rounds,
        int maxCount)
    {
        return (rounds ?? new List<WorldDiplomacyRound>())
            .Where(x => x != null)
            .OrderByDescending(x => x.CompletedDay)
            .Take(maxCount)
            .ToList();
    }

    public static List<WorldDiplomacyRoundSummary> SelectRetainedRoundSummaries(
        IEnumerable<WorldDiplomacyRoundSummary> summaries,
        int maxCount)
    {
        return (summaries ?? new List<WorldDiplomacyRoundSummary>())
            .Where(x => x != null)
            .OrderByDescending(x => x.CreatedDay)
            .Take(maxCount)
            .ToList();
    }

    public static List<WorldDiplomacyAnnualSummary> SelectRetainedAnnualSummaries(
        IEnumerable<WorldDiplomacyAnnualSummary> summaries,
        int maxCount)
    {
        return (summaries ?? new List<WorldDiplomacyAnnualSummary>())
            .Where(x => x != null)
            .OrderByDescending(x => x.Year)
            .Take(maxCount)
            .ToList();
    }

    public static List<WorldDiplomacyCompressionSummary> SelectRetainedCompressionSummaries(
        IEnumerable<WorldDiplomacyCompressionSummary> summaries,
        int maxCount)
    {
        return (summaries ?? new List<WorldDiplomacyCompressionSummary>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.BatchId))
            .OrderByDescending(x => x.CreatedDay)
            .Take(maxCount)
            .ToList();
    }

    public static int CalculatePropagationDays(float distance, float maximumDistance, int maximumDays)
    {
        if (maximumDistance <= 0.01f) return Math.Max(1, maximumDays);
        return Math.Max(1, Math.Min(maximumDays, (int)Math.Ceiling(distance / maximumDistance * maximumDays)));
    }

    public static int ParseCompressionSequence(string batchId)
    {
        string text = batchId ?? "";
        int separator = text.LastIndexOf('_');
        return separator >= 0 && int.TryParse(text.Substring(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Math.Max(0, value) : 0;
    }
    public static bool UsesCanonicalHistory(WorldDiplomacyJob job)
    {
        return job != null && (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")
            || WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress"));
    }

    public static bool HasOpenDiplomaticThreatForRound(
        IEnumerable<WorldDiplomacyThreat> threats, string roundId)
    {
        if (string.IsNullOrWhiteSpace(roundId)) return false;
        return (threats ?? Enumerable.Empty<WorldDiplomacyThreat>()).Any(x =>
            IsOpenDiplomaticThreatStatus(x?.Status)
            && IsThreatLinkedToRound(x, roundId));
    }

    public static string PairKey(string first, string second)
    {
        string a = first ?? "";
        string b = second ?? "";
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0 ? a + "|" + b : b + "|" + a;
    }

    public static bool IsWarResponseNoActionAllowed(
        WorldDiplomacyRound round, string slotId, string authorId, string targetId,
        Func<string, WorldDiplomacyDocument> resolveDocument)
    {
        if (round == null
            || !IsWarResponseSlotAuthorized(
                round.ResultSettlementPending,
                !string.IsNullOrWhiteSpace(authorId),
                !string.IsNullOrWhiteSpace(targetId),
                IsActiveRoundState(round.State),
                !string.IsNullOrWhiteSpace(slotId),
                string.Equals(round.ResultSettlementCurrentSlotId, slotId, StringComparison.OrdinalIgnoreCase))) return false;
        WorldDiplomacyResultSettlementSlot slot = SelectSettlementSlotBySlotAndKingdom(
            round.ResultSettlementSlots, slotId, authorId);
        if (!(slot != null && SettlementSlotKindContains(slot.Kind, "war_response"))) return false;
        if (!IsSettlementSlotRelatedTo(slot, targetId)) return false;
        foreach (string sourceDocumentId in slot.SourceDocumentIds ?? new List<string>())
        {
            WorldDiplomacyDocument war = resolveDocument(sourceDocumentId);
            if (!IsWarResponseSourceDocument(
                war, round.RoundId, targetId)) continue;
            if (war.Actions?.Any(x => x != null
                && IsWarDeclarationAction(
                    x.ChangedDiplomaticState, WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), x.TargetKingdomId, authorId)) == true) return true;
            if (IsWarDeclarationDocument(
                war.Actions != null && war.Actions.Count > 0,
                war.ChangedDiplomaticState,
                WorldDiplomacyIntentVocabulary.NormalizeIntent(war.Intent),
                war.TargetKingdomId,
                authorId)) return true;
        }
        return false;
    }


    public static bool IsImmediateWarResponsePeaceSuppressed(
        WorldDiplomacyRound round, string slotId, string authorId, string targetId,
        Func<string, WorldDiplomacyDocument> resolveDocument)
    {
        return IsWarResponseNoActionAllowed(
            round,
            FirstNonEmpty(slotId, round?.ResultSettlementCurrentSlotId),
            authorId, targetId, resolveDocument);
    }

    public static bool TryResolveUniqueOpenProposalForRound(
        WorldDiplomacyRound round, string responderId, string proposerId, string proposalIntent,
        out string sourceDocumentId)
    {
        return TryResolveUniqueOpenProposalForRound(
            round, responderId, proposerId, proposalIntent, out sourceDocumentId, out _);
    }

    public static bool TryResolveUniqueOpenProposalForRound(
        WorldDiplomacyRound round, string responderId, string proposerId, string proposalIntent,
        out string sourceDocumentId, out string sourceActionId)
    {
        sourceDocumentId = "";
        sourceActionId = "";
        if (round == null || string.IsNullOrWhiteSpace(responderId) || string.IsNullOrWhiteSpace(proposerId)
            || !WorldDiplomacyIntentVocabulary.IsProposalIntent(proposalIntent)) return false;
        return TryResolveUniqueOpenProposal(
            round.PendingOffers, responderId, proposerId, proposalIntent,
            out sourceDocumentId, out sourceActionId);
    }

    public static bool TryResolveOpenProposalFor(
        WorldDiplomacyJob job, string responderId, string proposerId, string proposalIntent,
        Func<string, WorldDiplomacyRound> resolveRound, out string sourceDocumentId)
    {
        sourceDocumentId = "";
        if (job == null || string.IsNullOrWhiteSpace(responderId) || string.IsNullOrWhiteSpace(proposerId)
            || !WorldDiplomacyIntentVocabulary.IsProposalIntent(proposalIntent)) return false;
        WorldDiplomacyRound round = resolveRound(FirstNonEmpty(job.RoundId, job.ExchangeId));
        return TryResolveUniqueOpenProposalForRound(
            round, responderId, proposerId, proposalIntent, out sourceDocumentId);
    }

    public static bool HasOpenProposalForDocument(
        WorldDiplomacyDocument response, string responderId, string proposerId, string proposalIntent,
        Func<string, WorldDiplomacyRound> resolveRound)
    {
        if (response == null || string.IsNullOrWhiteSpace(responderId) || string.IsNullOrWhiteSpace(proposerId)
            || !WorldDiplomacyIntentVocabulary.IsProposalIntent(proposalIntent)) return false;
        if (!response.IsPlayerAuthored && string.IsNullOrWhiteSpace(response.RespondingToOfferDocumentId)) return false;
        WorldDiplomacyRound round = resolveRound(response.RoundId);
        return round?.PendingOffers?.Any(x => x != null
            && IsOpenDirectedOffer(x, proposerId, responderId)
            && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase)
            && (response.IsPlayerAuthored || MatchesOfferSource(
                x, response.RespondingToOfferDocumentId, response.RespondingToOfferActionId))) == true;
    }

    public static bool IsEnforcingRejectedUltimatum(
        List<WorldDiplomacyThreat> threats, string authorId, string targetId)
    {
        return !string.IsNullOrWhiteSpace(authorId) && !string.IsNullOrWhiteSpace(targetId)
            && IsRejectedUltimatumEnforceable(SelectOpenThreatBetween(threats, authorId, targetId));
    }

    public static void UpdateDiplomaticThreatComplianceDocumentResult(
        WorldDiplomacyThreat threat, Func<string, WorldDiplomacyDocument> resolveDocument)
    {
        if (threat == null) return;
        WorldDiplomacyDocument document = resolveDocument(threat.ComplianceDocumentId);
        if (document == null) return;
        document.ChangedDiplomaticState = true;
        document.MechanicalResult = "已明确服从" + DescribeThreatStage(threat.Stage)
            + (IsThreatCancellationStatusCancelled(threat.PolicyConditionCancellationStatus)
                ? "；附带政策《" + FirstNonEmpty(threat.PolicyConditionPolicyName, threat.PolicyConditionPolicyId) + "》已取消"
                : "");
    }
	public static void MarkOpenBilateralOffersAccepted(
		WorldDiplomacyRound round,
		string firstKingdomId,
		string secondKingdomId,
		WorldDiplomacyOfferDomain domain)
	{
		if (round == null || string.IsNullOrWhiteSpace(firstKingdomId) || string.IsNullOrWhiteSpace(secondKingdomId)
			|| string.Equals(firstKingdomId, secondKingdomId, StringComparison.OrdinalIgnoreCase)
			|| domain == WorldDiplomacyOfferDomain.None) return;
		foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
		{
			if (!IsOfferOfStatus(offer, "open")
				|| !WorldDiplomacyOfferCooldownRules.TryGetProposalDomain(offer.Intent, out WorldDiplomacyOfferDomain offerDomain)
				|| offerDomain != domain) continue;
			bool samePair = (string.Equals(offer.ProposerKingdomId, firstKingdomId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(offer.TargetKingdomId, secondKingdomId, StringComparison.OrdinalIgnoreCase))
				|| (string.Equals(offer.ProposerKingdomId, secondKingdomId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(offer.TargetKingdomId, firstKingdomId, StringComparison.OrdinalIgnoreCase));
			if (samePair) offer.Status = "accepted";
		}
	}

	public static float Clamp01(float value)
	{
		return Math.Max(0f, Math.Min(1f, value));
	}

	public static float CalculateCessionScore(WarSituationSnapshot snapshot, int lostFiefCount, bool authorPerspective)
	{
		float ownProgress = authorPerspective ? snapshot.AuthorProgress : snapshot.TargetProgress;
		float enemyProgress = authorPerspective ? snapshot.TargetProgress : snapshot.AuthorProgress;
		float ownStrength = authorPerspective ? snapshot.AuthorStrength : snapshot.TargetStrength;
		float enemyStrength = authorPerspective ? snapshot.TargetStrength : snapshot.AuthorStrength;
		int suffered = authorPerspective ? snapshot.AuthorSufferedCasualties : snapshot.AuthorInflictedCasualties;
		int inflicted = authorPerspective ? snapshot.AuthorInflictedCasualties : snapshot.AuthorSufferedCasualties;
		int otherWars = authorPerspective ? snapshot.AuthorOtherWars : snapshot.TargetOtherWars;
		int lostFiefs = lostFiefCount;
		float progress = Clamp01((enemyProgress - ownProgress) / 500f) * 40f;
		float strength = Clamp01((enemyStrength / Math.Max(1f, ownStrength) - 1f) / 2f) * 20f;
		float territory = Clamp01(lostFiefs / 2f) * 20f;
		float casualties = Clamp01((suffered - inflicted) / Math.Max(500f, ownStrength)) * 10f;
		float multiWar = Clamp01(otherWars / 2f) * 10f;
		return Math.Max(0f, Math.Min(100f, progress + strength + territory + casualties + multiWar));
	}

	public static void AddOrMergeResultSettlementSlot(
		WorldDiplomacyRound round,
		string kingdomId,
		string kind,
		string sourceDocumentId,
		string relatedKingdomId,
		bool prioritize,
		Func<WorldDiplomacyRound, string, bool> includeResultSettlementTarget,
		Func<string, string> createId)
	{
		if (round == null || string.IsNullOrWhiteSpace(kingdomId)
			|| !includeResultSettlementTarget(round, kingdomId)) return;
		round.ResultSettlementSlots ??= new List<WorldDiplomacyResultSettlementSlot>();
		WorldDiplomacyResultSettlementSlot slot = round.ResultSettlementSlots
			.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
		if (slot == null)
		{
			slot = new WorldDiplomacyResultSettlementSlot
			{
				SlotId = createId("diplomacy_result_slot"),
				KingdomId = kingdomId,
				Kind = WorldDiplomacyRoundLifecycleRules.DefaultSettlementSlotKind(kind),
				Status = "pending"
			};
			round.ResultSettlementSlots.Add(slot);
		}
		else if (!string.IsNullOrWhiteSpace(kind) && !(slot != null && WorldDiplomacyRoundLifecycleRules.SettlementSlotKindContains(slot.Kind, kind)))
		{
			slot.Kind = WorldDiplomacyRoundLifecycleRules.MergeSettlementSlotKind(slot.Kind, kind);
		}
		slot.SourceDocumentIds ??= new List<string>();
		slot.RelatedKingdomIds ??= new List<string>();
		if (!string.IsNullOrWhiteSpace(sourceDocumentId)
			&& !slot.SourceDocumentIds.Contains(sourceDocumentId, StringComparer.OrdinalIgnoreCase))
		{
			slot.SourceDocumentIds.Add(sourceDocumentId);
		}
		if (!string.IsNullOrWhiteSpace(relatedKingdomId)
			&& !WorldDiplomacyRoundLifecycleRules.IsSettlementSlotRelatedTo(slot, relatedKingdomId))
		{
			slot.RelatedKingdomIds.Add(relatedKingdomId);
		}
		if (prioritize)
		{
			round.ResultSettlementSlots.Remove(slot);
			round.ResultSettlementSlots.Insert(0, slot);
		}
	}

	public static void AddWarResponseResultSettlementSlot(WorldDiplomacyRound round, WorldDiplomacyDocument document,
		Func<WorldDiplomacyRound, string, bool> includeResultSettlementTarget, Func<string, string> createId)
	{
		if (round == null || document == null) return;
		round.ResultSettlementWarDocumentIds ??= new List<string>();
		if (document.Actions?.Count > 0)
		{
			foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null
				&& WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAction(
					x.ChangedDiplomaticState, WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), x.TargetKingdomId)))
			{
				string actionKey = WorldDiplomacyRoundLifecycleRules.ComposeWarResponseActionKey(
					document.DocumentId, action.ActionId);
				if (round.ResultSettlementWarDocumentIds.Contains(actionKey, StringComparer.OrdinalIgnoreCase)) continue;
				round.ResultSettlementWarDocumentIds.Add(actionKey);
				AddOrMergeResultSettlementSlot(round, action.TargetKingdomId, "war_response",
					document.DocumentId, document.AuthorKingdomId, prioritize: true,
					includeResultSettlementTarget, createId);
			}
			return;
		}
		if (!WorldDiplomacyRoundLifecycleRules.IsWarResponseSlotAction(
			document.ChangedDiplomaticState, WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent), document.TargetKingdomId)) return;
		if (round.ResultSettlementWarDocumentIds.Contains(document.DocumentId, StringComparer.OrdinalIgnoreCase)) return;
		round.ResultSettlementWarDocumentIds.Add(document.DocumentId);
		AddOrMergeResultSettlementSlot(round, document.TargetKingdomId, "war_response",
			document.DocumentId, document.AuthorKingdomId, prioritize: true,
				includeResultSettlementTarget, createId);
	}

	public static void ClearRoundScopedQueuesAndExpireOpportunities(WorldDiplomacyStorage storage, WorldDiplomacyRound round)
	{
		if (storage == null || round == null) return;
		storage.PendingParticipationEvaluations.RemoveAll(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId));
		storage.PendingSpeeches.RemoveAll(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId));
		storage.RelayArrivals.RemoveAll(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId));
		foreach (WorldDiplomacyPlayerOpportunity opportunity in storage.PlayerOpportunities.Where(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId)))
		{
			if (WorldDiplomacyRoundLifecycleRules.IsOpenLifecycleStatus(opportunity.Status)) opportunity.Status = "expired";
		}
	}

	public static bool TryGetConfirmedRoundResult(
		WorldDiplomacyDocument document,
		WorldDiplomacyRound round,
		List<WorldDiplomacyThreat> threats,
		out string closeReason,
		out string roundStatus)
	{
		closeReason = "";
		roundStatus = "resolved";
		if (document == null || round == null) return false;
		WorldDiplomacyConfirmedRoundResult result = WorldDiplomacyRoundLifecycleRules.EvaluateConfirmedRoundResult(
			document, round.PendingOffers, threats);
		closeReason = result.CloseReason;
		roundStatus = result.RoundStatus;
		return result.Confirmed;
	}

	public static void InvalidateUnserviceableResultSettlementObligations(WorldDiplomacyRound round,
		List<WorldDiplomacyThreat> threats, string kingdomId, string reason, int currentDay)
	{
		if (round == null || string.IsNullOrWhiteSpace(kingdomId)) return;
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(x, kingdomId)))
		{
			offer.Status = "invalidated";
		}
		foreach (WorldDiplomacyThreat threat in (threats ?? new List<WorldDiplomacyThreat>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsThreatRelevantToResultSettlement(x, round?.RoundId)))
		{
			if (WorldDiplomacyRoundLifecycleRules.IsThreatRequiredSpeaker(threat, kingdomId))
			{
				WorldDiplomacyRoundLifecycleRules.InvalidateThreatForNormalization(threat, reason, currentDay);
			}
		}
	}

	public static void SettleDiplomaticThreatObligationsForClosedRound(
		WorldDiplomacyRound round,
		List<WorldDiplomacyDocument> documents,
		List<WorldDiplomacyThreat> threats, int currentDay)
	{
		if (round == null || string.IsNullOrWhiteSpace(round.RoundId)) return;
		List<WorldDiplomacyDocument> published = documents ?? new List<WorldDiplomacyDocument>();
		bool initiatorHasPublished = published.Any(x => x != null
			&& string.Equals(x.AuthorKingdomId, round.InitiatorKingdomId, StringComparison.OrdinalIgnoreCase));
		bool retryableAbort = WorldDiplomacyRoundLifecycleRules.IsRetryableAbort(round.RoundStatus, initiatorHasPublished);
		if (!retryableAbort)
		{
			foreach (WorldDiplomacyThreat notice in WorldDiplomacyRoundLifecycleRules.SelectIssuerResolutionNotices(
				threats, round.InitiatorKingdomId, round.RoundId, int.MaxValue))
			{
				notice.IssuerResolutionNoticePending = false;
				notice.UpdatedDay = currentDay;
			}
		}
	}

	public static void RemoveSettledPolicySignalContextFromActiveRound(
		WorldDiplomacyRound round, string policyId, string ownerKingdomId)
	{
		if (round == null || string.IsNullOrWhiteSpace(policyId) || string.IsNullOrWhiteSpace(ownerKingdomId)) return;
		round.AttachedPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
		List<WorldDiplomacyPolicySignal> removed = round.AttachedPolicySignals
			.Where(signal => WorldDiplomacyRoundLifecycleRules.IsPolicySignalBoundTo(signal, policyId, ownerKingdomId))
			.ToList();
		if (removed.Count == 0) return;

		string openingContext = round.ExternalOpeningContext ?? "";
		foreach (WorldDiplomacyPolicySignal signal in removed)
		{
			string context = WorldDiplomacyPromptContractRules.BuildPolicySignalContext(signal);
			if (!string.IsNullOrWhiteSpace(context)) openingContext = openingContext.Replace(context, "");
		}
		round.ExternalOpeningContext = openingContext.Trim();
		round.AttachedPolicySignals.RemoveAll(signal =>
			WorldDiplomacyRoundLifecycleRules.IsPolicySignalBoundTo(signal, policyId, ownerKingdomId));
		round.ExternalSignalKeys ??= new List<string>();
		HashSet<string> removedSignalKeys = new HashSet<string>(
			removed.Select(signal => signal.SignalKey).Where(key => !string.IsNullOrWhiteSpace(key)),
			StringComparer.OrdinalIgnoreCase);
		if (removedSignalKeys.Count > 0)
		{
			round.ExternalSignalKeys.RemoveAll(key => removedSignalKeys.Contains(key));
		}
	}

	public static void FinalizeDiplomaticThreatHistoryAfterDocument(WorldDiplomacyDocument document,
		List<WorldDiplomacyThreat> threats,
		Action<WorldDiplomacyThreat> appendBreachResult,
		Action<WorldDiplomacyThreat> appendDomesticPenaltyResult,
		Action<WorldDiplomacyThreat> appendIssuerRewardResult)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.DocumentId)) return;
		foreach (WorldDiplomacyThreat threat in (threats ?? new List<WorldDiplomacyThreat>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsThreatLinkedToDocument(x, document.DocumentId)))
		{
			WorldDiplomacyThreatHistoryFinalization finalization =
				WorldDiplomacyRoundLifecycleRules.EvaluateThreatHistoryFinalization(
					threat, document.HistoryDeclarationRecorded, document.HistoryResultRecorded);
			if (finalization == WorldDiplomacyThreatHistoryFinalization.AppendBreachResult)
			{
				appendBreachResult?.Invoke(threat);
			}
			else if (finalization == WorldDiplomacyThreatHistoryFinalization.MarkResultRecorded)
			{
				threat.HistoryResultRecorded = true;
			}
			if (threat.DomesticPenaltyCompleted)
			{
				appendDomesticPenaltyResult?.Invoke(threat);
			}
			if (threat.IssuerRewardCompleted)
			{
				appendIssuerRewardResult?.Invoke(threat);
			}
		}
	}

	public static void FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(WorldDiplomacyDocument document,
		List<WorldDiplomacyThreat> threats,
		Action<WorldDiplomacyThreat, WorldDiplomacyThreatNonComplianceEvent> appendNonComplianceEvent)
	{
		if (document?.HistoryDeclarationRecorded != true || string.IsNullOrWhiteSpace(document.DocumentId)) return;
		foreach (WorldDiplomacyThreat threat in (threats ?? new List<WorldDiplomacyThreat>())
			.Where(x => WorldDiplomacyRoundLifecycleRules.IsThreatNonComplianceLinkedToDocument(x, document.DocumentId)))
		{
			TryAppendDiplomaticThreatNonComplianceHistoryResult(threat, appendNonComplianceEvent);
		}
	}

	public static void TryAppendDiplomaticThreatNonComplianceHistoryResult(
		WorldDiplomacyThreat threat,
		Action<WorldDiplomacyThreat, WorldDiplomacyThreatNonComplianceEvent> appendNonComplianceEvent)
	{
		if (threat == null) return;
		WorldDiplomacyRoundLifecycleRules.CaptureThreatNonComplianceEvent(threat);
		foreach (WorldDiplomacyThreatNonComplianceEvent decision in
			WorldDiplomacyRoundLifecycleRules.SelectUnrecordedNonComplianceEvents(threat))
		{
			appendNonComplianceEvent?.Invoke(threat, decision);
		}
		WorldDiplomacyThreatNonComplianceEvent current =
			WorldDiplomacyRoundLifecycleRules.SelectCurrentNonComplianceEvent(threat);
		threat.NonComplianceHistoryRecorded = current?.HistoryRecorded == true;
	}

	public static void ResolveDiplomaticThreatsAfterWarStarted(
		List<WorldDiplomacyThreat> threats,
		string firstKingdomId, string secondKingdomId, int currentDay, bool structuredDeclarationPending)
	{
		if (string.IsNullOrWhiteSpace(firstKingdomId) || string.IsNullOrWhiteSpace(secondKingdomId)
			|| firstKingdomId == secondKingdomId || threats == null) return;
		int day = currentDay;
		foreach (WorldDiplomacyThreat threat in threats.Where(x => WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus(x?.Status)
			&& WorldDiplomacyRoundLifecycleRules.IsThreatBetweenParties(x, firstKingdomId, secondKingdomId)))
		{
			threat.UpdatedDay = day;
			if (structuredDeclarationPending)
			{
				// ExecuteImmediateIntent still knows the structured author. It will mark the
				// matching threat enforced after the game action returns.
				threat.ResolutionReason = "war_started_during_structured_declaration_pending";
				continue;
			}
			threat.Status = "invalidated";
			threat.ResolutionReason = "war_started_outside_structured_threat_follow_through";
			threat.ObligationRoundId = "";
			threat.ObligationClaimedDay = 0;
			threat.HistoryResultRecorded = true;
		}
	}

	public static void SettleDiplomaticThreatFollowThroughAfterDeclaration(
		WorldDiplomacyDocument document,
		List<WorldDiplomacyThreat> threats, string authorKingdomId,
		Action<WorldDiplomacyThreat, WorldDiplomacyDocument> applyReputationPenalty)
	{
		if (document == null || string.IsNullOrWhiteSpace(authorKingdomId)) return;
		WorldDiplomacyThreat threat = WorldDiplomacyRoundLifecycleRules.SelectOpenThreatIssuedBy(threats, authorKingdomId);
		if (!WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughObligationPending(
			threat, document.PresentedThreatFollowThroughDocumentIds)) return;
		WorldDiplomacyDocumentAction matchingAction = WorldDiplomacyRoundLifecycleRules.SelectThreatDecisionAction(
			document.Actions, threat.TargetKingdomId);
		bool targetsThreatTarget = WorldDiplomacyRoundLifecycleRules.ThreatDeclarationTargetsThreatTarget(
			matchingAction != null, threat.TargetKingdomId, document.TargetKingdomId);
		WorldDiplomacyThreatStateRuleResult result = WorldDiplomacyThreatStateRules.EvaluateIssuerFollowThrough(
			threat.TargetDecision,
			threat.Stage,
			threat.StageDocumentId,
			currentStageWasPresented: true,
			WorldDiplomacyIntentVocabulary.NormalizeIntent(matchingAction?.Intent ?? document.Intent),
			targetsThreatTarget,
			matchingAction?.ChangedDiplomaticState ?? document.ChangedDiplomaticState);
		if (result == WorldDiplomacyThreatStateRuleResult.MarkFollowThroughBreached)
		{
			applyReputationPenalty?.Invoke(threat, document);
		}
	}

	public static bool CompleteUnresolvableDiplomaticThreatDomesticPenalty(
		WorldDiplomacyThreat threat,
		out int affectedClanCount, int currentDay, Action<string> log)
	{
		affectedClanCount = 0;
		if (threat == null) return false;
		threat.DomesticPenaltyEligibleClanIds ??= new List<string>();
		threat.DomesticPenaltyAppliedClanIds ??= new List<string>();
		threat.DomesticPenaltySkippedClanIds ??= new List<string>();
		HashSet<string> applied = new HashSet<string>(threat.DomesticPenaltyAppliedClanIds, StringComparer.OrdinalIgnoreCase);
		HashSet<string> skipped = new HashSet<string>(threat.DomesticPenaltySkippedClanIds, StringComparer.OrdinalIgnoreCase);
		foreach (string clanId in WorldDiplomacyRoundLifecycleRules.SelectUnresolvedClanIds(
			threat.DomesticPenaltyEligibleClanIds.Where(x => !string.IsNullOrWhiteSpace(x)), applied))
		{
			skipped.Add(clanId);
		}
		WorldDiplomacyRoundLifecycleRules.FinalizeUnresolvedThreatDomesticPenalty(threat, skipped, currentDay);
		affectedClanCount = applied.Count;
		log?.Invoke("ultimatum compliance domestic penalty finalized without kingdom object threat=" + threat.ThreatId
			+ " applied=" + applied.Count.ToString(CultureInfo.InvariantCulture)
			+ " skipped=" + skipped.Count.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	public static bool CompleteUnresolvableDiplomaticThreatIssuerRelationReward(
		WorldDiplomacyThreat threat,
		out int affectedClanCount,
		int issuerRewardAmount, int currentDay, Action<string> log)
	{
		affectedClanCount = 0;
		if (threat == null) return false;
		threat.IssuerRewardEligibleClanIds ??= new List<string>();
		threat.IssuerRewardAppliedClanIds ??= new List<string>();
		threat.IssuerRewardSkippedClanIds ??= new List<string>();
		if (!threat.IssuerRewardSnapshotCaptured)
		{
			threat.IssuerRewardAmount = issuerRewardAmount;
		}
		HashSet<string> applied = new HashSet<string>(threat.IssuerRewardAppliedClanIds, StringComparer.OrdinalIgnoreCase);
		HashSet<string> skipped = new HashSet<string>(threat.IssuerRewardSkippedClanIds, StringComparer.OrdinalIgnoreCase);
		foreach (string clanId in WorldDiplomacyRoundLifecycleRules.SelectUnresolvedClanIds(
			threat.IssuerRewardEligibleClanIds.Where(x => !string.IsNullOrWhiteSpace(x)), applied))
		{
			skipped.Add(clanId);
		}
		WorldDiplomacyRoundLifecycleRules.FinalizeUnresolvedThreatIssuerReward(threat, skipped, currentDay);
		affectedClanCount = applied.Count;
		if (WorldDiplomacyRoundLifecycleRules.IsIssuerRewardHistoryEmpty(threat.IssuerRewardAmount, applied.Count))
		{
			threat.IssuerRewardHistoryRecorded = true;
		}
		log?.Invoke("diplomatic threat issuer relation reward finalized without kingdom object threat=" + threat.ThreatId
			+ " applied=" + applied.Count.ToString(CultureInfo.InvariantCulture)
			+ " skipped=" + skipped.Count.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	public static void InvalidateOtherThreatsBoundToSettledPolicy(
		WorldDiplomacyThreat settledThreat,
		List<WorldDiplomacyThreat> threats, int currentDay, Action<string> log)
	{
		if (settledThreat == null || string.IsNullOrWhiteSpace(settledThreat.PolicyConditionPolicyId)) return;
		int day = currentDay;
		foreach (WorldDiplomacyThreat other in (threats ?? new List<WorldDiplomacyThreat>())
			.Where(x => !ReferenceEquals(x, settledThreat)
				&& WorldDiplomacyRoundLifecycleRules.IsThreatBoundToPolicy(
					x, settledThreat.PolicyConditionPolicyId, settledThreat.PolicyConditionOwnerKingdomId)))
		{
			WorldDiplomacyRoundLifecycleRules.InvalidateThreatForPolicyCancellation(other, day);
			log?.Invoke("diplomatic threat invalidated after shared policy cancellation threat=" + other.ThreatId
				+ " policy=" + other.PolicyConditionPolicyId
				+ " settled_by=" + settledThreat.ThreatId);
		}
	}

	public static int FindPriorityThreatRelayIndex(WorldDiplomacyRound round, List<string> route, List<WorldDiplomacyThreat> threats, Func<string, bool> hasIndependentAuthority)
	{
		if (round == null || route == null || route.Count < 2) return -1;
		string currentKingdomId = round.RelayCursor >= 0 && round.RelayCursor < route.Count
			? route[round.RelayCursor]
			: "";
		foreach (WorldDiplomacyThreat threat in OrderThreatsByUpdatedDay(
			(threats ?? new List<WorldDiplomacyThreat>()).Where(x => x != null && IsOpenDiplomaticThreatStatus(x.Status))))
		{
			string requiredSpeakerId = ResolveThreatRequiredSpeakerId(threat);
			if (string.IsNullOrWhiteSpace(requiredSpeakerId)
				|| string.Equals(requiredSpeakerId, currentKingdomId, StringComparison.OrdinalIgnoreCase)) continue;
			int index = route.FindIndex(x => string.Equals(x, requiredSpeakerId, StringComparison.OrdinalIgnoreCase));
			if (index >= 0 && hasIndependentAuthority(requiredSpeakerId)) return index;
		}
		return -1;
	}

	public static void CompleteRelayPassProgressAccounting(WorldDiplomacyRound round, Action<string> log)
	{
		WorldDiplomacyRelayPassAccountingDecision accounting = EvaluateRelayPassAccounting(
			new WorldDiplomacyRelayPassAccountingInput
			{
				RelayPassNumber = round?.RelayPassNumber ?? 0,
				LastAccountedRelayPassNumber = round?.LastAccountedRelayPassNumber ?? 0,
				DiplomaticActionAttemptCount = round?.DiplomaticActionAttemptCount ?? 0,
				ActionAttemptCountAtPassStart = round?.ActionAttemptCountAtPassStart ?? 0,
				ConsecutiveNoActionPasses = round?.ConsecutiveNoActionPasses ?? 0
			});
		if (round == null || !accounting.ShouldAccount) return;
		round.ConsecutiveNoActionPasses = accounting.NewConsecutiveNoActionPasses;
		round.ActionAttemptCountAtPassStart = round.DiplomaticActionAttemptCount;
		round.LastAccountedRelayPassNumber = round.RelayPassNumber;
		log?.Invoke("relay pass progress accounted round=" + round.RoundId
			+ " pass=" + round.RelayPassNumber.ToString(CultureInfo.InvariantCulture)
			+ " action=" + accounting.ActionOccurred
			+ " consecutive_no_action=" + round.ConsecutiveNoActionPasses.ToString(CultureInfo.InvariantCulture));
	}

	public static void TripAutomaticRoundCircuitBreaker(WorldDiplomacyStorage storage, WorldDiplomacyRound round, string reason, Action<string> log)
	{
		if (round == null || round.AutomaticCircuitBreakerTripped)
		{
			return;
		}
		round.AutomaticCircuitBreakerTripped = true;
		foreach (WorldDiplomacyRoundParticipant participant in round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
		{
			if (participant == null) continue;
			participant.MandatoryReplyPending = false;
			participant.MandatorySinceDay = 0;
		}
		ClearRoundScopedQueuesAndExpireOpportunities(storage, round);
		log?.Invoke("round circuit breaker tripped round=" + round.RoundId
			+ " documents=" + round.AutomaticDocumentsStarted.ToString(CultureInfo.InvariantCulture)
			+ " reason=" + (reason ?? ""));
	}

	public static void RetryDeferredRoundProgress(WorldDiplomacyStorage storage, Action<WorldDiplomacyDocument> processDocument, Action<string> log)
	{
		WorldDiplomacyRound round = storage?.ActiveRound;
		if (round == null || !IsActiveRoundState(round.State)) return;
		foreach (WorldDiplomacyDocument document in OrderDocumentsChronologically((storage.Documents ?? new List<WorldDiplomacyDocument>())
			.Where(x => x != null && x.IsReadyForPublication && !x.RoundProgressHandled
				&& IsRecordInRound(x.RoundId, round.RoundId))).Take(8))
		{
			try
			{
				processDocument(document);
			}
			catch (Exception ex)
			{
				log?.Invoke("deferred round progress retry failed document=" + document.DocumentId + " error=" + ex.Message);
			}
		}
	}

	public static void RecordPlayerOpportunity(WorldDiplomacyRound round, string playerKingdomId, List<WorldDiplomacyPlayerOpportunity> opportunities, List<WorldDiplomacyDocument> documents, int currentDay)
	{
		if (round == null || string.IsNullOrWhiteSpace(playerKingdomId) || opportunities == null || documents == null) return;
		WorldDiplomacyPlayerOpportunity opportunity = opportunities.FirstOrDefault(x => x != null
			&& IsRecordInRound(x.RoundId, round.RoundId));
		if (opportunity == null)
		{
			opportunity = new WorldDiplomacyPlayerOpportunity { RoundId = round.RoundId, ArrivedDay = currentDay, Status = "open" };
			opportunities.Add(opportunity);
		}
		opportunity.ArrivedDay = currentDay;
		opportunity.Status = "open";
		opportunity.KnownDocumentIds = NormalizeIdListPreserveOrder(documents.Where(x => x != null && x.IsReadyForPublication && IsRecordInRound(x.RoundId, round.RoundId))
			.Select(x => x.DocumentId));
	}

	public static void ResetDailyGenerationBudget(ref int aiDocumentsStartedDay, ref int aiDocumentsStartedToday, int currentDay)
	{
		if (aiDocumentsStartedDay != currentDay)
		{
			aiDocumentsStartedDay = currentDay;
			aiDocumentsStartedToday = 0;
		}
	}

	public static bool TryConsumeAiDocumentBudget(ref int aiDocumentsStartedDay, ref int aiDocumentsStartedToday, int currentDay, int maxPerDay)
	{
		ResetDailyGenerationBudget(ref aiDocumentsStartedDay, ref aiDocumentsStartedToday, currentDay);
		if (aiDocumentsStartedToday >= maxPerDay)
		{
			return false;
		}
		aiDocumentsStartedToday++;
		return true;
	}

	public static void ExpireUnansweredSettlementOffersForNoActionDeclaration(
		WorldDiplomacyRound round,
		WorldDiplomacyDocument document,
		Action<string> log)
	{
		if (!IsNoActionExpiryEligible(
			round?.ResultSettlementPending == true,
			document != null && (document.IsRoundResponseNoActionDeclaration
				|| document.IsWarResponseNoActionDeclaration),
			!string.IsNullOrWhiteSpace(document?.ResultSettlementSlotId),
			!string.IsNullOrWhiteSpace(document?.AuthorKingdomId))) return;
		WorldDiplomacyResultSettlementSlot slot = SelectSettlementSlotBySlotAndKingdom(
			round.ResultSettlementSlots, document.ResultSettlementSlotId, document.AuthorKingdomId);
		if (slot == null || slot.SourceDocumentIds == null || slot.SourceDocumentIds.Count == 0) return;
		HashSet<string> sourceIds = new HashSet<string>(
			slot.SourceDocumentIds.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		if (sourceIds.Count == 0) return;
		int expired = 0;
		foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
			.Where(x => x != null
				&& ShouldExpireSettlementOffer(
					x.Status,
					string.Equals(x.TargetKingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase),
					sourceIds.Contains(x.SourceDocumentId ?? ""))))
		{
			offer.Status = "expired";
			expired++;
		}
		if (expired > 0)
		{
			log?.Invoke("round response statement left settlement offers unaccepted round=" + round.RoundId
				+ " slot=" + slot.SlotId
				+ " author=" + document.AuthorKingdomId
				+ " expired=" + expired.ToString(CultureInfo.InvariantCulture));
		}
	}

	public static void ConsumeResultSettlementSpeaker(WorldDiplomacyRound round, WorldDiplomacyDocument document, Action<string> log)
	{
		if (round == null || document == null || !round.ResultSettlementPending) return;
		round.ResultSettlementSlots ??= new List<WorldDiplomacyResultSettlementSlot>();
		WorldDiplomacyResultSettlementSlot slot = SelectSettlementSlot(
			round.ResultSettlementSlots, document.ResultSettlementSlotId, document.AuthorKingdomId);
		if (slot == null) return;
		round.ResultSettlementSlots.Remove(slot);
		if (string.Equals(round.ResultSettlementCurrentSlotId, slot.SlotId, StringComparison.OrdinalIgnoreCase))
		{
			round.ResultSettlementCurrentSlotId = "";
			round.ResultSettlementPlayerWaitingSinceDay = 0;
		}
		round.RelayWaiting = false;
		log?.Invoke("round result settlement turn consumed round=" + round.RoundId
			+ " slot=" + slot.SlotId + " author=" + document.AuthorKingdomId);
	}

	public static int FindNextRelayIndex(WorldDiplomacyRound round, int start, Func<string, bool> hasIndependentAuthority)
	{
		List<string> route = round?.RelayRouteKingdomIds ?? new List<string>();
		for (int index = start; index >= 0 && index < route.Count; index += round.RelayDirection)
		{
			if (!hasIndependentAuthority(route[index]))
			{
				continue;
			}
			WorldDiplomacyRoundParticipant participant = (round.Participants ?? new List<WorldDiplomacyRoundParticipant>())
				.FirstOrDefault(x => x != null && string.Equals(x.KingdomId, route[index], StringComparison.OrdinalIgnoreCase));
			if (participant == null || !string.Equals(participant.State, "withdrawn", StringComparison.OrdinalIgnoreCase)) return index;
		}
		return -1;
	}

	public static void InitializeResultSettlementRouteSlots(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents, Func<WorldDiplomacyRound, string, bool> includeResultSettlementTarget, Func<string, string> createId)
	{
		if (round == null || round.ResultSettlementRouteInitialized || !round.RelayPlanned) return;
		HashSet<string> spoken = CollectSpokenAuthorIds(
			documents, round.RoundId);
		foreach (string kingdomId in round.RelayRouteKingdomIds ?? new List<string>())
		{
			if (!spoken.Contains(kingdomId))
			{
				AddOrMergeResultSettlementSlot(round, kingdomId, "route", round.ResultSettlementTriggerDocumentId, "", prioritize: false, includeResultSettlementTarget, createId);
			}
		}
		round.ResultSettlementRouteInitialized = true;
	}

	public static void SkipResultSettlementSlot(WorldDiplomacyRound round, string slotId, string kingdomId, string reason, List<WorldDiplomacyThreat> threats, int currentDay, Action<string> log)
	{
		if (round == null || !round.ResultSettlementPending) return;
		WorldDiplomacyResultSettlementSlot slot = SelectSettlementSlot(
			round.ResultSettlementSlots, slotId, kingdomId);
		if (slot == null) return;
		InvalidateUnserviceableResultSettlementObligations(round, threats, slot.KingdomId,
			"result_settlement_" + (reason ?? "technical_skip"), currentDay);
		round.ResultSettlementSlots.Remove(slot);
		if (string.Equals(round.ResultSettlementCurrentSlotId, slot.SlotId, StringComparison.OrdinalIgnoreCase))
		{
			round.ResultSettlementCurrentSlotId = "";
			round.ResultSettlementPlayerWaitingSinceDay = 0;
		}
		round.RelayWaiting = false;
		log?.Invoke("round result settlement slot skipped round=" + round.RoundId
			+ " slot=" + slot.SlotId + " kingdom=" + slot.KingdomId + " reason=" + (reason ?? ""));
	}


    public static WorldDiplomacyExchange ResolveExchange(
        WorldDiplomacyExchange activeExchange,
        List<WorldDiplomacyExchange> suspendedExchanges,
        string exchangeId)
    {
        if (string.IsNullOrWhiteSpace(exchangeId))
        {
            return null;
        }
        if (activeExchange != null && string.Equals(activeExchange.ExchangeId, exchangeId, StringComparison.OrdinalIgnoreCase))
        {
            return activeExchange;
        }
        return suspendedExchanges?.FirstOrDefault(x => x != null
            && string.Equals(x.ExchangeId, exchangeId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool CompleteExchange(
        WorldDiplomacyExchange exchange,
        WorldDiplomacyExchange activeExchange,
        List<WorldDiplomacyExchange> suspendedExchanges,
        string reason,
        int currentDay)
    {
        if (exchange == null)
        {
            return false;
        }
        exchange.State = "completed";
        exchange.CompletedDay = currentDay;
        exchange.CloseReason = reason ?? "";
        if (ReferenceEquals(activeExchange, exchange))
        {
            return true;
        }
        suspendedExchanges?.Remove(exchange);
        return false;
    }

    public static bool SuspendActiveExchangeForPlayerInsertion(
        WorldDiplomacyExchange activeExchange,
        List<WorldDiplomacyExchange> suspendedExchanges,
        int currentDay)
    {
        if (activeExchange == null || suspendedExchanges == null)
        {
            return false;
        }
        activeExchange.SuspendedDay = currentDay;
        activeExchange.StateBeforeSuspension = activeExchange.State;
        activeExchange.State = "suspended_by_player";
        suspendedExchanges.Insert(0, activeExchange);
        return true;
    }

    public static WorldDiplomacyExchange RestoreSuspendedExchangeIfAny(
        WorldDiplomacyExchange activeExchange,
        List<WorldDiplomacyExchange> suspendedExchanges,
        int currentDay)
    {
        if (activeExchange != null || suspendedExchanges == null || suspendedExchanges.Count == 0)
        {
            return null;
        }
        WorldDiplomacyExchange exchange = suspendedExchanges[0];
        suspendedExchanges.RemoveAt(0);
        int pausedDays = ComputeSuspendedPauseDays(currentDay, exchange.SuspendedDay);
        exchange.ResponseDueDay += pausedDays;
        exchange.CloseDueDay += pausedDays;
        exchange.State = NormalizeRestoredExchangeState(exchange.StateBeforeSuspension);
        exchange.StateBeforeSuspension = "";
        return exchange;
    }

        public static void RebuildOfferCooldownIndex(
            List<WorldDiplomacyOfferCooldown> cooldowns,
            Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> cooldownByKey)
        {
            if (cooldownByKey == null)
            {
                return;
            }
            cooldownByKey.Clear();
            foreach (WorldDiplomacyOfferCooldown cooldown in cooldowns ?? new List<WorldDiplomacyOfferCooldown>())
            {
                if (!Persistence.WorldDiplomacyOfferCooldownStorageNormalizer.TryCreateKey(
                    cooldown,
                    out WorldDiplomacyOfferCooldownKey key)) continue;
                if (!cooldownByKey.TryGetValue(key, out WorldDiplomacyOfferCooldown existing)
                    || existing.LastFailedRoundDay <= cooldown.LastFailedRoundDay)
                {
                    cooldownByKey[key] = cooldown;
                }
            }
        }

        public static void UpsertOfferCooldown(
            List<WorldDiplomacyOfferCooldown> cooldowns,
            Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> cooldownByKey,
            WorldDiplomacyOfferCooldownKey key,
            int failedRoundDay,
            string sourceRoundId,
            Action normalizeStorage)
        {
            if (!key.IsValid || failedRoundDay < 0 || cooldowns == null || cooldownByKey == null) return;
            if (!cooldownByKey.TryGetValue(key, out WorldDiplomacyOfferCooldown cooldown))
            {
                cooldown = new WorldDiplomacyOfferCooldown();
                cooldowns.Add(cooldown);
            }
            cooldown.ProposerKingdomId = key.ProposerKingdomId;
            cooldown.TargetKingdomId = key.TargetKingdomId;
            cooldown.Domain = Persistence.WorldDiplomacyOfferCooldownStorageNormalizer.DomainToken(key.Domain);
            cooldown.LastFailedRoundDay = failedRoundDay;
            cooldown.SourceRoundId = sourceRoundId ?? "";
            cooldownByKey[key] = cooldown;
            if (cooldowns.Count > Persistence.WorldDiplomacyOfferCooldownStorageNormalizer.MaxStoredCooldowns)
            {
                normalizeStorage?.Invoke();
            }
        }

        public static void RemoveOfferCooldown(
            List<WorldDiplomacyOfferCooldown> cooldowns,
            Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> cooldownByKey,
            WorldDiplomacyOfferCooldownKey key)
        {
            if (!key.IsValid || cooldownByKey == null) return;
            if (cooldownByKey.TryGetValue(key, out WorldDiplomacyOfferCooldown existing))
            {
                cooldowns?.Remove(existing);
            }
            cooldownByKey.Remove(key);
        }

        public static void ClearBilateralOfferCooldowns(
            List<WorldDiplomacyOfferCooldown> cooldowns,
            Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> cooldownByKey,
            string firstKingdomId,
            string secondKingdomId,
            WorldDiplomacyOfferDomain domain)
        {
            if (string.IsNullOrWhiteSpace(firstKingdomId) || string.IsNullOrWhiteSpace(secondKingdomId)
                || string.Equals(firstKingdomId, secondKingdomId, StringComparison.OrdinalIgnoreCase)
                || domain == WorldDiplomacyOfferDomain.None)
            {
                return;
            }
            RemoveOfferCooldown(cooldowns, cooldownByKey, new WorldDiplomacyOfferCooldownKey(firstKingdomId, secondKingdomId, domain));
            RemoveOfferCooldown(cooldowns, cooldownByKey, new WorldDiplomacyOfferCooldownKey(secondKingdomId, firstKingdomId, domain));
        }


    internal const int MaxStoredRecentBattles = 96;
    internal const int MaxStoredNativeSignals = 180;

    public static WorldDiplomacyDocument ResolveDocument(
        List<WorldDiplomacyDocument> documents,
        string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return null;
        }
        return documents?.FirstOrDefault(x => x != null && MatchesDocumentId(x.DocumentId, documentId));
    }

    public static WorldDiplomacyRound ResolveRound(
        WorldDiplomacyRound activeRound,
        List<WorldDiplomacyRound> completedRounds,
        string roundId)
    {
        if (string.IsNullOrWhiteSpace(roundId)) return null;
        if (activeRound != null && IsRecordInRound(activeRound.RoundId, roundId)) return activeRound;
        return completedRounds?.FirstOrDefault(x => x != null && IsRecordInRound(x.RoundId, roundId));
    }

    public static List<WorldDiplomacyDocument> SelectRecentDocumentsForTimelineQuery(
        List<WorldDiplomacyDocument> documents,
        int maxCount,
        int maxStored)
    {
        return OrderDocumentsByRecency((documents ?? new List<WorldDiplomacyDocument>())
                .Where(x => x != null))
            .Take(Math.Max(1, Math.Min(maxStored, maxCount)))
            .Select(WorldDiplomacyDocumentFactRules.CloneDocument)
            .ToList();
    }

    public static HashSet<string> CollectKnownDocumentIds(
        List<WorldDiplomacySettlementKnowledge> settlementKnowledge,
        List<WorldDiplomacyKingdomKnowledge> nobleKnowledge,
        List<WorldDiplomacyKingdomKnowledge> kingdomKnowledge,
        string settlementId,
        string kingdomId,
        bool includeNobleKnowledge,
        bool includeCourtKnowledge)
    {
        HashSet<string> knownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        WorldDiplomacySettlementKnowledge localKnowledge = settlementKnowledge?.FirstOrDefault(x => x != null
            && string.Equals(x.SettlementId, settlementId, StringComparison.OrdinalIgnoreCase));
        foreach (string id in localKnowledge?.DocumentIds ?? new List<string>()) knownIds.Add(id);
        if (includeNobleKnowledge)
        {
            WorldDiplomacyKingdomKnowledge noble = nobleKnowledge?.FirstOrDefault(x => x != null
                && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
            foreach (string id in noble?.DocumentIds ?? new List<string>()) knownIds.Add(id);
        }
        if (includeCourtKnowledge)
        {
            WorldDiplomacyKingdomKnowledge court = kingdomKnowledge?.FirstOrDefault(x => x != null
                && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase));
            foreach (string id in court?.DocumentIds ?? new List<string>()) knownIds.Add(id);
        }
        return knownIds;
    }

    public static List<WorldDiplomacyBattleFact> TrimRecentBattleFacts(
        List<WorldDiplomacyBattleFact> battles,
        int currentDay,
        int retentionDays)
    {
        int cutoff = currentDay - retentionDays;
        return (battles ?? new List<WorldDiplomacyBattleFact>())
            .Where(x => x != null && x.Day >= cutoff && !string.IsNullOrWhiteSpace(x.BattleId))
            .OrderByDescending(x => x.Day)
            .Take(MaxStoredRecentBattles)
            .ToList();
    }

    public static List<NativeDiplomacySignal> TrimNativeSignals(
        List<NativeDiplomacySignal> signals,
        int cutoff)
    {
        return (signals ?? new List<NativeDiplomacySignal>())
            .Where(x => x != null && x.Day >= cutoff)
            .OrderByDescending(x => x.Day)
            .Take(MaxStoredNativeSignals)
            .ToList();
    }

    public static void RecalculateCanonicalHistoryTokens(
        WorldDiplomacyStorage storage,
        long compressionTriggerTokens)
{

    WorldDiplomacyCanonicalHistoryState history = storage?.CanonicalHistory;
    if (history == null) return;
    long total = Math.Max(0L, history.Snapshot?.EstimatedTokens ?? 0L);
    foreach (WorldDiplomacyCanonicalHistoryEntry entry in history.DeltaEntries ?? new List<WorldDiplomacyCanonicalHistoryEntry>())
    {
        total += Math.Max(0L, entry?.EstimatedTokens ?? 0L);
    }
    history.EstimatedTokens = Math.Max(0L, total);
    storage.DiplomacyTokensSinceCompression = history.EstimatedTokens;
    storage.DiplomacyCompressionPending = history.EstimatedTokens >= compressionTriggerTokens;
}

    public static List<WorldDiplomacyCanonicalProtectedFact> BuildCanonicalProtectedFactsThrough(
        WorldDiplomacyCanonicalHistoryState history,
        long cutoff)
{
    Dictionary<string, WorldDiplomacyCanonicalProtectedFact> facts = new Dictionary<string, WorldDiplomacyCanonicalProtectedFact>(StringComparer.OrdinalIgnoreCase);
    void Add(WorldDiplomacyCanonicalProtectedFact candidate)
    {
        WorldDiplomacyCanonicalProtectedFact clean = WorldDiplomacyCanonicalRenderRules.CloneProtectedFact(candidate);
        if (clean == null
            || (clean.Kind != "diplomatic_result" && clean.Kind != "response_link")
            || string.IsNullOrWhiteSpace(clean.SourceId)
            || (clean.Kind == "diplomatic_result" && string.IsNullOrWhiteSpace(clean.Text))
            || (clean.Kind == "response_link" && string.IsNullOrWhiteSpace(clean.RelatedSourceId))) return;
        string key = WorldDiplomacyCanonicalRenderRules.ProtectedFactStableKey(clean);
        if (!string.IsNullOrWhiteSpace(key) && !facts.ContainsKey(key)) facts.Add(key, clean);
    }
    foreach (WorldDiplomacyCanonicalProtectedFact fact in history.Snapshot.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>()) Add(fact);
    foreach (WorldDiplomacyCanonicalHistoryEntry entry in WorldDiplomacyRoundLifecycleRules
            .SelectDeltaEntriesThrough(history.DeltaEntries, cutoff))
    {
        if (entry.Verified && string.Equals(entry.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
        {
            Add(new WorldDiplomacyCanonicalProtectedFact
            {
                Kind = "diplomatic_result",
                SourceKey = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.SourceKey, "result:" + entry.SourceId),
                SourceId = entry.SourceId,
                RelatedSourceId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(entry.RespondingToThreatDocumentId, entry.RespondingToOfferDocumentId),
                Sequence = entry.Sequence,
                Day = entry.Day,
                GameDate = entry.GameDate,
                AuthorKingdomId = entry.AuthorKingdomId,
                TargetKingdomIds = entry.TargetKingdomIds,
                Intent = entry.Intent,
                Commitment = entry.Commitment,
                Text = entry.Text
            });
        }
        if (!string.IsNullOrWhiteSpace(entry.SourceId) && !string.IsNullOrWhiteSpace(entry.RespondingToOfferDocumentId))
        {
            Add(new WorldDiplomacyCanonicalProtectedFact
            {
                Kind = "response_link",
                SourceKey = "response:" + entry.SourceId + "->" + entry.RespondingToOfferDocumentId,
                SourceId = entry.SourceId,
                RelatedSourceId = entry.RespondingToOfferDocumentId,
                Sequence = entry.Sequence,
                Day = entry.Day,
                GameDate = entry.GameDate,
                AuthorKingdomId = entry.AuthorKingdomId,
                TargetKingdomIds = entry.TargetKingdomIds,
                Intent = entry.Intent,
                Commitment = entry.Commitment
            });
        }
        if (!string.IsNullOrWhiteSpace(entry.SourceId) && !string.IsNullOrWhiteSpace(entry.RespondingToThreatDocumentId))
        {
            Add(new WorldDiplomacyCanonicalProtectedFact
            {
                Kind = "response_link",
                SourceKey = "threat-response:" + entry.SourceId + "->" + entry.RespondingToThreatDocumentId,
                SourceId = entry.SourceId,
                RelatedSourceId = entry.RespondingToThreatDocumentId,
                Sequence = entry.Sequence,
                Day = entry.Day,
                GameDate = entry.GameDate,
                AuthorKingdomId = entry.AuthorKingdomId,
                TargetKingdomIds = entry.TargetKingdomIds,
                Intent = entry.Intent,
                Commitment = entry.Commitment
            });
        }
    }
    return WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequence(facts.Values).ToList();
}
    public static void EnqueueJob(
        WorldDiplomacyStorage storage,
        WorldDiplomacyJob job,
        int maxPendingJobs)
{
    if (job == null || string.IsNullOrWhiteSpace(job.JobId))
    {
        return;
    }
    if (storage.Jobs.Any(x => HasJobId(x, job.JobId)))
    {
        return;
    }
    storage.Jobs.Add(job);
    int queueCapacity = maxPendingJobs + (storage.Jobs.Any(x =>
        IsJobOfKind(x, "compress")) ? 1 : 0);
    storage.Jobs = storage.Jobs
        .Where(x => x != null)
        .OrderByDescending(x => x.Priority)
        .ThenBy(x => x.CreatedDay)
        .ThenBy(x => x.JobId, StringComparer.OrdinalIgnoreCase)
        .Take(queueCapacity)
        .ToList();
}

    public static void CommitLocalRoundSummary(
        WorldDiplomacyStorage storage,
        WorldDiplomacyRound round,
        List<WorldDiplomacyDocument> documents,
        Func<int> currentDay,
        Func<int, string> formatDayFallback,
        Action<string> log)
    {

        if (round == null || documents == null || documents.Count == 0) return;
        List<WorldDiplomacyDocument> ordered = WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(documents.Where(x => x != null)).ToList();
        List<string> kingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(ordered
            .SelectMany(x => new[] { x.AuthorKingdomId, x.TargetKingdomId }.Concat(x.AddressedKingdomIds ?? new List<string>())));
        WorldDiplomacyRoundSummary summary = new WorldDiplomacyRoundSummary
        {
            ArchiveSchemaVersion = 1,
            RoundId = round.RoundId ?? "",
            CreatedDay = round.CompletedDay > 0 ? round.CompletedDay : currentDay(),
            SourceDocumentIds = ordered.Select(x => x.DocumentId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            KingdomIds = kingdomIds,
            Summary = WorldDiplomacyTextRules.BuildLocalRoundSummaryText(round, ordered, formatDayFallback)
        };
        foreach (WorldDiplomacyDocument document in ordered.Take(48))
        {
            List<string> declarationKingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(new[] { document.AuthorKingdomId }
                .Concat(WorldDiplomacyStructureRules.GetDocumentTargetIds(document)));
            summary.Facts.Add(new WorldDiplomacyRoundFact
            {
                Kind = "declaration",
                Text = "[宣言记录] " + WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(document, formatDayFallback),
                SourceDocumentIds = new List<string> { document.DocumentId },
                KingdomIds = declarationKingdomIds
            });
            if (document.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(document.MechanicalResult))
            {
                summary.Facts.Add(new WorldDiplomacyRoundFact
                {
                    Kind = "confirmed_result",
                    Text = "[游戏已执行] " + document.MechanicalResult,
                    SourceDocumentIds = new List<string> { document.DocumentId },
                    KingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(new[] { document.AuthorKingdomId }.Concat(WorldDiplomacyStructureRules.GetDocumentTargetIds(document, changedOnly: true)))
                });
            }
        }
        storage.RoundSummaries.RemoveAll(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, summary.RoundId));
        storage.RoundSummaries.Add(summary);
        log("local round archive committed round=" + summary.RoundId
            + " declarations=" + ordered.Count.ToString(CultureInfo.InvariantCulture)
            + " confirmed_results=" + summary.Facts.Count(x => string.Equals(x.Kind, "confirmed_result", StringComparison.OrdinalIgnoreCase)).ToString(CultureInfo.InvariantCulture));
    }

    public static void UpgradeRoundSummaryToStructuredArchive(
        WorldDiplomacyStorage storage,
        WorldDiplomacyRoundSummary summary,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<int, string> formatDayFallback)
    {

        if (summary == null || summary.ArchiveSchemaVersion >= 1) return;
        WorldDiplomacyRound round = resolveRound(summary.RoundId);
        List<WorldDiplomacyDocument> documents = WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(storage.Documents.Where(x => x != null
                && (WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, summary.RoundId)
                    || (summary.SourceDocumentIds ?? new List<string>()).Contains(x.DocumentId, StringComparer.OrdinalIgnoreCase))))
            .ToList();
        if (round == null || documents.Count == 0)
        {
            summary.ArchiveSchemaVersion = 1;
            summary.Summary = "旧版外交摘要，仅表示当时保存的宣言叙述，不能据此认定任何外交机制已经执行：" + WorldDiplomacyTextRules.Limit(summary.Summary, 1200);
            summary.Facts = (summary.Facts ?? new List<WorldDiplomacyRoundFact>()).Where(x => x != null).Select(x => new WorldDiplomacyRoundFact
            {
                Kind = "declaration",
                Text = "[旧版宣言摘要，不代表游戏已执行] " + WorldDiplomacyTextRules.Limit(x.Text, 360),
                SourceDocumentIds = x.SourceDocumentIds ?? new List<string>(),
                KingdomIds = x.KingdomIds ?? new List<string>()
            }).ToList();
            return;
        }
        summary.Summary = WorldDiplomacyTextRules.BuildLocalRoundSummaryText(round, documents, formatDayFallback);
        summary.SourceDocumentIds = documents.Select(x => x.DocumentId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        summary.KingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(documents.SelectMany(x => new[] { x.AuthorKingdomId, x.TargetKingdomId }.Concat(x.AddressedKingdomIds ?? new List<string>())));
        summary.Facts = new List<WorldDiplomacyRoundFact>();
        foreach (WorldDiplomacyDocument document in documents.Take(48))
        {
            summary.Facts.Add(new WorldDiplomacyRoundFact
            {
                Kind = "declaration", Text = "[宣言记录] " + WorldDiplomacyTextRules.BuildCompactDocumentMemoryLine(document, formatDayFallback),
                SourceDocumentIds = new List<string> { document.DocumentId },
                KingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(new[] { document.AuthorKingdomId }.Concat(WorldDiplomacyStructureRules.GetDocumentTargetIds(document)))
            });
            if (document.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(document.MechanicalResult)) summary.Facts.Add(new WorldDiplomacyRoundFact
            {
                Kind = "confirmed_result", Text = "[游戏已执行] " + document.MechanicalResult,
                SourceDocumentIds = new List<string> { document.DocumentId },
                KingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(new[] { document.AuthorKingdomId }.Concat(WorldDiplomacyStructureRules.GetDocumentTargetIds(document, changedOnly: true)))
            });
        }
        summary.ArchiveSchemaVersion = 1;
    }

    public static void PruneInvalidOffers(
        WorldDiplomacyRound round,
        Func<bool> worldReady,
        Func<string, (bool Resolved, bool Eliminated, bool HasAuthority)> probeParty,
        Func<string, string, bool> sameParty,
        Func<string, string, bool> isAtWar,
        Func<bool> allianceBehaviorAvailable,
        Func<string, string, bool> isAllyWithKingdom,
        Func<bool> tradeBehaviorAvailable,
        Func<string, string, bool> hasTradeAgreement,
        Func<WorldDiplomacyRoundOffer, bool> arePeaceTermsExecutable,
        Action<string> log)
    {
        if (round?.PendingOffers == null || round.PendingOffers.Count == 0) return;
        // SyncData can run before the Campaign behavior graph and Kingdom objects are ready.
        // Defer all stateful offer validation instead of permanently invalidating valid saved offers.
        if (!worldReady()) return;
        int invalidated = 0;
        foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers.Where(x => x != null && IsOpenLifecycleStatus(x.Status)))
        {
            (bool proposerResolved, bool proposerEliminated, bool proposerHasAuthority) = probeParty(offer.ProposerKingdomId);
            (bool targetResolved, bool targetEliminated, bool targetHasAuthority) = probeParty(offer.TargetKingdomId);
            bool invalid = IsOfferPartyInvalid(
                proposerResolved,
                targetResolved,
                sameParty(offer.ProposerKingdomId, offer.TargetKingdomId),
                proposerEliminated,
                targetEliminated,
                proposerHasAuthority,
                targetHasAuthority);
            if (!invalid)
            {
                string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(offer.Intent);
                bool atWar = isAtWar(offer.ProposerKingdomId, offer.TargetKingdomId);
                bool peaceTermsExecutable = false;
                if (string.Equals(intent, "propose_peace", StringComparison.Ordinal) && atWar)
                {
                    peaceTermsExecutable = arePeaceTermsExecutable(offer);
                }
                bool alreadyAllied = string.Equals(intent, "propose_alliance", StringComparison.Ordinal)
                    && allianceBehaviorAvailable() && !atWar && isAllyWithKingdom(offer.ProposerKingdomId, offer.TargetKingdomId);
                bool tradeAgreementPresent = string.Equals(intent, "propose_trade", StringComparison.Ordinal)
                    && tradeBehaviorAvailable() && !atWar && hasTradeAgreement(offer.ProposerKingdomId, offer.TargetKingdomId);
                invalid = IsOfferInvalidForIntent(
                    intent, atWar, allianceBehaviorAvailable(), alreadyAllied, tradeBehaviorAvailable(), tradeAgreementPresent, peaceTermsExecutable);
            }
            if (!invalid) continue;
            offer.Status = "invalidated";
            invalidated++;
        }
        foreach (IGrouping<string, WorldDiplomacyRoundOffer> group in round.PendingOffers
            .Where(x => x != null && IsOpenLifecycleStatus(x.Status))
            .GroupBy(x => ComposeOfferDeduplicationKey(WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), x.ProposerKingdomId, x.TargetKingdomId), StringComparer.OrdinalIgnoreCase))
        {
            foreach (WorldDiplomacyRoundOffer superseded in group.OrderByDescending(x => x.CreatedDay).ThenByDescending(x => x.SourceDocumentId, StringComparer.OrdinalIgnoreCase).Skip(1))
            {
                superseded.Status = "superseded";
                invalidated++;
            }
        }
        if (invalidated > 0)
        {
            log("stale diplomacy offers invalidated round=" + round.RoundId + " count=" + invalidated.ToString(CultureInfo.InvariantCulture));
        }
    }

        public static void CompletePolicySignal(
            WorldDiplomacyStorage storage,
            WorldDiplomacyPolicySignal signal,
            int maxProcessedSignalKeys,
            string reason,
            Action<string> log)
        {
            if (signal == null || storage == null)
            {
                return;
            }
            storage.PendingPolicySignals.RemoveAll(item => item != null && string.Equals(item.SignalKey, signal.SignalKey, StringComparison.OrdinalIgnoreCase));
            storage.ProcessedPolicySignalKeys.RemoveAll(key => string.Equals(key, signal.SignalKey, StringComparison.OrdinalIgnoreCase));
            storage.ProcessedPolicySignalKeys.Add(signal.SignalKey ?? "");
            CapToMostRecentEntries(
                storage.ProcessedPolicySignalKeys, maxProcessedSignalKeys);
            log?.Invoke("policy diplomacy signal completed key=" + (signal.SignalKey ?? "") + " reason=" + (reason ?? ""));
        }

        public static void AttachPolicySignalToRound(
            WorldDiplomacyRound round,
            WorldDiplomacyPolicySignal signal)
        {
            if (round == null || signal == null)
            {
                return;
            }
            round.ExternalSignalKeys ??= new List<string>();
            round.AttachedPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
            if (!round.ExternalSignalKeys.Contains(signal.SignalKey, StringComparer.OrdinalIgnoreCase))
            {
                round.ExternalSignalKeys.Add(signal.SignalKey);
            }
            if (!round.AttachedPolicySignals.Any(item => item != null
                && string.Equals(item.SignalKey, signal.SignalKey, StringComparison.OrdinalIgnoreCase)))
            {
                round.AttachedPolicySignals.Add(ClonePolicySignalRecord(signal));
            }
            string context = WorldDiplomacyPromptContractRules.BuildPolicySignalContext(signal);
            if (!string.IsNullOrWhiteSpace(context) && (round.ExternalOpeningContext ?? "").IndexOf(signal.SignalKey, StringComparison.OrdinalIgnoreCase) < 0)
            {
                round.ExternalOpeningContext = string.Join("\n", new[] { round.ExternalOpeningContext, context }.Where(text => !string.IsNullOrWhiteSpace(text))).Trim();
            }
        }

        public static void SettleTradeAllianceOfferCooldownsForClosedRound(
            WorldDiplomacyRound round,
            List<WorldDiplomacyOfferCooldown> cooldowns,
            Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> cooldownByKey,
            Action normalizeStorage,
            Action<string> log)
        {
    if (round == null) return;
    List<WorldDiplomacyOfferRoundObservation> observations = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
        .Where(x => x != null)
        .Select(x => new WorldDiplomacyOfferRoundObservation(
            x.ProposerKingdomId,
            x.TargetKingdomId,
            x.Intent,
            x.Status))
        .ToList();
    List<WorldDiplomacyOfferCooldownDecision> decisions = WorldDiplomacyOfferCooldownRules.EvaluateClosedRound(observations);
    bool recordFailures = !WorldDiplomacyRoundLifecycleRules.IsOfferCooldownSkippingCloseReason(round.CloseReason);
    int started = 0;
    int cleared = 0;
    foreach (WorldDiplomacyOfferCooldownDecision decision in decisions)
    {
        if (decision.Action == WorldDiplomacyOfferCooldownAction.ClearCooldown)
        {
            bool existed = cooldownByKey.ContainsKey(decision.Key);
            WorldDiplomacyRoundLifecycleRules.RemoveOfferCooldown(cooldowns, cooldownByKey, decision.Key);
            if (existed) cleared++;
        }
        else if (recordFailures)
        {
            WorldDiplomacyRoundLifecycleRules.UpsertOfferCooldown(cooldowns, cooldownByKey, decision.Key, round.CompletedDay, round.RoundId, normalizeStorage);
            started++;
        }
    }
    if (started > 0 || cleared > 0)
    {
        log?.Invoke("trade/alliance proposal cooldowns settled round=" + round.RoundId
            + " started=" + started.ToString(CultureInfo.InvariantCulture)
            + " cleared=" + cleared.ToString(CultureInfo.InvariantCulture)
            + " recordFailures=" + recordFailures);
    }

        }

        public static void RegisterRelayProposalOffer(
            WorldDiplomacyRound round,
            WorldDiplomacyDocument document,
            string intent)
        {
            if (round == null || document == null)
            {
                return;
            }
            foreach (WorldDiplomacyRoundOffer countered in round.PendingOffers.Where(x => x != null
                && IsOpenDirectedOffer(x, document.TargetKingdomId, document.AuthorKingdomId)
                && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), intent, StringComparison.OrdinalIgnoreCase)))
            {
                countered.Status = "countered";
            }
            foreach (WorldDiplomacyRoundOffer superseded in round.PendingOffers.Where(x => x != null
                && IsOpenDirectedOffer(x, document.AuthorKingdomId, document.TargetKingdomId)
                && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), intent, StringComparison.OrdinalIgnoreCase)))
            {
                superseded.Status = "superseded";
            }
            round.PendingOffers.RemoveAll(x => x != null
                && MatchesDocumentId(x.SourceDocumentId, document.DocumentId)
                && string.Equals(x.SourceActionId ?? "", document.ProcessingActionId ?? "", StringComparison.OrdinalIgnoreCase));
            round.PendingOffers.Add(new WorldDiplomacyRoundOffer
            {
                SourceDocumentId = document.DocumentId,
                SourceActionId = document.ProcessingActionId ?? "",
                ProposerKingdomId = document.AuthorKingdomId,
                TargetKingdomId = document.TargetKingdomId,
                Intent = intent,
                Status = "open",
                CreatedDay = document.Day
            });
        }

        public static List<WorldDiplomacyRoundOffer> SelectMatchingRelayResponseOffers(
            WorldDiplomacyRound round,
            WorldDiplomacyDocument document,
            string proposalIntent)
        {
            return (round?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
                .Where(x => IsOpenOfferToTarget(x, document?.AuthorKingdomId)
                    && string.Equals(x.Intent, proposalIntent, StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(document?.TargetKingdomId) || string.Equals(x.ProposerKingdomId, document.TargetKingdomId, StringComparison.OrdinalIgnoreCase))
                    && MatchesOfferSource(
                        x, document?.RespondingToOfferDocumentId, document?.RespondingToOfferActionId))
                .Take(2).ToList();
        }


    public static void AppendOpenOfferResponseIntents(
        WorldDiplomacyRound round,
        string authorKingdomId,
        string targetKingdomId,
        List<string> actions)
    {
        if (round == null || string.IsNullOrWhiteSpace(authorKingdomId) || string.IsNullOrWhiteSpace(targetKingdomId) || actions == null) return;
        IEnumerable<string> proposalIntents = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
            .Where(x => WorldDiplomacyRoundLifecycleRules.IsOpenDirectedOffer(x, targetKingdomId, authorKingdomId))
            .Select(x => WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent))
            .Where(WorldDiplomacyIntentVocabulary.IsProposalIntent));
        foreach (string proposalIntent in proposalIntents)
        {
            if (!WorldDiplomacyRoundLifecycleRules.TryResolveUniqueOpenProposalForRound(round, authorKingdomId, targetKingdomId, proposalIntent, out _)) continue;
            string acceptIntent = WorldDiplomacyIntentVocabulary.ProposalIntentToResponseIntent(proposalIntent, accepted: true);
            string rejectIntent = WorldDiplomacyIntentVocabulary.ProposalIntentToResponseIntent(proposalIntent, accepted: false);
            if (!string.IsNullOrWhiteSpace(acceptIntent)) actions.Add(acceptIntent);
            if (!string.IsNullOrWhiteSpace(rejectIntent)) actions.Add(rejectIntent);
        }
    }

    public static WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(
        WorldDiplomacyRound round,
        string authorKingdomId,
        string resultSettlementSlotId,
        bool isExternalResponseOnly,
        string sourceDocumentId,
        bool requireAnyOpenPeaceOffer = false)
    {
        if (round == null || string.IsNullOrWhiteSpace(authorKingdomId)) return null;
        IEnumerable<WorldDiplomacyRoundOffer> openPeaceOffers = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
            .Where(x => WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(x, authorKingdomId)
                && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase));
        if (isExternalResponseOnly && !string.IsNullOrWhiteSpace(sourceDocumentId))
        {
            WorldDiplomacyRoundOffer exactSource = openPeaceOffers.FirstOrDefault(x => string.Equals(
                x.SourceDocumentId,
                sourceDocumentId,
                StringComparison.OrdinalIgnoreCase));
            return exactSource;
        }
        if (round.ResultSettlementPending == true
            && !string.IsNullOrWhiteSpace(resultSettlementSlotId)
            && string.Equals(resultSettlementSlotId, round.ResultSettlementCurrentSlotId, StringComparison.OrdinalIgnoreCase))
        {
            WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
                .FirstOrDefault(x => x != null
                    && string.Equals(x.SlotId, resultSettlementSlotId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.KingdomId, authorKingdomId, StringComparison.OrdinalIgnoreCase)
                    && (x != null && WorldDiplomacyRoundLifecycleRules.SettlementSlotKindContains(x.Kind, "offer_response")));
            if (slot != null)
            {
                HashSet<string> sourceIds = new HashSet<string>(
                    slot.SourceDocumentIds ?? new List<string>(),
                    StringComparer.OrdinalIgnoreCase);
                WorldDiplomacyRoundOffer slotOffer = openPeaceOffers
                    .Where(x => sourceIds.Contains(x.SourceDocumentId ?? ""))
                    .OrderBy(x => x.CreatedDay)
                    .ThenBy(x => x.SourceDocumentId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.SourceActionId, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (slotOffer != null) return slotOffer;
            }
        }
        if (!requireAnyOpenPeaceOffer) return null;
        return openPeaceOffers
            .OrderBy(x => x.CreatedDay)
            .ThenBy(x => x.SourceDocumentId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SourceActionId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static List<string> BuildLegalDiplomaticActionIntents(
        WorldDiplomacyRound round,
        string authorKingdomId,
        string targetKingdomId,
        Func<List<string>> buildPotentialActions,
        Func<string, WorldDiplomacyDocument> resolveDocument)
    {
        List<string> actions = buildPotentialActions?.Invoke() ?? new List<string>();
        if (round != null && !string.IsNullOrWhiteSpace(authorKingdomId) && !string.IsNullOrWhiteSpace(targetKingdomId)
            && TryResolveUniqueOpenProposalForRound(round, authorKingdomId, targetKingdomId, "propose_peace", out _))
        {
            actions.Clear();
            actions.Add("accept_peace");
            actions.Add("reject_peace");
            return actions;
        }
        if (IsImmediateWarResponsePeaceSuppressed(round, round?.ResultSettlementCurrentSlotId,
                authorKingdomId, targetKingdomId, resolveDocument))
        {
            actions.RemoveAll(x => string.Equals(
                WorldDiplomacyIntentVocabulary.NormalizeIntent(x),
                "propose_peace",
                StringComparison.OrdinalIgnoreCase));
        }
        if (round?.ResultSettlementPending == true && !string.IsNullOrWhiteSpace(authorKingdomId) && !string.IsNullOrWhiteSpace(targetKingdomId))
        {
            WorldDiplomacyResultSettlementSlot currentSlot = SelectSettlementSlotBySlotAndKingdom(
                round.ResultSettlementSlots, round.ResultSettlementCurrentSlotId, authorKingdomId);
            bool hasAnswerableOfferForTarget = currentSlot != null
                && (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
                    .Any(x => IsOpenDirectedOffer(x, targetKingdomId, authorKingdomId));
            if (hasAnswerableOfferForTarget)
            {
                actions.Clear();
                AppendOpenOfferResponseIntents(round, authorKingdomId, targetKingdomId, actions);
                return NormalizeIdListPreserveOrder(actions);
            }
        }
        if (round != null && !string.IsNullOrWhiteSpace(authorKingdomId) && !string.IsNullOrWhiteSpace(targetKingdomId))
        {
            HashSet<string> ownOpenProposalIntents = new HashSet<string>((round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
                .Where(x => IsOpenDirectedOffer(x, authorKingdomId, targetKingdomId))
                .Select(x => WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent)), StringComparer.OrdinalIgnoreCase);
            actions.RemoveAll(ownOpenProposalIntents.Contains);
        }
        AppendOpenOfferResponseIntents(round, authorKingdomId, targetKingdomId, actions);
        return NormalizeIdListPreserveOrder(actions);
    }

    public static void AppendRelayResponseSourceContext(
        StringBuilder sb,
        WorldDiplomacyRound round,
        string authorKingdomId,
        WorldDiplomacyDocument responseSource,
        string requiredSourceDocumentId,
        List<WorldDiplomacyDocument> documents)
    {
        if (sb == null || round == null || string.IsNullOrWhiteSpace(authorKingdomId)) return;
        HashSet<string> sourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> answerableOfferSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> answerablePeaceOfferSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(responseSource?.DocumentId)) sourceIds.Add(responseSource.DocumentId);
        WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
            .FirstOrDefault(x => x != null
                && string.Equals(x.SlotId, round.ResultSettlementCurrentSlotId, StringComparison.OrdinalIgnoreCase));
        foreach (string id in slot?.SourceDocumentIds ?? new List<string>())
        {
            if (!string.IsNullOrWhiteSpace(id)) sourceIds.Add(id);
        }
        foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
        {
            if (!WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(offer, authorKingdomId)
                || string.IsNullOrWhiteSpace(offer.SourceDocumentId)) continue;
            sourceIds.Add(offer.SourceDocumentId);
            answerableOfferSourceIds.Add(offer.SourceDocumentId);
            if (string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(offer.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase))
            {
                answerablePeaceOfferSourceIds.Add(offer.SourceDocumentId);
            }
        }
        if (sourceIds.Count == 0) return;
        List<WorldDiplomacyDocument> sources = WorldDiplomacyRoundLifecycleRules.ThenOrderDocumentsByRecency((documents ?? new List<WorldDiplomacyDocument>())
                .Where(x => x != null && x.IsReadyForPublication && sourceIds.Contains(x.DocumentId))
                .OrderByDescending(x => WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.DocumentId, requiredSourceDocumentId))
                .ThenByDescending(x => WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.DocumentId, responseSource?.DocumentId))
                .ThenByDescending(x => answerablePeaceOfferSourceIds.Contains(x.DocumentId))
                .ThenByDescending(x => answerableOfferSourceIds.Contains(x.DocumentId)))
            .Take(4)
            .ToList();
        if (sources.Count == 0 && responseSource?.IsReadyForPublication == true) sources.Add(responseSource);
        if (sources.Count == 0) return;
        sb.AppendLine("【本篇回应依据】");
        if (slot?.RelatedKingdomIds?.Count > 0)
        {
            sb.AppendLine("当前处理义务涉及王国=" + string.Join(",", WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(slot.RelatedKingdomIds)));
        }
        foreach (WorldDiplomacyDocument source in sources)
        {
            List<string> actionFacts = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(source.Actions?.Where(x => x != null)
                .Select(x => (x.TargetKingdomId ?? "") + "=" + WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent)));
            if (actionFacts.Count == 0 && !string.IsNullOrWhiteSpace(source.Intent))
            {
                actionFacts.Add((source.TargetKingdomId ?? "") + "=" + WorldDiplomacyIntentVocabulary.NormalizeIntent(source.Intent));
            }
            int bodyLimit = WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(source.DocumentId, requiredSourceDocumentId)
                || WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(source.DocumentId, responseSource?.DocumentId)
                ? 1800
                : 700;
            string peaceOfferTerms = WorldDiplomacyDocumentFactRules.BuildPeaceOfferTermsFact(source, authorKingdomId);
            sb.AppendLine("- 来源=" + source.DocumentId
                + "|发文国=" + source.AuthorKingdomId
                + "|动作=" + string.Join("/", actionFacts)
                + "|标题=" + WorldDiplomacyTextRules.Limit(source.Title, 100)
                + (string.IsNullOrWhiteSpace(peaceOfferTerms) ? "" : "|和平原案条款=" + peaceOfferTerms)
                + "|正文摘要=" + WorldDiplomacyTextRules.Limit(source.Body, bodyLimit));
        }
    }

    public static string BuildGenerationLegalActionSignature(
        WorldDiplomacyJob job,
        WorldDiplomacyRound round,
        string authorKingdomId,
        WorldDiplomacyDocument responseSource,
        List<WorldDiplomacyThreat> threats,
        Func<List<string>> resolveSettlementTargetIds,
        Func<string, bool> isTargetUnavailable,
        Func<string, List<string>> buildDeclarationIntents)
    {
List<string> ids = new List<string>();
        if (job.IsRelayTurn && round?.ResultSettlementPending == true
            && !string.IsNullOrWhiteSpace(job.ResultSettlementSlotId))
        {
            ids.AddRange(resolveSettlementTargetIds?.Invoke() ?? new List<string>());
        }
        else if (job.IsRelayTurn && round?.RelayRouteKingdomIds != null) ids.AddRange(round.RelayRouteKingdomIds);
        else if (!string.IsNullOrWhiteSpace(job.TargetKingdomId)) ids.Add(job.TargetKingdomId);
        else if (job.CandidateKingdomIds?.Count > 0) ids.AddRange(job.CandidateKingdomIds);
        else if (round?.RelayRouteKingdomIds != null) ids.AddRange(round.RelayRouteKingdomIds);
        StringBuilder state = new StringBuilder();
        state.Append(authorKingdomId).Append('|').Append(round?.RoundId ?? "")
            .Append("|slot=").Append(job.ResultSettlementSlotId ?? "")
            .Append("|current_slot=").Append(round?.ResultSettlementCurrentSlotId ?? "");
        foreach (WorldDiplomacyPolicySignal signal in (round?.AttachedPolicySignals ?? new List<WorldDiplomacyPolicySignal>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.SignalKey))
            .OrderBy(x => x.SignalKey, StringComparer.OrdinalIgnoreCase))
        {
            state.Append("|policy=").Append(signal.SignalKey)
                .Append('@').Append(signal.PolicyId ?? "");
        }
        foreach (string id in NormalizeOrderedIdList(ids
            .Where(x => !string.Equals(x, authorKingdomId, StringComparison.OrdinalIgnoreCase))))
        {
            state.Append('\n').Append(id).Append('=');
            if (isTargetUnavailable == null || isTargetUnavailable(id))
            {
                state.Append("missing");
                continue;
            }
            List<string> actions = buildDeclarationIntents?.Invoke(id) ?? new List<string>();
            bool firstAction = true;
            foreach (string action in actions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (!firstAction) state.Append(',');
                firstAction = false;
                string normalized = WorldDiplomacyIntentVocabulary.NormalizeIntent(action);
                state.Append(normalized);
                string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(normalized);
                if (!string.IsNullOrWhiteSpace(proposalIntent)
                    && TryResolveUniqueOpenProposalForRound(round, authorKingdomId, id, proposalIntent, out string offerId, out string offerActionId))
                {
                    state.Append('@').Append(offerId).Append('#').Append(offerActionId);
                }
                else if (normalized == "comply_ultimatum")
                {
                    WorldDiplomacyThreat incoming = SelectOpenThreatBetween(threats, id, authorKingdomId);
                    if (IsThreatDecisionPending(incoming))
                    {
                        state.Append('@').Append(incoming.StageDocumentId ?? "")
                            .Append('#').Append(incoming.StageActionId ?? "");
                    }
                }
            }
        }
        return WorldDiplomacyPromptContractRules.StablePromptHash(state.ToString());
    }

    public static List<string> GetAuthorizedGenerationTargetIds(
        WorldDiplomacyJob source,
        WorldDiplomacyRound round,
        string authorKingdomId,
        Func<List<string>> resolveSettlementTargetIds,
        Func<string, bool> isCandidateAuthorized)
    {
        if (source == null || string.IsNullOrWhiteSpace(authorKingdomId)) return new List<string>();
        List<string> ids = new List<string>();
        bool resultSettlementRepair = source.IsRelayTurn
            && round?.ResultSettlementPending == true
            && !string.IsNullOrWhiteSpace(source.ResultSettlementSlotId);
        if (!resultSettlementRepair && !string.IsNullOrWhiteSpace(source.TargetKingdomId)) ids.Add(source.TargetKingdomId);
        if (source.AllowUntargeted) ids.AddRange(source.CandidateKingdomIds ?? new List<string>());
        if (source.IsRelayTurn)
        {
            if (resultSettlementRepair)
            {
                ids.AddRange(resolveSettlementTargetIds?.Invoke() ?? new List<string>());
            }
            else ids.AddRange(round?.RelayRouteKingdomIds ?? new List<string>());
        }
        if (ids.Count == 0) ids.AddRange(source.CandidateKingdomIds ?? new List<string>());
        return NormalizeIdListPreserveOrder(ids
            .Where(x => !string.Equals(x, authorKingdomId, StringComparison.OrdinalIgnoreCase)))
            .Where(x => isCandidateAuthorized == null || isCandidateAuthorized(x))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool TryDeriveGeneratedDiplomaticStructure(
        WorldDiplomacyJob job,
        WorldDiplomacyRound round,
        JObject json,
        string authorKingdomId,
        string targetKingdomId,
        string intent,
        List<WorldDiplomacyThreat> threats,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        out string reason)
    {
        reason = "";
        if (json == null || string.IsNullOrWhiteSpace(authorKingdomId))
        {
            reason = "semantic_envelope_incomplete";
            return false;
        }
        json["responding_to_offer_document_id"] = "";
        json["responding_to_offer_action_id"] = "";
        json["responding_to_threat_document_id"] = "";
        json["responding_to_threat_action_id"] = "";
        string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent);
        if (!string.IsNullOrWhiteSpace(proposalIntent))
        {
            if (string.IsNullOrWhiteSpace(targetKingdomId) || !TryResolveUniqueOpenProposalForRound(round, authorKingdomId, targetKingdomId, proposalIntent, out string offerId, out string offerActionId))
            {
                reason = "offer_response_without_unique_open_offer";
                return false;
            }
            json["responding_to_offer_document_id"] = offerId;
            json["responding_to_offer_action_id"] = offerActionId;
            if (string.Equals(intent, "accept_peace", StringComparison.OrdinalIgnoreCase))
            {
                WorldDiplomacyDocument source = resolveDocument(offerId);
                json["peace_terms"] = WorldDiplomacyEnvelopeJsonRules.BuildPeaceTermsJson(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(source, offerActionId));
            }
            return true;
        }
        if (!string.Equals(intent, "comply_ultimatum", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.IsNullOrWhiteSpace(targetKingdomId))
        {
            reason = "comply_ultimatum_without_open_threat";
            return false;
        }
        HashSet<string> presented = new HashSet<string>(job?.PresentedThreatDocumentIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        WorldDiplomacyThreat threat = SelectUniquePresentedPendingThreat(
            threats, targetKingdomId, authorKingdomId, presented);
        if (threat == null)
        {
            reason = "comply_ultimatum_without_unique_presented_threat";
            return false;
        }
        json["responding_to_threat_document_id"] = threat.StageDocumentId ?? "";
        json["responding_to_threat_action_id"] = threat.StageActionId ?? "";
        return true;
    }



        public static WorldDiplomacyJob BuildGeneratedDeclarationRepairJob(
        WorldDiplomacyJob source,
        WorldDiplomacyRound repairRound,
        string correction,
        List<WorldDiplomacyLlmMessage> messages,
        List<string> authorizedTargetIds,
        string newJobId)
    {
        bool resultSettlementRepair = source.IsRelayTurn
            && repairRound?.ResultSettlementPending == true
            && !string.IsNullOrWhiteSpace(source.ResultSettlementSlotId);
        WorldDiplomacyJob repair = new WorldDiplomacyJob
        {
            JobId = newJobId,
            Kind = "generate",
            Priority = source.Priority + 100,
            CreatedDay = source.CreatedDay,
            ExchangeId = source.ExchangeId ?? "",
            RoundId = source.RoundId ?? "",
            AuthorKingdomId = source.AuthorKingdomId ?? "",
            TargetKingdomId = source.TargetKingdomId ?? "",
            SourceDocumentId = source.SourceDocumentId ?? "",
            IsResponse = source.IsResponse,
            ForcedIntent = "",
            IsExternalResponseOnly = source.IsExternalResponseOnly,
            IsReminder = source.IsReminder,
            IsRelayTurn = source.IsRelayTurn,
            AllowUntargeted = source.AllowUntargeted,
            PreviousKingdomId = source.PreviousKingdomId ?? "",
            ResultSettlementSlotId = source.ResultSettlementSlotId ?? "",
            AllowAutonomousNoAction = false,
            CandidateKingdomIds = resultSettlementRepair
                ? new List<string>(authorizedTargetIds)
                : new List<string>(source.CandidateKingdomIds ?? new List<string>()),
            PresentedThreatDocumentIds = new List<string>(source.PresentedThreatDocumentIds ?? new List<string>()),
            PresentedThreatFollowThroughDocumentIds = new List<string>(source.PresentedThreatFollowThroughDocumentIds ?? new List<string>()),
            WasAtWarWhenQueued = source.WasAtWarWhenQueued,
            SystemPrompt = source.SystemPrompt ?? "",
            UserPrompt = correction,
            LlmMessages = messages,
            ProfiledKingdomId = source.ProfiledKingdomId ?? "",
            StrategicProfileKingdomId = source.StrategicProfileKingdomId ?? "",
            CacheAffinityKey = source.CacheAffinityKey ?? "",
            HistoryThroughSequence = source.HistoryThroughSequence,
            HistoryRevision = source.HistoryRevision,
            HistoryPrefixHash = source.HistoryPrefixHash ?? "",
            HistoryEstimatedTokens = source.HistoryEstimatedTokens,
            HistorySnapshotThroughSequence = source.HistorySnapshotThroughSequence,
            HistorySnapshotHash = source.HistorySnapshotHash ?? "",
            MaxTokens = source.MaxTokens,
            SemanticRepairAttempts = source.SemanticRepairAttempts + 1
        };
        return repair;
    }
    public static void MarkSettlementSlotWaitingForPlayer(
        WorldDiplomacyRound round,
        WorldDiplomacyResultSettlementSlot slot,
        string receiverId,
        List<WorldDiplomacyPlayerOpportunity> opportunities,
        List<WorldDiplomacyDocument> documents,
        int currentDay)
    {
        if (round == null || slot == null || string.IsNullOrWhiteSpace(receiverId)) return;
        slot.Status = "waiting_player";
        round.ResultSettlementPlayerWaitingSinceDay = currentDay;
        round.RelayWaiting = true;
        WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(
            round, receiverId, "active", mandatoryReply: true);
        participant.MandatoryReplyPending = true;
        participant.MandatorySinceDay = currentDay;
        participant.LastTriggeredDocumentId = slot.SourceDocumentIds?.FirstOrDefault() ?? round.ResultSettlementTriggerDocumentId;
        RecordPlayerOpportunity(round, receiverId, opportunities, documents, currentDay);
    }

    public static void ScheduleSettlementRelayArrival(
        WorldDiplomacyRound round,
        WorldDiplomacyResultSettlementSlot slot,
        string receiverId,
        WorldDiplomacyStorage storage,
        int currentDay)
    {
        if (round == null || slot == null || storage == null) return;
        slot.Status = "scheduled";
        string lastPublishedAuthorId = SelectLastPublishedAuthorId(storage.Documents, round.RoundId);
        string previousKingdomId = ResolveSettlementPreviousSpeaker(
            slot.RelatedKingdomIds, lastPublishedAuthorId, round.InitiatorKingdomId);
        round.RelaySequence++;
        round.RelayWaiting = true;
        storage.RelayArrivals.Add(new WorldDiplomacyRelayArrival
        {
            RoundId = round.RoundId,
            FromKingdomId = previousKingdomId,
            ToKingdomId = receiverId,
            ResultSettlementSlotId = slot.SlotId,
            DueDay = currentDay,
            Sequence = round.RelaySequence
        });
        storage.RelayArrivals = OrderRelayArrivalsByDueDate(storage.RelayArrivals).ToList();
    }

    public static bool ShouldCloseRoundAfterInvalidSuppression(
        WorldDiplomacyRound activeRound,
        WorldDiplomacyRound round,
        List<WorldDiplomacyDocument> documents,
        List<WorldDiplomacyJob> jobs,
        string excludedDocumentId)
    {
        return round != null
            && ReferenceEquals(activeRound, round)
            && IsActiveRoundState(round.State)
            && string.IsNullOrWhiteSpace(round.RootDocumentId)
            && !(documents ?? new List<WorldDiplomacyDocument>()).Any(x => x != null
                && x.IsReadyForPublication
                && IsRecordInRound(x.RoundId, round.RoundId))
            && !(jobs ?? new List<WorldDiplomacyJob>()).Any(x => x != null
                && string.Equals(FirstNonEmpty(x.RoundId, x.ExchangeId), round.RoundId, StringComparison.OrdinalIgnoreCase)
                && !MatchesDocumentId(x.DocumentId, excludedDocumentId));
    }

    public static void ApplyThreatComplianceResolution(
        WorldDiplomacyThreat threat,
        WorldDiplomacyDocument document,
        int currentDay)
    {
        if (threat == null || document == null) return;
        threat.Status = "complied";
        threat.TargetDecision = "complied";
        threat.TargetDecisionDocumentId = document.DocumentId ?? "";
        threat.TargetDecisionActionId = document.ProcessingActionId ?? "";
        threat.TargetDecisionRoundId = document.RoundId ?? "";
        threat.TargetDecisionDay = currentDay;
        threat.ComplianceDocumentId = document.DocumentId ?? "";
        threat.ComplianceActionId = document.ProcessingActionId ?? "";
        threat.ResolutionRoundId = document.RoundId ?? "";
        threat.ResolutionDocumentId = document.DocumentId ?? "";
        threat.ResolutionActionId = document.ProcessingActionId ?? "";
        threat.ResolutionReason = "target_explicitly_complied";
        threat.UpdatedDay = currentDay;
        threat.ObligationRoundId = "";
        threat.ObligationClaimedDay = 0;
        threat.IssuerResolutionNoticePending = true;
    }

    public static void ReconcilePlayerDeclarationWithOpenOffer(
        WorldDiplomacyDocument document,
        string intent,
        WorldDiplomacyRound round,
        ref string targetId,
        ref string respondingToOfferDocumentId,
        Action<string> log)
    {
        if (document == null || round == null) return;
        string proposalIntent = WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent);
        if (string.IsNullOrWhiteSpace(proposalIntent)) return;
        string claimedOfferDocumentId = respondingToOfferDocumentId ?? "";
        IEnumerable<WorldDiplomacyRoundOffer> candidates = (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
            .Where(x => IsOpenOfferToTarget(x, document.AuthorKingdomId)
                && string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent), proposalIntent, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(claimedOfferDocumentId))
        {
            candidates = candidates.Where(x => MatchesDocumentId(x.SourceDocumentId, claimedOfferDocumentId));
        }
        string requestedTargetId = FirstNonEmpty(targetId, document.TargetKingdomId);
        if (!string.IsNullOrWhiteSpace(requestedTargetId))
        {
            candidates = candidates.Where(x => string.Equals(x.ProposerKingdomId, requestedTargetId, StringComparison.OrdinalIgnoreCase));
        }
        List<WorldDiplomacyRoundOffer> matches = candidates.Take(2).ToList();
        if (matches.Count != 1) return;
        WorldDiplomacyRoundOffer offer = matches[0];
        targetId = offer.ProposerKingdomId;
        respondingToOfferDocumentId = offer.SourceDocumentId;
        document.RespondingToOfferActionId = offer.SourceActionId ?? "";
        log?.Invoke("player declaration bound to open offer document=" + document.DocumentId
            + " offer=" + offer.SourceDocumentId + " intent=" + intent + " proposer=" + offer.ProposerKingdomId);
    }

    public static void BeginOrExtendRoundResultSettlement(
        WorldDiplomacyRound round,
        WorldDiplomacyDocument document,
        string closeReason,
        string roundStatus,
        WorldDiplomacyStorage storage,
        int currentDay,
        Func<WorldDiplomacyRound, string, bool> includeResultSettlementTarget,
        Func<string, string> createId,
        Action<WorldDiplomacyRound> refreshActionSlots,
        Action<string> log)
    {
        if (round == null || document == null) return;
        if (!round.ResultSettlementPending)
        {
            round.ResultSettlementPending = true;
            round.ResultSettlementTriggerDocumentId = document.DocumentId;
            round.ResultSettlementCloseReason = string.IsNullOrWhiteSpace(closeReason) ? "result_settled" : closeReason;
            round.ResultSettlementRoundStatus = NormalizeResultSettlementStatus(roundStatus);
            round.ResultSettlementSlots ??= new List<WorldDiplomacyResultSettlementSlot>();
            round.ResultSettlementWarDocumentIds ??= new List<string>();
            round.RelayWaiting = false;
            // A result near the old relay deadline must still leave enough bounded time for
            // every selected speaker and every newly addressed action target to answer.
            int settlementWindowDays = ComputeSettlementWindowDays(
                round.RelayRouteKingdomIds?.Count ?? 0);
            round.HardEndDay = ExtendHardEndDay(
                round.HardEndDay, currentDay, settlementWindowDays);
            storage?.RelayArrivals.RemoveAll(x => x != null
                && IsRecordInRound(x.RoundId, round.RoundId));
            storage?.Jobs.RemoveAll(x => x != null
                && IsJobOfKind(x, "generate")
                && string.Equals(FirstNonEmpty(x.RoundId, x.ExchangeId), round.RoundId, StringComparison.OrdinalIgnoreCase));
            log?.Invoke("round result settlement opened round=" + round.RoundId
                + " trigger=" + document.DocumentId + " reason=" + round.ResultSettlementCloseReason);
        }
        else if (IsResolvedRoundStatus(roundStatus))
        {
            round.ResultSettlementRoundStatus = "resolved";
        }
        InitializeResultSettlementRouteSlots(round, storage?.Documents, includeResultSettlementTarget, createId);
        AddWarResponseResultSettlementSlot(round, document, includeResultSettlementTarget, createId);
        refreshActionSlots?.Invoke(round);
    }

    public static bool EnsureRequestFitsInputBudget(
        WorldDiplomacyJob job,
        JArray messages,
        long inputTokenLimit,
        int historyCompressionTargetTokens,
        Func<string, int> estimateTokens,
        Func<long, string> buildHistoryBlock,
        Func<WorldDiplomacyJob, bool> rebuildPendingJob,
        Action<WorldDiplomacyJob, string> commitFailedJob,
        Action scheduleTokenCompression,
        Action<string> log)
    {
        long inputTokens = 0L;
        foreach (JToken message in messages)
            inputTokens += EstimateHistoryTokens((string)message["content"], estimateTokens) + EstimateHistoryTokens((string)message["role"], estimateTokens) + 4L;
        long limit = inputTokenLimit;
        if (inputTokens <= limit)
        {
            job.AwaitingHistoryCompression = false;
            return true;
        }
        if (!IsJobOfKind(job, "generate"))
        {
            commitFailedJob?.Invoke(job, "input budget exceeded before send: " + inputTokens + "/" + limit
                + "; single archive entry/snapshot or non-history prompt requires reduction");
            return false;
        }
        if (WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(job))
        {
            // A repair owns a frozen rejected prompt. Rebuild the declaration from current
            // authoritative state before compressing, rather than silently editing that chain.
            job.LlmMessages.Clear();
            job.SemanticRepairAttempts = 0;
            if (rebuildPendingJob?.Invoke(job) != true) commitFailedJob?.Invoke(job, "oversized repair could not be rebuilt");
            return false;
        }
        long historyTokens = EstimateHistoryTokens(buildHistoryBlock?.Invoke(job.HistoryThroughSequence) ?? "", estimateTokens);
        long availableHistoryTokens = limit - (inputTokens - historyTokens) - 1024L;
        if (availableHistoryTokens < 512L)
        {
            commitFailedJob?.Invoke(job, "non-history prompt alone exceeds input budget; history was retained");
            return false;
        }
        job.AwaitingHistoryCompression = true;
        job.InputBudgetHistoryTargetTokens = (int)Math.Min(historyCompressionTargetTokens, availableHistoryTokens / 2L);
        scheduleTokenCompression?.Invoke();
        log?.Invoke("generation deferred for history compression job=" + job.JobId + " input_tokens=" + inputTokens
            + " input_limit=" + limit + " history_target=" + job.InputBudgetHistoryTargetTokens);
        return false;
    }

    public static void RecordDiplomacyWeeklyMaterial(
        WorldDiplomacyDocument document,
        List<WorldDiplomacyDocument> documents,
        Action<string, string, string, string, string, string, bool, int, string> recordMaterial)
    {
        if (document == null || string.IsNullOrWhiteSpace(document.DocumentId))
        {
            return;
        }
        int day = Math.Max(0, document.Day);
        string roundKey = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.RoundId, document.DocumentId);
        List<WorldDiplomacyDocument> sameDay = documents
            .Where(item => item != null && item.IsReadyForPublication && item.Day == day
                && string.Equals(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(item.RoundId, item.DocumentId), roundKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.CreatedUtcTicks)
            .Take(6)
            .ToList();
        if (!sameDay.Any(item => WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(item.DocumentId, document.DocumentId)))
        {
            sameDay.Add(document);
        }
        StringBuilder snapshot = new StringBuilder();
        snapshot.Append("外交回合").Append(roundKey).Append("在本日出现以下公开进展：");
        foreach (WorldDiplomacyDocument item in sameDay.Take(6))
        {
            snapshot.Append(" ").Append(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(item.AuthorRulerName, item.AuthorKingdomName)).Append("发布《")
                .Append(WorldDiplomacyTextRules.Limit(item.Title, 80)).Append("》");
            if (!string.IsNullOrWhiteSpace(item.Body))
            {
                snapshot.Append("，核心主张：").Append(WorldDiplomacyTextRules.Limit(WorldDiplomacyTextRules.NormalizeBody(item.Body), 180));
            }
            if (item.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(item.MechanicalResult))
            {
                snapshot.Append("；[游戏已执行] ").Append(WorldDiplomacyTextRules.Limit(item.MechanicalResult, 120));
            }
            snapshot.Append("。");
        }
        snapshot.Append("尚未标注[游戏已执行]的内容只是公开主张、提案、接受或拒绝，不得写成已经完成的外交结果。");

        List<string> relatedKingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeTrimmedIdListPreserveOrder(sameDay
            .SelectMany(item => new[] { item.AuthorKingdomId, item.TargetKingdomId }
                .Concat(item.AddressedKingdomIds ?? new List<string>())));
        string stableBase = "world_diplomacy:" + roundKey + ":day:" + day.ToString(CultureInfo.InvariantCulture);
        string authorKingdomId = (document.AuthorKingdomId ?? "").Trim();
        recordMaterial(
            stableBase + ":world",
            "外交宣言进展 - " + WorldDiplomacyTextRules.Limit(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.Title, document.AuthorKingdomName), 80),
            snapshot.ToString(),
            authorKingdomId,
            document.AuthorRulerId ?? "",
            authorKingdomId,
            true,
            day,
            document.GameDate ?? "");
        foreach (string kingdomId in relatedKingdomIds.Where(id => !string.Equals(id, authorKingdomId, StringComparison.OrdinalIgnoreCase)))
        {
            recordMaterial(
                stableBase + ":kingdom:" + kingdomId,
                "与本国有关的外交宣言进展",
                snapshot.ToString(),
                kingdomId,
                document.AuthorRulerId ?? "",
                authorKingdomId,
                false,
                day,
                document.GameDate ?? "");
        }
    }



    public static void ProcessRoundLifecycle(
        WorldDiplomacyStorage storage,
        Func<int> currentDay,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Action<WorldDiplomacyRound, WorldDiplomacyDocument> enqueueRoundPlanJob,
        Action<WorldDiplomacyRound> scheduleResultSettlementTurn,
        Action<WorldDiplomacyRound> scheduleRelayHop,
        Action<string> closeActiveRound,
        Action<string> log)
    {
        WorldDiplomacyRound round = storage.ActiveRound;
        if (round == null || !WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State)) return;
        if (round.AutomaticCircuitBreakerTripped)
        {
            bool hasRunningRoundJob = storage.Jobs.Any(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId));
            switch (WorldDiplomacyRoundLifecycleRules.EvaluateRoundTerminalClose(
                hasRunningRoundJob, round.ResultSettlementPending))
            {
                case WorldDiplomacyRoundTerminalAction.CloseResultSettlement:
                    round.ResultSettlementSlots?.Clear();
                    round.RoundStatus = WorldDiplomacyRoundLifecycleRules.NormalizeResultSettlementStatus(
                        round.ResultSettlementRoundStatus);
                    closeActiveRound("result_settlement_circuit_breaker");
                    break;
                case WorldDiplomacyRoundTerminalAction.CloseRelay:
                    closeActiveRound("automatic_request_circuit_breaker");
                    break;
            }
            return;
        }
        bool pendingRoundJob = storage.Jobs.Any(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId));
        int day = currentDay();
        if (WorldDiplomacyRoundLifecycleRules.IsHardEndReached(day, round.HardEndDay))
        {
            switch (WorldDiplomacyRoundLifecycleRules.EvaluateRoundTerminalClose(
                pendingRoundJob, round.ResultSettlementPending))
            {
                case WorldDiplomacyRoundTerminalAction.WaitForRunningJob:
                    // Game time may continue while the background request is running. Let the
                    // already-started final turn finish instead of closing the round underneath it.
                    return;
                case WorldDiplomacyRoundTerminalAction.CloseResultSettlement:
                    round.ResultSettlementSlots?.Clear();
                    round.RoundStatus = WorldDiplomacyRoundLifecycleRules.NormalizeResultSettlementStatus(
                        round.ResultSettlementRoundStatus);
                    closeActiveRound("result_settlement_hard_end");
                    return;
                default:
                    closeActiveRound("relay_hard_end");
                    return;
            }
        }
        if (!round.RelayPlanned)
        {
            WorldDiplomacyDocument root = resolveDocument(round.RootDocumentId);
            if (root != null && root.IsReadyForPublication) enqueueRoundPlanJob(round, root);
            return;
        }
        if (round.ResultSettlementPending)
        {
            WorldDiplomacyResultSettlementSlot currentSlot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
                .FirstOrDefault(x => x != null
                    && string.Equals(x.SlotId, round.ResultSettlementCurrentSlotId, StringComparison.OrdinalIgnoreCase));
            if (currentSlot != null && WorldDiplomacyRoundLifecycleRules.IsPlayerSlotWaitingExpired(
                currentSlot.Status, round.ResultSettlementPlayerWaitingSinceDay, day))
            {
                WorldDiplomacyRoundLifecycleRules.SkipResultSettlementSlot(round, currentSlot.SlotId, currentSlot.KingdomId, "player_timeout", storage?.DiplomaticThreats, currentDay(), log);
                currentSlot = null;
            }
            if (!pendingRoundJob && !storage.RelayArrivals.Any(x => x != null
                && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId)))
            {
                round.RelayWaiting = currentSlot != null
                    && WorldDiplomacyRoundLifecycleRules.IsWaitingPlayerSlot(currentSlot.Status);
                if (!round.RelayWaiting) scheduleResultSettlementTurn(round);
            }
            return;
        }
        int activeAi = (round.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Count(x => x != null
            && WorldDiplomacyRoundLifecycleRules.IsActiveRelayParticipant(
                x.SelectedForRelay, x.IsPlayerAsync, x.State));
        if (activeAi <= 0)
        {
            closeActiveRound("relay_all_ai_withdrew");
            return;
        }
        if (!pendingRoundJob && !storage.RelayArrivals.Any(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId)))
        {
            round.RelayWaiting = false;
            scheduleRelayHop(round);
        }
    }

    public static void ReconcileActiveDiplomacyAfterLoad(
        WorldDiplomacyStorage storage,
        Func<int> currentDay,
        Action<WorldDiplomacyRound> scheduleResultSettlementTurn,
        Action<WorldDiplomacyRound> scheduleRelayHopImmediately,
        Action<string> closeActiveRound,
        Action<string> log)
    {
        WorldDiplomacyRound round = storage?.ActiveRound;
        if (round == null || !WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State)) return;
        if (WorldDiplomacyRoundLifecycleRules.IsHardEndReached(currentDay(), round.HardEndDay))
        {
            closeActiveRound("relay_hard_end_after_load");
            return;
        }
        bool hasPersistedWork = (storage.Jobs ?? new List<WorldDiplomacyJob>()).Any(x => x != null
            && string.Equals(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(x.RoundId, x.ExchangeId), round.RoundId, StringComparison.OrdinalIgnoreCase))
            || (storage.RelayArrivals ?? new List<WorldDiplomacyRelayArrival>()).Any(x => x != null
                && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId));
        bool playerWaiting = (storage.PlayerOpportunities ?? new List<WorldDiplomacyPlayerOpportunity>()).Any(x => x != null
            && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId)
            && WorldDiplomacyRoundLifecycleRules.IsPlayerOpportunityOfStatus(x, "open"));
        WorldDiplomacyRoundReconcileDecision decision = WorldDiplomacyRoundLifecycleRules.EvaluateReconcileAfterLoad(
            new WorldDiplomacyRoundReconcileInput
            {
                RoundActive = true,
                CurrentDay = currentDay(),
                HardEndDay = round.HardEndDay,
                HasPersistedWork = hasPersistedWork,
                PlayerWaiting = playerWaiting,
                RelayWaiting = round.RelayWaiting,
                ResultSettlementPending = round.ResultSettlementPending,
                RelayPlanned = round.RelayPlanned,
                HasRootDocument = !string.IsNullOrWhiteSpace(round.RootDocumentId)
            });
        if (decision.ClearOrphanedRelayWait && round.RelayWaiting)
        {
            round.RelayWaiting = false;
            log("reconciled orphaned diplomacy wait after load round=" + round.RoundId);
        }
        switch (decision.Action)
        {
            case WorldDiplomacyRoundReconcileAction.ScheduleResultSettlementTurn:
                scheduleResultSettlementTurn(round);
                break;
            case WorldDiplomacyRoundReconcileAction.ScheduleRelayImmediately:
                scheduleRelayHopImmediately(round);
                break;
            case WorldDiplomacyRoundReconcileAction.CloseMissingRootDocument:
                closeActiveRound("technical_missing_root_after_load");
                break;
        }
    }



        public delegate bool TryGetGeneratedLegalityViolation(
            WorldDiplomacyJob job,
            JObject json,
            string authorKingdomId,
            string fallbackTargetKingdomId,
            out string resolvedTargetKingdomId,
            out string reason);

    public static void FinalizePublishedDocumentAfterAnalysis(
        WorldDiplomacyDocument document,
        string authorKingdomId,
        string targetKingdomId,
        string normalizedIntent,
        bool recordNoActionDecision,
        List<WorldDiplomacyThreat> threats,
        Action<WorldDiplomacyDocument, string, string, string> recordThreatDecisions,
        Func<WorldDiplomacyDocument, string, string, string, bool> deferUnresolvedThreatAction,
        Action<WorldDiplomacyThreat, WorldDiplomacyDocument> applyThreatReputationPenalty,
        Action<WorldDiplomacyDocument> settleReputation,
        Action<WorldDiplomacyDocument, string> startPropagation,
        Action<WorldDiplomacyDocument> recordWeeklyMaterial,
        Action<WorldDiplomacyDocument> reconcilePlayerDeclaration,
        Action<WorldDiplomacyDocument> appendCanonicalEvents,
        Action<WorldDiplomacyThreat> appendThreatHistory,
        Action<WorldDiplomacyThreat> appendThreatDomesticPenaltyHistory,
        Action<WorldDiplomacyThreat> appendThreatIssuerRewardHistory,
        Action<WorldDiplomacyThreat, WorldDiplomacyThreatNonComplianceEvent> appendThreatNonComplianceHistory,
        Action<string> scheduleDeferredHistoryRetry,
        Action<WorldDiplomacyDocument> handleRoundDocumentProcessed,
        Action<string> log)
    {
        WorldDiplomacyDocumentPublicationApplication.FinalizePublishedDocumentAfterAnalysis(
            document,
            authorKingdomId,
            targetKingdomId,
            normalizedIntent,
            recordNoActionDecision,
            threats,
            recordThreatDecisions,
            deferUnresolvedThreatAction,
            applyThreatReputationPenalty,
            settleReputation,
            startPropagation,
            recordWeeklyMaterial,
            reconcilePlayerDeclaration,
            appendCanonicalEvents,
            appendThreatHistory,
            appendThreatDomesticPenaltyHistory,
            appendThreatIssuerRewardHistory,
            appendThreatNonComplianceHistory,
            scheduleDeferredHistoryRetry,
            handleRoundDocumentProcessed,
            log);
    }



    public static void CommitCompletedLlmJobResult(
        WorldDiplomacyJob job,
        string resultContent,
        bool resultSuccess,
        bool resultIsServiceFailure,
        bool resultIsOutputTruncated,
        string resultError,
        WorldDiplomacyStorage storage,
        int currentHour,
        int failedServiceCooldownHours,
        Func<WorldDiplomacyJob, bool> hasStaleThreatPresentation,
        Func<WorldDiplomacyJob, bool> refreshThreatPresentation,
        Func<WorldDiplomacyJob, bool> hasStaleActionPresentation,
        Func<WorldDiplomacyJob, bool> refreshActionPresentation,
        Action<WorldDiplomacyJob, string> handleTruncatedDraft,
        Action<WorldDiplomacyJob, string> commitGeneratedDocument,
        Action<WorldDiplomacyJob, string> commitAnalysis,
        Action<WorldDiplomacyJob, string> commitCompression,
        Action<WorldDiplomacyJob, string> commitRoundPlan,
        Action<WorldDiplomacyJob, string> commitRoundCompression,
        Action<WorldDiplomacyJob, string> commitFailedJob,
        Action<string> removeJob,
        Action<string> log)
    {
        var effects = new WorldDiplomacyCompletionCallbacks(hasStaleThreatPresentation, refreshThreatPresentation, hasStaleActionPresentation, refreshActionPresentation, handleTruncatedDraft, commitGeneratedDocument, commitAnalysis, commitCompression, commitRoundPlan, commitRoundCompression, commitFailedJob, removeJob, log);
        WorldDiplomacyCompletionApplication.Complete(job, resultContent, resultSuccess,
            resultIsServiceFailure, resultIsOutputTruncated, resultError, storage,
            currentHour, failedServiceCooldownHours, ref effects);
    }

public static void ProcessDueRelayArrivals(
        WorldDiplomacyStorage storage,
        int currentDay,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<string, string> resolveKingdomId,
        Func<string, bool> hasAuthority,
        Func<string, bool> isPlayerKingdom,
        Action<string, WorldDiplomacyDocument> markPlayerCourtReached,
        Action<WorldDiplomacyRound> scheduleSettlement,
        Action<WorldDiplomacyRound> advanceRelay,
        Action<WorldDiplomacyRelayArrival, WorldDiplomacyDocument, WorldDiplomacyRound, string> enqueueRelayTurn,
        Action<string> log)
{
	// The persisted queue is sorted on load and at both insertion sites. Snapshot
	// only the due prefix so a callback that enqueues another arrival cannot make
	// it part of this daily batch.
	List<WorldDiplomacyRelayArrival> arrivals = storage?.RelayArrivals;
	if (arrivals == null || arrivals.Count == 0 || arrivals[0]?.DueDay > currentDay) return;
	List<WorldDiplomacyRelayArrival> due = new List<WorldDiplomacyRelayArrival>(Math.Min(8, arrivals.Count));
	for (int index = 0; index < arrivals.Count && due.Count < 8; index++)
	{
		WorldDiplomacyRelayArrival candidate = arrivals[index];
		if (candidate == null) continue;
		if (candidate.DueDay > currentDay) break;
		due.Add(candidate);
	}
	foreach (WorldDiplomacyRelayArrival arrival in due)
	{
		storage.RelayArrivals.Remove(arrival);
		WorldDiplomacyRound round = resolveRound?.Invoke(arrival.RoundId);
		if (round == null) continue;
		if (IsArrivalStale(
			IsActiveRoundState(round.State),
			arrival.Sequence, round.RelaySequence)) continue;
		if (round.ResultSettlementPending)
		{
			WorldDiplomacyResultSettlementSlot settlementSlot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
				.FirstOrDefault(x => x != null
					&& string.Equals(x.SlotId, arrival.ResultSettlementSlotId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.KingdomId, arrival.ToKingdomId, StringComparison.OrdinalIgnoreCase));
			string settlementReceiverId = resolveKingdomId?.Invoke(arrival.ToKingdomId);
			WorldDiplomacyRelayArrivalAction settlementAction =
				EvaluateArrivalAction(
					new WorldDiplomacyArrivalEvaluationInput
					{
						SettlementPending = true,
						SettlementSlotFound = settlementSlot != null,
						CurrentSlotMatches = settlementSlot != null && string.Equals(
							round.ResultSettlementCurrentSlotId, arrival.ResultSettlementSlotId,
							StringComparison.OrdinalIgnoreCase),
						ReceiverEligible = settlementReceiverId != null
							&& hasAuthority?.Invoke(settlementReceiverId) == true
					});
			if (settlementAction == WorldDiplomacyRelayArrivalAction.RescheduleSettlementTurn)
			{
				round.RelayWaiting = false;
				scheduleSettlement?.Invoke(round);
				continue;
			}
			if (settlementAction == WorldDiplomacyRelayArrivalAction.SkipSettlementSlotAndReschedule)
			{
				SkipResultSettlementSlot(round, settlementSlot.SlotId, settlementSlot.KingdomId, "receiver_ineligible", storage?.DiplomaticThreats, currentDay, log);
				scheduleSettlement?.Invoke(round);
				continue;
			}
			List<WorldDiplomacyDocument> settlementRoundDocuments = OrderDocumentsByRecency(storage.Documents
					.Where(x => x != null && x.IsReadyForPublication
						&& IsRecordInRound(x.RoundId, round.RoundId)))
				.ToList();
			foreach (WorldDiplomacyDocument known in settlementRoundDocuments)
			{
				WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage?.KingdomKnowledge, settlementReceiverId, known.DocumentId, currentDay);
				WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(storage?.NobleKnowledge, settlementReceiverId, known.DocumentId, currentDay);
				markPlayerCourtReached?.Invoke(settlementReceiverId, known);
			}
			settlementSlot.Status = "inflight";
			WorldDiplomacyDocument settlementSource = settlementRoundDocuments.FirstOrDefault();
			enqueueRelayTurn?.Invoke(arrival, settlementSource, round, settlementSlot.SlotId);
			continue;
		}
		int index = (round.RelayRouteKingdomIds ?? new List<string>()).FindIndex(x => string.Equals(x, arrival.ToKingdomId, StringComparison.OrdinalIgnoreCase));
		string receiverId = resolveKingdomId?.Invoke(arrival.ToKingdomId);
					WorldDiplomacyRelayArrivalAction relayAction =
			EvaluateArrivalAction(
				new WorldDiplomacyArrivalEvaluationInput
				{
					RouteIndexFound = index >= 0,
					ReceiverEligible = receiverId != null
						&& hasAuthority?.Invoke(receiverId) == true,
					ReceiverIsPlayer = receiverId != null && isPlayerKingdom?.Invoke(receiverId) == true
				});
		if (relayAction == WorldDiplomacyRelayArrivalAction.AdvanceRelay)
		{
			round.RelayWaiting = false;
			advanceRelay?.Invoke(round);
			continue;
		}
		round.RelayCursor = index;
		List<WorldDiplomacyDocument> relayRoundDocuments = OrderDocumentsByRecency(storage.Documents
				.Where(x => x != null && x.IsReadyForPublication
					&& IsRecordInRound(x.RoundId, round.RoundId)))
			.ToList();
		foreach (WorldDiplomacyDocument document in relayRoundDocuments)
		{
			WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(storage?.KingdomKnowledge, receiverId, document.DocumentId, currentDay);
			WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(storage?.NobleKnowledge, receiverId, document.DocumentId, currentDay);
			markPlayerCourtReached?.Invoke(receiverId, document);
		}
		if (relayAction == WorldDiplomacyRelayArrivalAction.AdvanceRelayAfterPlayerOpportunity)
		{
			RecordPlayerOpportunity(round, receiverId, storage?.PlayerOpportunities, storage?.Documents, currentDay);
			round.RelayWaiting = false;
			advanceRelay?.Invoke(round);
			continue;
		}
		WorldDiplomacyDocument source = relayRoundDocuments.FirstOrDefault();
		enqueueRelayTurn?.Invoke(arrival, source, round, null);
	}
}

public static void NotifyExternalDiplomacyResolved(
        string action,
        string initiatorId,
        string targetId,
        string reason,
        bool initiatorIsPlayer,
        WorldDiplomacyStorage storage,
        int currentDay,
        Func<string, bool> proposalTakenEffect,
        Action<WorldDiplomacyOfferDomain> clearBilateralCooldowns,
        Func<string, string, string, bool, WorldDiplomacyDocument> createDocument,
        Func<string, string> buildFactBody,
        Func<bool, WorldDiplomacyRound> ensureActiveRound,
        Func<WorldDiplomacyRound, bool> canFactJoinRound,
        Func<WorldDiplomacyRound, string, bool> tryIncludeSettlementTarget,
        Func<string, string> createId,
        Action<WorldDiplomacyDocument> addDocument,
        Action<WorldDiplomacyDocument> startPropagation,
        Action<WorldDiplomacyDocument> appendCanonicalEvents,
        Action<string> scheduleDeferredRetry,
        Action<WorldDiplomacyDocument> handleRoundDocumentProcessed,
        Action<string> log)
{
	if (string.IsNullOrWhiteSpace(initiatorId) || string.IsNullOrWhiteSpace(targetId)
		|| string.Equals(initiatorId, targetId, StringComparison.OrdinalIgnoreCase))
	{
		return;
	}
	string normalizedAction = WorldDiplomacyIntentVocabulary.NormalizeIntent(action);
	if (!WorldDiplomacyIntentVocabulary.IsExternallyResolvedDiplomaticIntent(normalizedAction))
	{
		log?.Invoke("external resolved diplomacy ignored because action is not an executed result action="
			+ (normalizedAction ?? "") + " initiator=" + (initiatorId ?? "")
			+ " target=" + (targetId ?? ""));
		return;
	}
	if (string.Equals(normalizedAction, "accept_trade", StringComparison.OrdinalIgnoreCase))
	{
		if (proposalTakenEffect?.Invoke("propose_trade") != true)
		{
			log?.Invoke("external trade acceptance ignored because the live trade agreement was not created initiator="
				+ initiatorId + " target=" + targetId);
			return;
		}
		MarkOpenBilateralOffersAccepted(storage?.ActiveRound, initiatorId, targetId, WorldDiplomacyOfferDomain.Trade);
		clearBilateralCooldowns?.Invoke(WorldDiplomacyOfferDomain.Trade);
	}
	else if (string.Equals(normalizedAction, "accept_alliance", StringComparison.OrdinalIgnoreCase))
	{
		if (proposalTakenEffect?.Invoke("propose_alliance") != true)
		{
			log?.Invoke("external alliance acceptance ignored because the live alliance was not created initiator="
				+ initiatorId + " target=" + targetId);
			return;
		}
		MarkOpenBilateralOffersAccepted(storage?.ActiveRound, initiatorId, targetId, WorldDiplomacyOfferDomain.Alliance);
		clearBilateralCooldowns?.Invoke(WorldDiplomacyOfferDomain.Alliance);
	}
	WorldDiplomacyDocument fact = createDocument?.Invoke(
		"口头外交结果",
		buildFactBody?.Invoke(normalizedAction) ?? "",
		"oral_diplomacy",
		initiatorIsPlayer);
	if (fact == null) return;
	fact.Intent = normalizedAction;
	fact.Commitment = "binding";
	fact.AnalysisStatus = "external_fact";
	fact.MechanicalResult = "已由口头外交执行";
	fact.ChangedDiplomaticState = true;
	fact.HistoryDeclarationRecorded = true;
	WorldDiplomacyRound activeRound = storage?.ActiveRound;
	WorldDiplomacyRound round = activeRound == null
		? ensureActiveRound?.Invoke(initiatorIsPlayer)
		: canFactJoinRound?.Invoke(activeRound) == true
			? activeRound
			: null;
	bool appendedExternalSettlementTarget = round?.ResultSettlementPending == true
		&& !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, targetId);
	if (appendedExternalSettlementTarget
		&& tryIncludeSettlementTarget?.Invoke(round, targetId) != true) round = null;
	else if (appendedExternalSettlementTarget)
	{
		AddOrMergeResultSettlementSlot(round, targetId, "route",
			fact.DocumentId, initiatorId, prioritize: false, tryIncludeSettlementTarget, createId);
	}
	fact.RoundId = round?.RoundId ?? "";
	fact.ExchangeId = fact.RoundId;
	fact.AddressedKingdomIds = new List<string> { targetId };
	// This document records a diplomacy action that has already resolved elsewhere; it must not start a reply chain.
	fact.RequiresResponse = false;
	addDocument?.Invoke(fact);
	if (normalizedAction == "declare_war")
	{
		WorldDiplomacyWarPressureRules.ClearWarPressure(storage?.WarPressure, initiatorId, targetId, currentDay);
	}
	try
	{
		startPropagation?.Invoke(fact);
	}
	catch (Exception ex)
	{
		log?.Invoke("external diplomacy propagation failed document=" + fact.DocumentId + " error=" + ex.Message);
	}
	try
	{
		appendCanonicalEvents?.Invoke(fact);
	}
	catch (Exception ex)
	{
		scheduleDeferredRetry?.Invoke(fact.DocumentId);
		log?.Invoke("external diplomacy canonical history append deferred document=" + fact.DocumentId + " error=" + ex.Message);
	}
	if (round == null)
	{
		fact.RoundProgressHandled = true;
		log?.Invoke("external diplomacy fact kept outside unrelated active round document=" + fact.DocumentId
			+ " activeRound=" + (activeRound?.RoundId ?? ""));
		return;
	}
	try
	{
		handleRoundDocumentProcessed?.Invoke(fact);
	}
	catch (Exception ex)
	{
		log?.Invoke("external diplomacy round progress deferred document=" + fact.DocumentId + " error=" + ex.Message);
	}
}

public static void PrepareAnalysisJob(
        WorldDiplomacyDocument document,
        int priority,
        WorldDiplomacyStorage storage,
        int currentDay,
        int analysisMaxTokens,
        Func<string, string> createId,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<WorldDiplomacyRound, string> getCommonContract,
        Func<WorldDiplomacyDocument, string> buildAnalysisPrompt,
        Action<WorldDiplomacyJob> enqueueJob)
{
	if (document == null)
	{
		return;
	}
	WorldDiplomacyRound owningRound = resolveRound?.Invoke(FirstNonEmpty(document.RoundId, document.ExchangeId));
	string frozenCommonContract = getCommonContract?.Invoke(owningRound);
	WorldDiplomacyJob job = new WorldDiplomacyJob
	{
		JobId = createId?.Invoke("diplomacy_analyze"),
		Kind = "analyze",
		Priority = priority,
		CreatedDay = currentDay,
		ExchangeId = document.ExchangeId ?? "",
		DocumentId = document.DocumentId ?? "",
		AuthorKingdomId = document.AuthorKingdomId ?? "",
		TargetKingdomId = document.TargetKingdomId ?? "",
		PresentedThreatDocumentIds = SelectPresentedThreatStageDocumentIds(storage?.DiplomaticThreats, document.AuthorKingdomId),
		PresentedThreatFollowThroughDocumentIds = SelectNoncompliedThreatStageDocumentIds(storage?.DiplomaticThreats, document.AuthorKingdomId),
		IsResponse = document.IsResponse,
		SystemPrompt = WorldDiplomacyPromptContractRules.BuildAnalysisSystemPrompt(frozenCommonContract),
		UserPrompt = buildAnalysisPrompt?.Invoke(document),
		CacheAffinityKey = "analyze",
		MaxTokens = analysisMaxTokens
	};
	enqueueJob?.Invoke(job);
}

public static void PrepareRoundPlanJob(
        WorldDiplomacyRound round,
        WorldDiplomacyDocument root,
        WorldDiplomacyStorage storage,
        int currentDay,
        int analysisMaxTokens,
        Func<string, string> createId,
        Func<WorldDiplomacyRound, string, List<string>> getPlanCandidates,
        Func<WorldDiplomacyRound, string> buildSystemPrompt,
        Func<WorldDiplomacyDocument, List<string>, string> buildUserPrompt,
        Action<WorldDiplomacyJob> enqueueJob,
        Action<string> closeActiveRound)
{
	if (round == null || root == null || round.RelayPlanned
		|| !ReferenceEquals(storage?.ActiveRound, round)
		|| !IsActiveRoundState(round.State)
		|| storage.Jobs.Any(x => IsJobOfKind(x, "round_plan")
			&& IsRecordInRound(x.RoundId, round.RoundId))) return;
	List<string> candidates = getPlanCandidates?.Invoke(round, root.AuthorKingdomId) ?? new List<string>();
	if (candidates.Count == 0)
	{
		closeActiveRound?.Invoke("round_plan_no_actionable_participants");
		return;
	}
	WorldDiplomacyJob job = new WorldDiplomacyJob
	{
		JobId = createId?.Invoke("diplomacy_round_plan"),
		Kind = "round_plan",
		Priority = 85,
		CreatedDay = currentDay,
		RoundId = round.RoundId,
		DocumentId = root.DocumentId,
		AuthorKingdomId = root.AuthorKingdomId,
		CandidateKingdomIds = candidates,
		SystemPrompt = buildSystemPrompt?.Invoke(round),
		UserPrompt = buildUserPrompt?.Invoke(root, candidates),
		CacheAffinityKey = "diplomacy-round-plan:v6",
		MaxTokens = analysisMaxTokens
	};
	enqueueJob?.Invoke(job);
}




}
