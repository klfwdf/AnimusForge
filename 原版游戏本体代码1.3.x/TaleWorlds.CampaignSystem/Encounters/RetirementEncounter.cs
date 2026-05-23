using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Encounters
{
	// Token: 0x020002EB RID: 747
	public class RetirementEncounter : LocationEncounter
	{
		// Token: 0x06002972 RID: 10610 RVA: 0x000AC18A File Offset: 0x000AA38A
		public RetirementEncounter(Settlement settlement)
			: base(settlement)
		{
		}

		// Token: 0x06002973 RID: 10611 RVA: 0x000AC194 File Offset: 0x000AA394
		public override IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
		{
			IMission result = null;
			if (Settlement.CurrentSettlement.SettlementComponent is RetirementSettlementComponent)
			{
				int upgradeLevel = (Settlement.CurrentSettlement.IsTown ? Settlement.CurrentSettlement.Town.GetWallLevel() : 1);
				result = CampaignMission.OpenRetirementMission(nextLocation.GetSceneName(upgradeLevel), nextLocation, null, null, "retirement_after_player_knockedout");
			}
			return result;
		}

		// Token: 0x04000BF9 RID: 3065
		private const string UnconsciousGameMenuID = "retirement_after_player_knockedout";
	}
}
