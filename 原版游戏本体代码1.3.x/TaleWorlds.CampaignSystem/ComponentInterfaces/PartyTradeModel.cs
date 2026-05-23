using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000193 RID: 403
	public abstract class PartyTradeModel : MBGameModel<PartyTradeModel>
	{
		// Token: 0x17000706 RID: 1798
		// (get) Token: 0x06001C15 RID: 7189
		public abstract int CaravanTransactionHighestValueItemCount { get; }

		// Token: 0x06001C16 RID: 7190
		public abstract float GetTradePenaltyFactor(MobileParty party);
	}
}
