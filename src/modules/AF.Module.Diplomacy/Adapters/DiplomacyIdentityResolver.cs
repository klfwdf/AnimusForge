using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

// Resolve against each type's native owner on synchronous boundaries. Never retain live targets.
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
    internal static Settlement Settlement(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        // Settlements are MBObjectManager objects, unlike heroes and kingdoms.
        try { return Campaign.Current == null ? null : TaleWorlds.CampaignSystem.Settlements.Settlement.Find(id); }
        catch { return null; }
    }
}
