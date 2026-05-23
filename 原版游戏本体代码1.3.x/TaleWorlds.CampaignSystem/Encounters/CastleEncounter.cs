using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Encounters
{
	// Token: 0x020002E7 RID: 743
	public class CastleEncounter : LocationEncounter
	{
		// Token: 0x06002961 RID: 10593 RVA: 0x000ABED5 File Offset: 0x000AA0D5
		public CastleEncounter(Settlement settlement)
			: base(settlement)
		{
		}

		// Token: 0x06002962 RID: 10594 RVA: 0x000ABEE0 File Offset: 0x000AA0E0
		public override IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
		{
			int num = base.Settlement.Town.GetWallLevel();
			IMission result;
			if (nextLocation.StringId == "center")
			{
				result = CampaignMission.OpenCastleCourtyardMission(nextLocation.GetSceneName(num), nextLocation, talkToChar, num);
			}
			else if (nextLocation.StringId == "lordshall")
			{
				nextLocation.GetSceneName(num);
				result = CampaignMission.OpenIndoorMission(nextLocation.GetSceneName(num), num, nextLocation, talkToChar);
			}
			else
			{
				num = Campaign.Current.Models.LocationModel.GetSettlementUpgradeLevel(PlayerEncounter.LocationEncounter);
				nextLocation.GetSceneName(num);
				result = CampaignMission.OpenIndoorMission(nextLocation.GetSceneName(num), num, nextLocation, talkToChar);
			}
			return result;
		}
	}
}
