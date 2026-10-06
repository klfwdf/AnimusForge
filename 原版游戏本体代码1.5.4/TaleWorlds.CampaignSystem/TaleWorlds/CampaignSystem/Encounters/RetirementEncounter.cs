using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Encounters;

public class RetirementEncounter : LocationEncounter
{
	private const string UnconsciousGameMenuID = "retirement_after_player_knockedout";

	public RetirementEncounter(Settlement settlement)
		: base(settlement)
	{
	}

	public override IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
	{
		IMission result = null;
		if (Settlement.CurrentSettlement.SettlementComponent is RetirementSettlementComponent)
		{
			int upgradeLevel = ((!Settlement.CurrentSettlement.IsTown) ? 1 : Settlement.CurrentSettlement.Town.GetWallLevel());
			result = CampaignMission.OpenRetirementMission(nextLocation.GetSceneName(upgradeLevel), nextLocation, null, null, "retirement_after_player_knockedout");
		}
		return result;
	}
}
