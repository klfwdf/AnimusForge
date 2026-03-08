using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001CA RID: 458
	public abstract class SettlementProsperityModel : MBGameModel<SettlementProsperityModel>
	{
		// Token: 0x06001DF3 RID: 7667
		public abstract ExplainedNumber CalculateProsperityChange(Town fortification, bool includeDescriptions = false);

		// Token: 0x06001DF4 RID: 7668
		public abstract ExplainedNumber CalculateHearthChange(Village village, bool includeDescriptions = false);
	}
}
