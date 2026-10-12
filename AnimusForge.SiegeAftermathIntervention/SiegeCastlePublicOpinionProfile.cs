namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Short civilian/notable interpretation of the already-applied castle trust ledger.
/// </summary>
public static class SiegeCastlePublicOpinionProfile
{
    public static string Build(
        string playerName,
        int settlementTrustDelta,
        int villageTrustDelta,
        int notableRelationDelta,
        int notableTrustDelta)
    {
        return Build(playerName, settlementTrustDelta, villageTrustDelta, notableRelationDelta, notableTrustDelta, isEnglish: false);
    }

    public static string Build(
        string playerName,
        int settlementTrustDelta,
        int villageTrustDelta,
        int notableRelationDelta,
        int notableTrustDelta,
        bool isEnglish)
    {
        string player = string.IsNullOrWhiteSpace(playerName) ? (isEnglish ? "The player" : CastleAftermathCompatibilityTexts.Raw("player_fallback")) : playerName.Trim();
        int score = settlementTrustDelta + villageTrustDelta + notableRelationDelta + notableTrustDelta;
        if (isEnglish)
        {
            if (score >= 45)
            {
                return "Nearby villagers and notables praise " + player + " as restrained and just; trust has risen noticeably.";
            }
            if (score >= 15)
            {
                return "Nearby civilians generally approve of " + player + "\u0027s disposition; villagers\u0027 and notables\u0027 attitudes have warmed.";
            }
            if (score > -15)
            {
                return "Nearby civilians are still waiting and watching " + player + "; no clear praise or resentment has formed.";
            }
            if (score > -45)
            {
                return "Nearby villagers and notables are displeased with " + player + "\u0027s treatment of captives; trust is declining.";
            }
            return "Nearby civilians fear and resent " + player + " for harsh dispositions; villages and notables strongly object.";
        }

        if (score >= 45)
        {
            return CastleAftermathCompatibilityTexts.Raw("opinion_tier_45").Replace("{player}", player);
        }
        if (score >= 15)
        {
            return CastleAftermathCompatibilityTexts.Raw("opinion_tier_15").Replace("{player}", player);
        }
        if (score > -15)
        {
            return CastleAftermathCompatibilityTexts.Raw("opinion_tier_neutral").Replace("{player}", player);
        }
        if (score > -45)
        {
            return CastleAftermathCompatibilityTexts.Raw("opinion_tier_neg15").Replace("{player}", player);
        }
        return CastleAftermathCompatibilityTexts.Raw("opinion_tier_neg45").Replace("{player}", player);
    }
}
