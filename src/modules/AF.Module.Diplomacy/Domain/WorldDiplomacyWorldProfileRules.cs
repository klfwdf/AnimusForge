using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.Refactor.Domain;

internal readonly struct WorldDiplomacyFortSnapshot
{
    internal readonly string Id, Name, KingdomId;
    internal readonly int KingdomIndex;
    internal readonly bool ActiveKingdom, IsFort, RulingOwned;
    internal readonly float Prosperity;
    internal WorldDiplomacyFortSnapshot(string id, string name, string kingdomId, int kingdomIndex,
        bool activeKingdom, bool isFort, bool rulingOwned, float prosperity)
    {
        Id = id; Name = name; KingdomId = kingdomId; KingdomIndex = kingdomIndex;
        ActiveKingdom = activeKingdom; IsFort = isFort; RulingOwned = rulingOwned; Prosperity = prosperity;
    }
}

internal readonly struct WorldDiplomacyClanSnapshot
{
    internal readonly string Id, LeaderId;
    internal readonly bool Exists, Belongs, Ruling, Eliminated, MercenaryService, MercenaryType, LeaderIsRuler;
    internal readonly int Tier, FiefCount;
    internal readonly float Influence;
    internal WorldDiplomacyClanSnapshot(string id, string leaderId, bool exists, bool belongs, bool ruling,
        bool eliminated, bool mercenaryService, bool mercenaryType, bool leaderIsRuler, int tier = 0, float influence = 0, int fiefCount = 0)
    {
        Id = id; LeaderId = leaderId; Exists = exists; Belongs = belongs; Ruling = ruling;
        Eliminated = eliminated; MercenaryService = mercenaryService; MercenaryType = mercenaryType;
        LeaderIsRuler = leaderIsRuler; Tier = tier; Influence = influence; FiefCount = fiefCount;
    }
}

internal static class WorldDiplomacyWorldProfileRules
{
    internal const float MinimumBorderDistance = 24f;
    private const float MaximumBorderDistance = 72f, BorderDistanceMedianMultiplier = 3.5f;
    private const int BorderForeignNeighborCount = 2;

