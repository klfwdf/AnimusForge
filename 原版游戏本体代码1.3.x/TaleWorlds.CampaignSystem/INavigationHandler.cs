using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000094 RID: 148
	public interface INavigationHandler
	{
		// Token: 0x1700050B RID: 1291
		// (get) Token: 0x0600129C RID: 4764
		// (set) Token: 0x0600129D RID: 4765
		bool IsNavigationLocked { get; set; }

		// Token: 0x0600129E RID: 4766
		INavigationElement[] GetElements();

		// Token: 0x0600129F RID: 4767
		INavigationElement GetElement(string id);

		// Token: 0x060012A0 RID: 4768
		bool IsAnyElementActive();
	}
}
