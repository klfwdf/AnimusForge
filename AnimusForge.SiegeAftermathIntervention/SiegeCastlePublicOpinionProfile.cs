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
        string player = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
        int score = settlementTrustDelta + villageTrustDelta + notableRelationDelta + notableTrustDelta;
        if (score >= 45)
        {
            return "附近村民与要人盛赞 " + player + " 克制而公正，信任明显上升。";
        }
        if (score >= 15)
        {
            return "附近平民总体认可 " + player + " 的处置，村庄与要人的态度转暖。";
        }
        if (score > -15)
        {
            return "附近平民仍在观望 " + player + "，没有形成明显赞许或怨恨。";
        }
        if (score > -45)
        {
            return "附近村民与要人不满 " + player + " 对俘虏的做法，信任正在下降。";
        }
        return "附近平民因 " + player + " 的严酷处置而恐惧怨恨，村庄与要人强烈反感。";
    }
}
