using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Encounters
{
	// Token: 0x020002ED RID: 749
	public class VillageEncounter : LocationEncounter
	{
		// Token: 0x06002976 RID: 10614 RVA: 0x000AC2AA File Offset: 0x000AA4AA
		public VillageEncounter(Settlement settlement)
			: base(settlement)
		{
		}

		// Token: 0x06002977 RID: 10615 RVA: 0x000AC2B4 File Offset: 0x000AA4B4
		public override IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
		{
			IMission result = null;
			if (nextLocation.StringId == "village_center")
			{
				result = CampaignMission.OpenVillageMission(nextLocation.GetSceneName(1), nextLocation, talkToChar);
			}
			return result;
		}
	}
}
