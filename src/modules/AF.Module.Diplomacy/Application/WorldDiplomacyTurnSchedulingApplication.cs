using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;

namespace AnimusForge;

// Owns the next relay or settlement turn from the persisted round queues.
internal static class WorldDiplomacyTurnSchedulingApplication
{
    internal static void ScheduleNextRelayHop(
        WorldDiplomacyRound round,
        bool scheduleImmediately,
        WorldDiplomacyStorage storage,
        int currentDay,
        int relayPassDurationDays,
        Func<string, bool> hasAuthority,
        Action<WorldDiplomacyRound> scheduleResultSettlement,
        Action<string> closeActiveRound,
        Action<string> log)
    {
        if (round?.ResultSettlementPending == true)
        {
            scheduleResultSettlement?.Invoke(round);
            return;
        }
        if (round == null || !CanScheduleRelayHop(
            round.RelayPlanned,
            round.RelayWaiting,
            round.AutomaticCircuitBreakerTripped,
            IsActiveRoundState(round.State))) return;
        if (storage.RelayArrivals.Any(x => x != null && IsRecordInRound(x.RoundId, round.RoundId))
            || storage.Jobs.Any(x => x != null && x.IsRelayTurn && IsRecordInRound(x.RoundId, round.RoundId))) return;
        List<string> route = round.RelayRouteKingdomIds ?? new List<string>();
        if (!HasMinimumRelayRoute(route.Count))
        {
            closeActiveRound?.Invoke("relay_has_no_participants");
            return;
        }
        // Old saves can lose route members when kingdoms are eliminated or become controlled
        // vassals. Never trust the persisted cursor/direction after such a route rewrite.
        round.RelayCursor = NormalizeRelayCursor(round.RelayCursor, route.Count);
        round.RelayDirection = NormalizeRelayDirection(round.RelayDirection);
        int passDurationDays = NormalizeRelayPassDurationDays(round.RelayPassDurationDays, relayPassDurationDays);
        int nextIndex = FindPriorityThreatRelayIndex(round, route, storage?.DiplomaticThreats, hasAuthority);
        if (nextIndex < 0) nextIndex = FindNextRelayIndex(round, round.RelayCursor + round.RelayDirection, hasAuthority);
        if (nextIndex < 0)
        {
            CompleteRelayPassProgressAccounting(round, log);
            round.RelayDirection = NextRelayDirection(round.RelayDirection);
            round.RelayPassNumber++;
            round.RelayPassStartedDay += passDurationDays;
            if (ShouldForceTerminalMove(round.ConsecutiveNoActionPasses)
                && !round.FinalActionOpportunityIssued)
            {
                round.FinalActionOpportunityIssued = true;
                log?.Invoke("relay final resolution phase opened after consecutive no-action passes round=" + round.RoundId);
            }
            nextIndex = FindNextRelayIndex(round, round.RelayCursor + round.RelayDirection, hasAuthority);
        }
        if (nextIndex < 0)
        {
            closeActiveRound?.Invoke("relay_all_participants_withdrew");
            return;
        }
        int plannedDay = ComputeRelayArrivalDay(
            new WorldDiplomacyRelayArrivalPlanInput
            {
                RelayPassStartedDay = round.RelayPassStartedDay,
                PassDurationDays = passDurationDays,
                NextIndex = nextIndex,
                RouteCount = route.Count,
                RelayDirection = round.RelayDirection,
                ScheduleImmediately = scheduleImmediately,
                CurrentDay = currentDay,
                FinalActionOpportunityIssued = round.FinalActionOpportunityIssued,
                SubstantiveProgressCount = round.SubstantiveProgressCount,
                HardEndDay = round.HardEndDay
            });
        round.RelaySequence++;
        round.RelayWaiting = true;
        storage.RelayArrivals.Add(new WorldDiplomacyRelayArrival
        {
            RoundId = round.RoundId,
            FromKingdomId = route[round.RelayCursor],
            ToKingdomId = route[nextIndex],
            DueDay = plannedDay,
            Sequence = round.RelaySequence
        });
        storage.RelayArrivals = OrderRelayArrivalsByDueDate(storage.RelayArrivals).ToList();
    }

