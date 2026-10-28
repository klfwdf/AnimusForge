using System;
using AnimusForge.Refactor.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyThreatMechanicsHost
{
    IWorldDiplomacyPrestigePort Prestige();
    IWorldDiplomacyThreatBindingPort ThreatBinding();
}

internal sealed class WorldDiplomacyPrestigeCourt
{
    internal string KingdomId { get; }
    internal bool Eliminated { get; }
    internal string RulerId { get; }
    internal IReadOnlyList<WorldDiplomacyClanSnapshot> Clans { get; }
    internal WorldDiplomacyPrestigeCourt(string kingdomId, bool eliminated, string rulerId, IReadOnlyList<WorldDiplomacyClanSnapshot> clans)
    { KingdomId = kingdomId; Eliminated = eliminated; RulerId = rulerId; Clans = clans; }
}

internal interface IWorldDiplomacyPrestigePort
{
    bool CampaignAvailable { get; }
    IEnumerable<string> KingdomIds(bool activeOnly);
    WorldDiplomacyPrestigeCourt CaptureCourt(string kingdomId);
    bool HasHero(string heroId);
    bool TryReadRelation(string firstHeroId, string secondHeroId, out int value);
    WorldDiplomacyRelationEffectReceipt ChangeRelationAndMeasure(string firstHeroId, string secondHeroId, int difference);
    string KingdomName(string kingdomId);
    int CurrentDay();
    void Log(string message);
}

// Canonical prestige changes, relation-modifier recovery and daily drift share this owner.
internal static class WorldDiplomacyPrestigeApplication
{
    internal static int Apply(ref WorldDiplomacyStorage storage, IWorldDiplomacyPrestigePort port,
        string kingdomId, int delta, WorldDiplomacyDocument document, string reason)
    {
        string normalizedId = (kingdomId ?? "").Trim();
        if (normalizedId.Length == 0) return WorldDiplomacyReputationRules.DefaultNationalPrestige;
        storage ??= new WorldDiplomacyStorage();
        storage.NationalPrestigeByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int updated = WorldDiplomacyReputationRules.ApplyNationalPrestigeDelta(storage.NationalPrestigeByKingdom,
            normalizedId, delta, document, reason, port.KingdomName);
        try { Reconcile(storage, port, normalizedId); }
        catch (Exception ex) { port.Log("prestige relation reconciliation deferred: " + ex.GetType().Name); }
        return updated;
    }

    internal static void ReconcileAll(WorldDiplomacyStorage storage, IWorldDiplomacyPrestigePort port)
    {
        if (!port.CampaignAvailable) return;
        foreach (string kingdomId in port.KingdomIds(false)) Reconcile(storage, port, kingdomId);
    }

