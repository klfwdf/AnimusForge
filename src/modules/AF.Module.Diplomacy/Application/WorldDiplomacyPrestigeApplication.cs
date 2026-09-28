using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal sealed class WorldDiplomacyPrestigeCourt
{
    internal string KingdomId { get; }
    internal bool Eliminated { get; }
    internal string RulerId { get; }
    internal IReadOnlyList<string> VassalLeaderIds { get; }
    internal WorldDiplomacyPrestigeCourt(string kingdomId, bool eliminated, string rulerId, IReadOnlyList<string> vassals)
    { KingdomId = kingdomId; Eliminated = eliminated; RulerId = rulerId; VassalLeaderIds = vassals; }
}

internal interface IWorldDiplomacyPrestigePort
{
    bool CampaignAvailable { get; }
    IEnumerable<string> KingdomIds(bool activeOnly);
    WorldDiplomacyPrestigeCourt CaptureCourt(string kingdomId);
    bool HasHero(string heroId);
    int ChangeRelationAndMeasure(string firstHeroId, string secondHeroId, int difference);
    void ChangeRelation(string firstHeroId, string secondHeroId, int difference);
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
        WorldDiplomacyStorage canonical = storage;
        return WorldDiplomacyReputationRules.ApplyNationalPrestigeDelta(storage.NationalPrestigeByKingdom,
            normalizedId, delta, document, reason, port.KingdomName, id => Reconcile(canonical, port, id));
    }

    internal static void SettleDocument(ref WorldDiplomacyStorage storage, IWorldDiplomacyPrestigePort port,
        WorldDiplomacyDocument document)
    {
        storage ??= new WorldDiplomacyStorage();
        storage.InternationalReputationByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        WorldDiplomacyReputationRules.SettleInternationalReputationForDocument(
            storage.InternationalReputationByKingdom, document, port.KingdomName, port.Log);
    }

    internal static void RecoverDocuments(ref WorldDiplomacyStorage storage, IWorldDiplomacyPrestigePort port)
    {
        WorldDiplomacyStorage canonical = storage;
        WorldDiplomacyReputationRules.RecoverUnsettledAiInternationalReputation(storage?.Documents,
            document => SettleDocument(ref canonical, port, document), port.Log);
        storage = canonical;
    }

    internal static void NaturalChange(WorldDiplomacyStorage storage, IWorldDiplomacyPrestigePort port, bool anchorOnly)
    {
        if (storage == null || !port.CampaignAvailable) return;
        if (!anchorOnly) storage.InternationalReputationByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        storage.InternationalReputationNaturalChangeLastDayByKingdom ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (anchorOnly)
            WorldDiplomacyReputationRules.AnchorInternationalReputationNaturalChangeDays(
                storage.InternationalReputationNaturalChangeLastDayByKingdom, port.KingdomIds(true), port.CurrentDay());
        else
            WorldDiplomacyReputationRules.ProcessInternationalReputationNaturalChange(storage.InternationalReputationByKingdom,
                storage.InternationalReputationNaturalChangeLastDayByKingdom, port.KingdomIds(true), port.CurrentDay(), port.Log);
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
            foreach (string vassal in court.VassalLeaderIds)
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
            if (stale.AppliedAmount == 0 || !hasRuler || !hasVassal) storage.NationalPrestigeRelationModifiers.Remove(stale);
        }
    }

    private static void ApplyDifference(WorldDiplomacyPrestigeRelationModifier modifier, IWorldDiplomacyPrestigePort port,
        string vassal, string ruler, int desired)
    {
        if (modifier == null || vassal == null || ruler == null) return;
        int difference = desired - modifier.AppliedAmount;
        if (difference == 0) return;
        modifier.AppliedAmount += port.ChangeRelationAndMeasure(vassal, ruler, difference);
    }

    internal static void ApplyZeroPrestigePenalty(IWorldDiplomacyPrestigePort port, string kingdomId, int amount)
    {
        if (amount >= 0) return;
        WorldDiplomacyPrestigeCourt court = port.CaptureCourt(kingdomId);
        if (court?.RulerId == null) return;
        foreach (string vassal in court.VassalLeaderIds) port.ChangeRelation(vassal, court.RulerId, amount);
    }
}
