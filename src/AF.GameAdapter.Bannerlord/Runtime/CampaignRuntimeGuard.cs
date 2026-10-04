using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal static class CampaignRuntimeGuard
{
    internal static bool IsLiveCampaign(Campaign owner)
        => owner != null && ReferenceEquals(owner, Campaign.Current) && owner.GameStarted;
}