    // Return original ordinals so the adapter captures native float distances only
    // for eligible unique forts, without implementing neighbor/threshold policy.
    internal static List<int> SelectBorderFortIndices(IReadOnlyList<WorldDiplomacyFortSnapshot> forts)
        => Enumerable.Range(0, forts.Count).Where(i => forts[i].ActiveKingdom && forts[i].IsFort)
            .GroupBy(i => forts[i].Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();

    internal static Dictionary<string, WorldDiplomacyBorderRelation> BuildBorders(
        IReadOnlyList<WorldDiplomacyFortSnapshot> forts, float[,] distances, out float threshold)
    {
        var result = new Dictionary<string, WorldDiplomacyBorderRelation>(StringComparer.OrdinalIgnoreCase);
        threshold = MinimumBorderDistance;
        if (forts.Count < 2) return result;
        var nearestDistances = new List<float>(forts.Count);
        for (int i = 0; i < forts.Count; i++)
        {
            float nearest = float.MaxValue;
            for (int j = 0; j < forts.Count; j++)
                if (i != j && distances[i, j] < nearest) nearest = distances[i, j];
            if (nearest < float.MaxValue) nearestDistances.Add(nearest);
        }
        nearestDistances.Sort();
        float median = nearestDistances.Count == 0 ? MinimumBorderDistance : nearestDistances[nearestDistances.Count / 2];
        threshold = Math.Max(MinimumBorderDistance, Math.Min(MaximumBorderDistance, median * BorderDistanceMedianMultiplier));
        for (int i = 0; i < forts.Count; i++)
        {
            WorldDiplomacyFortSnapshot first = forts[i];
            foreach (int j in Enumerable.Range(0, forts.Count).Where(j => forts[j].KingdomIndex != first.KingdomIndex)
                .OrderBy(j => distances[i, j]).Take(BorderForeignNeighborCount))
            {
                float distance = distances[i, j];
                if (distance > threshold) continue;
                WorldDiplomacyFortSnapshot second = forts[j];
                string key = WorldDiplomacyRoundLifecycleRules.PairKey(first.KingdomId, second.KingdomId);
                if (result.TryGetValue(key, out WorldDiplomacyBorderRelation existing) && existing.Distance <= distance) continue;
                result[key] = new WorldDiplomacyBorderRelation
                {
                    SharesBorder = true, FirstSettlementId = first.Id ?? "", FirstSettlementName = first.Name ?? "",
                    SecondSettlementId = second.Id ?? "", SecondSettlementName = second.Name ?? "", Distance = distance
                };
            }
        }
        return result;
    }

    internal static int SelectCourtIndex(IReadOnlyList<WorldDiplomacyFortSnapshot> forts)
        => Enumerable.Range(0, forts.Count).Where(i => forts[i].IsFort).OrderByDescending(i => forts[i].RulingOwned)
            .ThenByDescending(i => forts[i].Prosperity).ThenBy(i => forts[i].Id, StringComparer.OrdinalIgnoreCase)
            .DefaultIfEmpty(-1).First();

    internal static List<int> SelectRealmClanIndices(IReadOnlyList<WorldDiplomacyClanSnapshot> clans)
        => Enumerable.Range(0, clans.Count).Where(i => clans[i].Exists && !clans[i].Eliminated)
            .OrderByDescending(i => clans[i].Ruling).ThenByDescending(i => clans[i].Tier)
            .ThenByDescending(i => clans[i].Influence).Take(8).ToList();

    internal static WorldDiplomacyRealmRelationProfile BuildRealmProfile(
        IReadOnlyList<WorldDiplomacyClanSnapshot> source, IReadOnlyList<WorldDiplomacyClanSnapshot> target,
        int[,] relations, int rulerRelation)
    {
        double weightedSum = 0d, weightSum = 0d, positiveWeight = 0d, hostileWeight = 0d;
        var values = new List<(double Value, double Weight)>(source.Count * target.Count);
        for (int i = 0; i < source.Count; i++)
            for (int j = 0; j < target.Count; j++)
            {
                int relation = relations[i, j];
                double weight = Math.Sqrt(Math.Max(1d, 1d + source[i].Tier * 0.5d + source[i].FiefCount * 0.25d)
                    * Math.Max(1d, 1d + target[j].Tier * 0.5d + target[j].FiefCount * 0.25d));
                weightedSum += relation * weight; weightSum += weight;
                if (relation >= 10) positiveWeight += weight;
                if (relation <= -10) hostileWeight += weight;
                values.Add((relation, weight));
            }
        float average = weightSum <= 0d ? rulerRelation : (float)(weightedSum / weightSum);
        double variance = weightSum <= 0d ? 0d : values.Sum(x => x.Weight * Math.Pow(x.Value - average, 2d)) / weightSum;
        return new WorldDiplomacyRealmRelationProfile
        {
            AverageRelation = average, PositiveRatio = weightSum <= 0d ? 0f : (float)(positiveWeight / weightSum),
            HostileRatio = weightSum <= 0d ? 0f : (float)(hostileWeight / weightSum), Polarization = (float)Math.Sqrt(Math.Max(0d, variance)),
            RulerRelation = rulerRelation, SamplePairCount = values.Count, RulerEliteGap = rulerRelation - average
        };
    }

    private static bool IsVassal(WorldDiplomacyClanSnapshot clan)
        => clan.Exists && clan.Belongs && !clan.Ruling && !clan.Eliminated && !clan.MercenaryService && !clan.MercenaryType;
    internal static HashSet<string> SelectThreatClans(IEnumerable<WorldDiplomacyClanSnapshot> clans)
        => new HashSet<string>(clans.Where(c => IsVassal(c) && !string.IsNullOrWhiteSpace(c.Id)).Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
    internal static List<string> SelectPrestigeLeaders(IEnumerable<WorldDiplomacyClanSnapshot> clans)
        => clans.Where(c => IsVassal(c) && c.LeaderId != null && !c.LeaderIsRuler).Select(c => c.LeaderId).ToList();
}
