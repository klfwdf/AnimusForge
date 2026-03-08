using System;
using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200040E RID: 1038
	public interface ITradeRumorCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x17000E1A RID: 3610
		// (get) Token: 0x060040C6 RID: 16582
		IEnumerable<TradeRumor> TradeRumors { get; }
	}
}
