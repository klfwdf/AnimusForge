using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200009A RID: 154
	public interface ITrackableCampaignObject : ITrackableBase
	{
		// Token: 0x060012C2 RID: 4802
		Banner GetBanner();

		// Token: 0x17000516 RID: 1302
		// (get) Token: 0x060012C3 RID: 4803
		bool IsReady { get; }
	}
}
