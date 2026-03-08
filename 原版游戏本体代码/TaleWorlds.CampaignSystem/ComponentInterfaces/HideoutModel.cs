using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B0 RID: 432
	public abstract class HideoutModel : MBGameModel<HideoutModel>
	{
		// Token: 0x17000736 RID: 1846
		// (get) Token: 0x06001D0E RID: 7438
		public abstract CampaignTime HideoutHiddenDuration { get; }

		// Token: 0x17000737 RID: 1847
		// (get) Token: 0x06001D0F RID: 7439
		public abstract int CanAttackHideoutStartTime { get; }

		// Token: 0x17000738 RID: 1848
		// (get) Token: 0x06001D10 RID: 7440
		public abstract int CanAttackHideoutEndTime { get; }

		// Token: 0x06001D11 RID: 7441
		public abstract float GetRogueryXpGainOnHideoutMissionEnd(bool isSucceeded);
	}
}
