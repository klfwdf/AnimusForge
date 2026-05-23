using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map
{
	// Token: 0x0200021E RID: 542
	public struct LocatableSearchData<T>
	{
		// Token: 0x0600207F RID: 8319 RVA: 0x0008F214 File Offset: 0x0008D414
		public LocatableSearchData(Vec2 position, float radius, int minX, int minY, int maxX, int maxY)
		{
			this.Position = position;
			this.RadiusSquared = radius * radius;
			this.MinY = minY;
			this.MaxXInclusive = maxX;
			this.MaxYInclusive = maxY;
			this.CurrentX = minX;
			this.CurrentY = minY - 1;
			this.CurrentLocatable = null;
		}

		// Token: 0x04000977 RID: 2423
		public readonly Vec2 Position;

		// Token: 0x04000978 RID: 2424
		public readonly float RadiusSquared;

		// Token: 0x04000979 RID: 2425
		public readonly int MinY;

		// Token: 0x0400097A RID: 2426
		public readonly int MaxXInclusive;

		// Token: 0x0400097B RID: 2427
		public readonly int MaxYInclusive;

		// Token: 0x0400097C RID: 2428
		public int CurrentX;

		// Token: 0x0400097D RID: 2429
		public int CurrentY;

		// Token: 0x0400097E RID: 2430
		internal ILocatable<T> CurrentLocatable;
	}
}
