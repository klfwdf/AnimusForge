using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001CB RID: 459
	public abstract class SettlementGarrisonModel : MBGameModel<SettlementGarrisonModel>
	{
		// Token: 0x06001DF6 RID: 7670
		public abstract int GetMaximumDailyAutoRecruitmentCount(Town town);

		// Token: 0x06001DF7 RID: 7671
		public abstract ExplainedNumber CalculateBaseGarrisonChange(Settlement settlement, bool includeDescriptions = false);

		// Token: 0x06001DF8 RID: 7672
		public abstract int FindNumberOfTroopsToTakeFromGarrison(MobileParty mobileParty, Settlement settlement, float idealGarrisonStrengthPerWalledCenter = 0f);

		// Token: 0x06001DF9 RID: 7673
		public abstract int FindNumberOfTroopsToLeaveToGarrison(MobileParty mobileParty, Settlement settlement);

		// Token: 0x06001DFA RID: 7674
		public abstract float GetMaximumDailyRepairAmount(Settlement settlement);
	}
}
