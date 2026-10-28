using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Encounters;

public class TownEncounter : LocationEncounter
{
	public TownEncounter(Settlement settlement)
		: base(settlement)
	{
	}

	public override IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
	{
		int wallLevel = base.Settlement.Town.GetWallLevel();
		string sceneName = nextLocation.GetSceneName(wallLevel);
		if (nextLocation.StringId == "center")
		{
			if (Campaign.Current.IsMainHeroDisguised)
			{
				string civilianUpgradeLevelTag = Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(wallLevel);
				return CampaignMission.OpenDisguiseMission(sceneName, willSetUpContact: false, civilianUpgradeLevelTag, previousLocation);
			}
			return CampaignMission.OpenTownCenterMission(sceneName, nextLocation, talkToChar, wallLevel, playerSpecialSpawnTag);
		}
		if (nextLocation.StringId == "arena")
		{
			return CampaignMission.OpenArenaStartMission(sceneName, nextLocation, talkToChar);
		}
		wallLevel = Campaign.Current.Models.LocationModel.GetSettlementUpgradeLevel(PlayerEncounter.LocationEncounter);
		return CampaignMission.OpenIndoorMission(sceneName, wallLevel, nextLocation, talkToChar);
	}
}
