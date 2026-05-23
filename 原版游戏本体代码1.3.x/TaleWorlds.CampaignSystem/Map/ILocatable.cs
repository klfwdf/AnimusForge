using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map
{
	// Token: 0x0200021D RID: 541
	internal interface ILocatable<T>
	{
		// Token: 0x17000800 RID: 2048
		// (get) Token: 0x0600207A RID: 8314
		// (set) Token: 0x0600207B RID: 8315
		[CachedData]
		int LocatorNodeIndex { get; set; }

		// Token: 0x17000801 RID: 2049
		// (get) Token: 0x0600207C RID: 8316
		// (set) Token: 0x0600207D RID: 8317
		[CachedData]
		T NextLocatable { get; set; }

		// Token: 0x17000802 RID: 2050
		// (get) Token: 0x0600207E RID: 8318
		[CachedData]
		Vec2 GetPosition2D { get; }
	}
}
