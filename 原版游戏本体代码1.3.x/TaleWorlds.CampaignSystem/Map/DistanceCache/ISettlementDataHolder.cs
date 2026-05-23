using System;

namespace TaleWorlds.CampaignSystem.Map.DistanceCache
{
	// Token: 0x02000223 RID: 547
	public interface ISettlementDataHolder
	{
		// Token: 0x17000806 RID: 2054
		// (get) Token: 0x060020BB RID: 8379
		CampaignVec2 GatePosition { get; }

		// Token: 0x17000807 RID: 2055
		// (get) Token: 0x060020BC RID: 8380
		CampaignVec2 PortPosition { get; }

		// Token: 0x17000808 RID: 2056
		// (get) Token: 0x060020BD RID: 8381
		string StringId { get; }

		// Token: 0x17000809 RID: 2057
		// (get) Token: 0x060020BE RID: 8382
		bool IsFortification { get; }

		// Token: 0x1700080A RID: 2058
		// (get) Token: 0x060020BF RID: 8383
		bool HasPort { get; }
	}
}
