using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001F9 RID: 505
	public abstract class VillageTradeModel : MBGameModel<VillageTradeModel>
	{
		// Token: 0x06001F33 RID: 7987
		public abstract float TradeBoundDistanceLimitAsDays(MobileParty.NavigationType navigationType);

		// Token: 0x06001F34 RID: 7988
		public abstract Settlement GetTradeBoundToAssignForVillage(Village village);
	}
}
