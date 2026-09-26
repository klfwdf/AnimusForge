using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Resolve against the current campaign index on each synchronous call. Never retain live targets.
internal static class DiplomacyIdentityResolver
{
    internal static Hero Hero(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        try { return Campaign.Current?.CampaignObjectManager?.Find<Hero>(id); }
        catch { return null; }
    }
    internal static Kingdom Kingdom(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        try { return Campaign.Current?.CampaignObjectManager?.Find<Kingdom>(id); }
        catch { return null; }
    }
}
