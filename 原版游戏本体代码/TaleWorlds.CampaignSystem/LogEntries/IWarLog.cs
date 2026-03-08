using System;

namespace TaleWorlds.CampaignSystem.LogEntries
{
	// Token: 0x02000350 RID: 848
	public interface IWarLog
	{
		// Token: 0x060031AC RID: 12716
		bool IsRelatedToWar(StanceLink stance, out IFaction effector, out IFaction effected);
	}
}
