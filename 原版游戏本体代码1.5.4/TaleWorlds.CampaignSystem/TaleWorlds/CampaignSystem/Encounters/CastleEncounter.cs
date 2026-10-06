using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Encounters;

public class CastleEncounter : LocationEncounter
{
	public CastleEncounter(Settlement settlement)
		: base(settlement)
	{
	}

	public override IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
	{
		int wallLevel = base.Settlement.Town.GetWallLevel();
		if (nextLocation.StringId == "center")
		{
			return CampaignMission.OpenCastleCourtyardMission(nextLocation.GetSceneName(wallLevel), nextLocation, talkToChar, wallLevel);
		}
		if (nextLocation.StringId == "lordshall")
		{
			nextLocation.GetSceneName(wallLevel);
			return CampaignMission.OpenIndoorMission(nextLocation.GetSceneName(wallLevel), wallLevel, nextLocation, talkToChar);
		}
		wallLevel = Campaign.Current.Models.LocationModel.GetSettlementUpgradeLevel(PlayerEncounter.LocationEncounter);
		nextLocation.GetSceneName(wallLevel);
		return CampaignMission.OpenIndoorMission(nextLocation.GetSceneName(wallLevel), wallLevel, nextLocation, talkToChar);
	}
}
