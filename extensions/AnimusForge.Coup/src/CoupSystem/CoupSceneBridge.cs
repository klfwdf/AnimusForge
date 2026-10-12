using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Localization;

namespace AnimusForge.CoupSystem;

internal static class CoupSceneBridge
{
    internal static bool TryValidateScene(Settlement settlement, out string reason)
    {
        reason = null;
        if (settlement?.IsTown != true || settlement.LocationComplex == null)
        {
            reason = new TextObject("{=AF_COUPX_NoTownScene}Current location has no usable town scene.").ToString();
            return false;
        }
        Location center = settlement.LocationComplex.GetLocationWithId("center");
        Location hall = settlement.LocationComplex.GetLocationWithId("lordshall");
        int level = settlement.Town.GetWallLevel();
        if (center == null || hall == null || string.IsNullOrWhiteSpace(center.GetSceneName(level))
            || string.IsNullOrWhiteSpace(hall.GetSceneName(level)))
        {
            reason = new TextObject("{=AF_COUPX_MissingStreetOrHall}This town lacks street or lord's hall scenes.").ToString();
            return false;
        }
        // Actual passage/navmesh availability is checked once the native scene has loaded.
        if (!center.LocationsOfPassages.Contains(hall))
        {
            reason = new TextObject("{=AF_COUPX_NoPassageToHall}Town street has no vanilla passage leading to lord's hall.").ToString();
            return false;
        }
        return true;
    }

}
