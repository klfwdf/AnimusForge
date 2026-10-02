using System;
using Persistence = AnimusForge.Refactor.Persistence;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
namespace AnimusForge;

// Application owns dependency invocation and canonical workflow ordering.
internal static class WorldDiplomacyOfferCooldownApplication
{
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
    bool normalizationRequired = false;
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
            WorldDiplomacyOfferCooldownApplication.UpsertOfferCooldown(cooldowns, cooldownByKey, decision.Key, round.CompletedDay, round.RoundId, () => normalizationRequired = true);
            started++;
        }
    }
    // Normalize once after the batch: normalization can replace the canonical list.
    if (normalizationRequired) normalizeStorage?.Invoke();
    if (started > 0 || cleared > 0)
    {
        log?.Invoke("trade/alliance proposal cooldowns settled round=" + round.RoundId
            + " started=" + started.ToString(CultureInfo.InvariantCulture)
            + " cleared=" + cleared.ToString(CultureInfo.InvariantCulture)
            + " recordFailures=" + recordFailures);
    }

        }
}