    internal static void Reconcile(WorldDiplomacyStorage storage, IWorldDiplomacyPrestigePort port, string kingdomId)
    {
        if (string.IsNullOrWhiteSpace(kingdomId)) return;
        WorldDiplomacyPrestigeCourt court = port.CaptureCourt(kingdomId);
        if (court == null) return;
        storage.NationalPrestigeRelationModifiers ??= new List<WorldDiplomacyPrestigeRelationModifier>();
        string ruler = court.RulerId;
        int desired = court.Eliminated || ruler == null ? 0 : WorldDiplomacyReputationRules.GetNationalPrestigeRelationTarget(
            WorldDiplomacyReputationRules.GetNationalPrestige(storage.NationalPrestigeByKingdom, court.KingdomId));
        var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ruler != null)
            foreach (string vassal in WorldDiplomacyWorldProfileRules.SelectPrestigeLeaders(court.Clans))
            {
                string key = court.KingdomId + "|" + ruler + "|" + vassal;
                activeKeys.Add(key);
                WorldDiplomacyPrestigeRelationModifier modifier = storage.NationalPrestigeRelationModifiers.FirstOrDefault(x => x != null
                    && string.Equals(x.KingdomId, court.KingdomId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.RulerHeroId, ruler, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.VassalLeaderHeroId, vassal, StringComparison.OrdinalIgnoreCase));
                if (modifier == null)
                {
                    modifier = new WorldDiplomacyPrestigeRelationModifier { KingdomId = court.KingdomId, RulerHeroId = ruler, VassalLeaderHeroId = vassal };
                    storage.NationalPrestigeRelationModifiers.Add(modifier);
                }
                ApplyDifference(modifier, port, vassal, ruler, desired);
            }
        foreach (WorldDiplomacyPrestigeRelationModifier stale in storage.NationalPrestigeRelationModifiers
            .Where(x => x != null && string.Equals(x.KingdomId, court.KingdomId, StringComparison.OrdinalIgnoreCase)
                && !activeKeys.Contains(x.KingdomId + "|" + x.RulerHeroId + "|" + x.VassalLeaderHeroId)).ToList())
        {
            bool hasRuler = port.HasHero(stale.RulerHeroId);
            bool hasVassal = port.HasHero(stale.VassalLeaderHeroId);
            if (hasRuler && hasVassal) ApplyDifference(stale, port, stale.VassalLeaderHeroId, stale.RulerHeroId, 0);
            if (stale.PendingEffect == null && (stale.AppliedAmount == 0 || !hasRuler || !hasVassal))
                storage.NationalPrestigeRelationModifiers.Remove(stale);
        }
    }

    private static void ApplyDifference(WorldDiplomacyPrestigeRelationModifier modifier, IWorldDiplomacyPrestigePort port,
        string vassal, string ruler, int desired)
    {
        if (modifier == null || vassal == null || ruler == null) return;
        if (modifier.PendingEffect != null)
        {
            if (!port.TryReadRelation(vassal, ruler, out int current)) return;
            WorldDiplomacyPendingRelationEffect pending = modifier.PendingEffect;
            if (current == pending.ExpectedAfter)
                modifier.AppliedAmount += pending.ExpectedAfter - pending.Before;
            else if (current != pending.Before)
            {
                // An unrelated later relation change makes attribution ambiguous.
                // Retain the receipt and do not replay an unconfirmed effect.
                port.Log("prestige relation pending outcome remains ambiguous: " + vassal + "|" + ruler);
                return;
            }
            modifier.PendingEffect = null;
        }
        int difference = desired - modifier.AppliedAmount;
        if (difference == 0) return;
        if (!port.TryReadRelation(vassal, ruler, out int before)) return;
        modifier.PendingEffect = new WorldDiplomacyPendingRelationEffect
        { Before = before, ExpectedAfter = Math.Max(-100, Math.Min(100, before + difference)) };
        WorldDiplomacyRelationEffectReceipt receipt = port.ChangeRelationAndMeasure(vassal, ruler, difference);
        if (receipt.IsKnown)
        {
            modifier.AppliedAmount += receipt.AppliedDelta;
            modifier.PendingEffect = null;
        }
        if (!string.IsNullOrEmpty(receipt.Diagnostic)) port.Log("prestige relation receipt known=" + receipt.IsKnown + " delta=" + receipt.AppliedDelta + " " + receipt.Diagnostic);
    }

    internal static void ApplyZeroPrestigePenalty(IWorldDiplomacyPrestigePort port, string kingdomId, int amount)
    {
        if (amount >= 0) return;
        WorldDiplomacyPrestigeCourt court = port.CaptureCourt(kingdomId);
        if (court?.RulerId == null) return;
        int confirmed = 0, unknown = 0;
        List<string> vassals = WorldDiplomacyWorldProfileRules.SelectPrestigeLeaders(court.Clans);
        foreach (string vassal in vassals)
        {
            WorldDiplomacyRelationEffectReceipt receipt = port.ChangeRelationAndMeasure(vassal, court.RulerId, amount);
            if (!receipt.IsKnown) unknown++;
            else if (receipt.AppliedDelta != 0) confirmed++;
            if (!string.IsNullOrEmpty(receipt.Diagnostic)) port.Log("zero-prestige relation receipt: " + receipt.Diagnostic);
        }
        port.Log("zero-prestige penalty confirmed=" + confirmed + " unknown=" + unknown + " attempted=" + vassals.Count);
    }
}
