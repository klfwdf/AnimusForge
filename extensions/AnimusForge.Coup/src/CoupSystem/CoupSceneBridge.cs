using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace AnimusForge.CoupSystem;

internal static class CoupSceneBridge
{
    internal static bool TryValidateScene(Settlement settlement, out string reason)
    {
        reason = null;
        if (settlement?.IsTown != true || settlement.LocationComplex == null)
        {
            reason = "当前地点没有可用的城镇场景。";
            return false;
        }
        Location center = settlement.LocationComplex.GetLocationWithId("center");
        Location hall = settlement.LocationComplex.GetLocationWithId("lordshall");
        int level = settlement.Town.GetWallLevel();
        if (center == null || hall == null || string.IsNullOrWhiteSpace(center.GetSceneName(level))
            || string.IsNullOrWhiteSpace(hall.GetSceneName(level)))
        {
            reason = "这座城镇缺少街道或领主大厅场景。";
            return false;
        }
        // Actual passage/navmesh availability is checked once the native scene has loaded.
        if (!center.LocationsOfPassages.Contains(hall))
        {
            reason = "城镇街道没有通向领主大厅的原版通道。";
            return false;
        }
        return true;
    }

}
