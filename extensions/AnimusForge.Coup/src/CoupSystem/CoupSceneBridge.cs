using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.MountAndBlade;

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

    internal static bool TryOpenStage(CoupSession session, out string reason)
    {
        reason = null;
        try
        {
            Settlement settlement = Settlement.Find(session?.SettlementId);
            if (session == null || !session.IsCombatPhase || Mission.Current != null)
            {
                reason = "当前尚不能进入政变战斗。";
                return false;
            }
            if (!TryValidateScene(settlement, out reason)) return false;
            if (settlement.IsUnderSiege || PlayerEncounterCompat.HasEncounterBattleContext()
                || PlayerEncounter.LocationEncounter?.Settlement != settlement)
            {
                reason = "城镇遭遇已变化，无法开启政变场景。";
                return false;
            }
            string locationId = session.Phase == CoupPhase.Hall ? "lordshall" : "center";
            Location location = settlement.LocationComplex.GetLocationWithId(locationId);
            // Clearing these prevents native passage/menu flow from reopening an obsolete location.
            Campaign.Current.GameMenuManager.NextLocation = null;
            Campaign.Current.GameMenuManager.PreviousLocation = session.Phase == CoupPhase.Hall
                ? settlement.LocationComplex.GetLocationWithId("center") : null;
            var mission = PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location,
                Campaign.Current.GameMenuManager.PreviousLocation, null, null);
            if (mission != null) return true;
            reason = "原版场景入口未返回任务。";
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "OpenStage failed: " + ex);
            reason = "政变场景启动失败：" + ex.Message;
            return false;
        }
    }
}
