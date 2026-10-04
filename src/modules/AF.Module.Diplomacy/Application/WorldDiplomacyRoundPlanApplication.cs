using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Application owns plan admission, route order and the next scheduled turn.
internal static class WorldDiplomacyRoundPlanApplication
{
        internal static void Commit(
            WorldDiplomacyJob job,
            string raw,
            WorldDiplomacyStorage storage,
            int relaySchemaVersion,
            int participantLimit,
            Func<string, WorldDiplomacyRound> resolveRound,
            Func<string, WorldDiplomacyDocument> resolveDocument,
            Func<string, string> resolveKingdomId,
            Func<string, bool> isKingdomEliminated,
            Func<string, bool> hasIndependentAuthority,
            Func<string, string> resolveRepresentativeKingdomId,
            Func<string, bool> isPlayerKingdom,
            Func<string, string, bool> isAtWar,
            Func<string, string, float> courtDistance,
            Func<int> currentDay,
            Action<string> closeActiveRound,
            Func<WorldDiplomacyRound, string, bool> tryIncludeResultSettlementTarget,
            Func<string, string> createId,
            Action<WorldDiplomacyRound> refreshResultSettlementActionSlots,
            Action<WorldDiplomacyRound> scheduleResultSettlement,
            Action<WorldDiplomacyRound> scheduleRelayHop,
            Action<string> log)
        {
            WorldDiplomacyRound round = resolveRound?.Invoke(job?.RoundId);
            WorldDiplomacyDocument root = resolveDocument?.Invoke(job?.DocumentId);
            string initiatorId = resolveKingdomId?.Invoke(root?.AuthorKingdomId ?? round?.InitiatorKingdomId);
            if (round == null || root == null || string.IsNullOrWhiteSpace(initiatorId) || round.RelayPlanned
                || !WorldDiplomacyLiveRoundRules.Contains(storage, round)
                || !WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State)) return;
            JObject json = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(raw);
            round.RoundTopic = WorldDiplomacyTextRules.Limit(WorldDiplomacyTextRules.SanitizePublicDiplomacyText(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "topic"), root.PlannedRoundTopic, root.Title, "外交交涉")), 120);
            round.TopicCategory = ((root.Actions?.Any(x => x != null && WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent) is "warning" or "ultimatum") == true)
                || WorldDiplomacyIntentVocabulary.NormalizeIntent(root.Intent) is "warning" or "ultimatum")
                ? "war_escalation"
                : WorldDiplomacyIntentVocabulary.InferTopicCategory(round.RoundTopic, !string.IsNullOrWhiteSpace(initiatorId) && !string.IsNullOrWhiteSpace(resolveKingdomId?.Invoke(root.TargetKingdomId)) && (isAtWar?.Invoke(initiatorId, root.TargetKingdomId) ?? false));
            List<string> selected = new List<string>();
            HashSet<string> selectedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> candidateSet = new HashSet<string>(job.CandidateKingdomIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (string id in WorldDiplomacyEnvelopeJsonRules.ReadStringList(json, "selected_kingdom_ids"))
            {
                if (candidateSet.Contains(id) && !string.IsNullOrWhiteSpace(resolveKingdomId?.Invoke(id))
                    && !(isKingdomEliminated?.Invoke(id) ?? false)
                    && (hasIndependentAuthority?.Invoke(id) ?? false) && selectedSet.Add(id)) selected.Add(id);
            }
            HashSet<string> mandatoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string explicitTargetId = resolveRepresentativeKingdomId?.Invoke(root.TargetKingdomId);
            if (!string.IsNullOrWhiteSpace(explicitTargetId) && !string.Equals(explicitTargetId, initiatorId, StringComparison.OrdinalIgnoreCase)) mandatoryIds.Add(explicitTargetId);
            foreach (string id in WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(storage.Documents
                .Where(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId))
                .SelectMany(x => x.AddressedKingdomIds ?? new List<string>())))
            {
                string mandatoryId = resolveRepresentativeKingdomId?.Invoke(id);
                if (!string.IsNullOrWhiteSpace(mandatoryId) && !string.Equals(mandatoryId, initiatorId, StringComparison.OrdinalIgnoreCase))
                {
                    mandatoryIds.Add(mandatoryId);
                    if (selectedSet.Add(mandatoryId)) selected.Add(mandatoryId);
                }
            }
            string primaryTargetId = resolveKingdomId?.Invoke(root.TargetKingdomId);
            List<string> mandatoryRoute = mandatoryIds.Select(id => resolveKingdomId?.Invoke(id))
                .Where(id => id != null && !string.Equals(id, initiatorId, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(id => string.Equals(id, primaryTargetId, StringComparison.OrdinalIgnoreCase))
                .ThenBy(id => courtDistance?.Invoke(initiatorId, id) ?? float.MaxValue)
                .Take(Math.Max(0, participantLimit - 1))
                .ToList();
            int optionalSlots = Math.Max(0, participantLimit - 1 - mandatoryRoute.Count);
            List<string> optionalRoute = selected.Where(id => !mandatoryIds.Contains(id)).Select(id => resolveKingdomId?.Invoke(id))
                .Where(id => id != null && !string.Equals(id, initiatorId, StringComparison.OrdinalIgnoreCase)
                    && !(isKingdomEliminated?.Invoke(id) ?? false) && (hasIndependentAuthority?.Invoke(id) ?? false))
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(optionalSlots).ToList();
            List<string> remaining = mandatoryRoute.Concat(optionalRoute).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            List<string> route = new List<string> { initiatorId };
            string cursor = initiatorId;
            while (remaining.Count > 0)
            {
                string next = remaining.OrderBy(id => courtDistance?.Invoke(cursor, id) ?? float.MaxValue).ThenBy(id => id, StringComparer.OrdinalIgnoreCase).First();
                route.Add(next);
                remaining.Remove(next);
                cursor = next;
            }
            if (route.Count < 2)
            {
                if (WorldDiplomacyLiveRoundRules.Contains(storage, round)) closeActiveRound?.Invoke("round_plan_no_participants");
                return;
            }
            round.SchemaVersion = relaySchemaVersion;
            round.RelayPlanned = true;
            round.RelayRouteKingdomIds = route;
            round.RelayCursor = 0;
            round.RelayDirection = 1;
            round.RelayPassNumber = 1;
            round.RelayPassStartedDay = currentDay();
            round.ActionAttemptCountAtPassStart = round.DiplomaticActionAttemptCount;
            round.ConsecutiveNoActionPasses = 0;
            round.LastAccountedRelayPassNumber = 0;
            round.RelayWaiting = false;
            foreach (string id in route)
            {
                WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, id, "active", mandatoryReply: false);
                participant.SelectedForRelay = true;
                participant.IsPlayerAsync = isPlayerKingdom?.Invoke(id) ?? false;
            }
            round.CachePrefix = "";
            log("relay round planned round=" + round.RoundId + " route=" + string.Join(">", route)
                + " participantLimit=" + participantLimit.ToString(CultureInfo.InvariantCulture)
                + " passDays=" + round.RelayPassDurationDays.ToString(CultureInfo.InvariantCulture)
                + " targetDays=" + Math.Max(1, round.SoftEndDay - round.StartedDay).ToString(CultureInfo.InvariantCulture));
            if (round.ResultSettlementPending)
            {
                WorldDiplomacyResultSlotApplication.InitializeResultSettlementRouteSlots(round, storage?.Documents, tryIncludeResultSettlementTarget, createId);
                refreshResultSettlementActionSlots?.Invoke(round);
                scheduleResultSettlement?.Invoke(round);
            }
            else scheduleRelayHop?.Invoke(round);
        }

}
