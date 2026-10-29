using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal static class DiplomacyNativeAdmissionRules
{
    internal static float AdjustPeaceScore(float score, int tendency)
    {
        if (float.IsNaN(score) || float.IsInfinity(score) || Math.Abs(score) > 1000000f) return score;
        return score + (50 - Math.Max(0, Math.Min(100, tendency))) * 10f;
    }

    internal static bool Occupies(WorldDiplomacyRound round, string kingdom) => round?.State == "active"
        && !string.IsNullOrEmpty(kingdom) && (round.Participants?.Any(x => x.KingdomId == kingdom && x.State == "active") == true
            || (round.InitiatorKingdomId == kingdom && round.Participants?.Any(x => x.KingdomId == kingdom && x.State is "withdrawn" or "eliminated") != true));

    internal static bool AllowsPeace(bool enabled, bool authorized, bool firstPlayerRuler, bool secondPlayerRuler,
        IEnumerable<WorldDiplomacyRound> live, string first, string second)
        => !enabled || authorized || (!firstPlayerRuler && !secondPlayerRuler
            && !(live?.Any(x => Occupies(x, first) || Occupies(x, second)) ?? false));
}
