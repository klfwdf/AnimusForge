using TaleWorlds.CampaignSystem;
namespace AnimusForge;
internal static class DiplomacyFactionSnapshot
{
    internal static string Id(IFaction faction)
    {
        if (faction == null) return null;
        IFaction normalized = faction;
        try { normalized = faction.MapFaction ?? faction; }
        catch { }
        return normalized.StringId;
    }
}