    internal static void ScheduleNextResultSettlementTurn(
        WorldDiplomacyRound round,
        WorldDiplomacyStorage storage,
        int currentDay,
        int maxRelayParticipants,
        Func<string, string> resolveKingdomId,
        Func<string, bool> hasAuthority,
        Func<string, bool> isPlayerKingdom,
        Func<WorldDiplomacyRound, string, int> actionableTargetCount,
        Action<WorldDiplomacyRound> refreshActionSlots,
        Action<string> closeActiveRound,
        Action<string> log)
    {
        if (!CanScheduleResultSettlementTurn(
            new WorldDiplomacySettlementTurnGateInput
            {
                ResultSettlementPending = round?.ResultSettlementPending == true,
                RelayPlanned = round?.RelayPlanned == true,
                RoundActive = round != null
                    && IsActiveRoundState(round.State),
                HasPendingArrival = round != null && (storage?.RelayArrivals ?? new List<WorldDiplomacyRelayArrival>())
                    .Any(x => x != null && IsRecordInRound(x.RoundId, round.RoundId)),
                HasPendingJob = round != null && (storage?.Jobs ?? new List<WorldDiplomacyJob>())
                    .Any(x => x != null && IsRecordInRound(x.RoundId, round.RoundId))
            })) return;

        refreshActionSlots?.Invoke(round);
        for (int guard = 0; guard < maxRelayParticipants + 4; guard++)
        {
            WorldDiplomacyResultSettlementSlot slot = (round.ResultSettlementSlots ?? new List<WorldDiplomacyResultSettlementSlot>())
                .FirstOrDefault(x => x != null);
            WorldDiplomacySettlementSlotAction slotAction;
            string receiverId = null;
            if (slot == null)
            {
                slotAction = EvaluateSettlementSlotAction(
                    new WorldDiplomacySettlementSlotEvaluationInput());
            }
            else
            {
                round.ResultSettlementCurrentSlotId = slot.SlotId;
                receiverId = resolveKingdomId?.Invoke(slot.KingdomId);
                bool receiverEligible = receiverId != null
                        && hasAuthority?.Invoke(receiverId) == true;
                slotAction = EvaluateSettlementSlotAction(
                    new WorldDiplomacySettlementSlotEvaluationInput
                    {
                        HasSlot = true,
                        ReceiverEligible = receiverEligible,
                        ActionableTargetCount = receiverEligible
                            ? actionableTargetCount?.Invoke(round, receiverId) ?? 0 : 0,
                        ReceiverIsPlayer = receiverId != null && isPlayerKingdom?.Invoke(receiverId) == true
                    });
            }
            if (slotAction == WorldDiplomacySettlementSlotAction.CloseRound)
            {
                round.RoundStatus = NormalizeResultSettlementStatus(
                    round.ResultSettlementRoundStatus);
                closeActiveRound?.Invoke(ResolveSettlementCloseReason(
                    round.ResultSettlementCloseReason));
                return;
            }
            if (slotAction == WorldDiplomacySettlementSlotAction.SkipSlot)
            {
                SkipResultSettlementSlot(round, slot.SlotId, slot.KingdomId, "no_legal_action", storage?.DiplomaticThreats, currentDay, log);
                refreshActionSlots?.Invoke(round);
                continue;
            }

            if (slotAction == WorldDiplomacySettlementSlotAction.WaitForPlayer)
            {
                MarkSettlementSlotWaitingForPlayer(
                    round, slot, receiverId, storage?.PlayerOpportunities, storage?.Documents, currentDay);
                return;
            }

            ScheduleSettlementRelayArrival(
                round, slot, receiverId, storage, currentDay);
            return;
        }
    }
}
