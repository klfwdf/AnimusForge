using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Encounters
{
	// Token: 0x020002EC RID: 748
	public class TownEncounter : LocationEncounter
	{
		// Token: 0x06002974 RID: 10612 RVA: 0x000AC1E9 File Offset: 0x000AA3E9
		public TownEncounter(Settlement settlement)
			: base(settlement)
		{
		}

		// Token: 0x06002975 RID: 10613 RVA: 0x000AC1F4 File Offset: 0x000AA3F4
		public override IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
		{
			int num = base.Settlement.Town.GetWallLevel();
			string sceneName = nextLocation.GetSceneName(num);
			IMission result;
			if (nextLocation.StringId == "center")
			{
				if (Campaign.Current.IsMainHeroDisguised)
				{
					string civilianUpgradeLevelTag = Campaign.Current.Models.LocationModel.GetCivilianUpgradeLevelTag(num);
					result = CampaignMission.OpenDisguiseMission(sceneName, false, civilianUpgradeLevelTag, previousLocation);
				}
				else
				{
					result = CampaignMission.OpenTownCenterMission(sceneName, nextLocation, talkToChar, num, playerSpecialSpawnTag);
				}
			}
			else if (nextLocation.StringId == "arena")
			{
				result = CampaignMission.OpenArenaStartMission(sceneName, nextLocation, talkToChar);
			}
			else
			{
				num = Campaign.Current.Models.LocationModel.GetSettlementUpgradeLevel(PlayerEncounter.LocationEncounter);
				result = CampaignMission.OpenIndoorMission(sceneName, num, nextLocation, talkToChar);
			}
			return result;
		}
	}
}
