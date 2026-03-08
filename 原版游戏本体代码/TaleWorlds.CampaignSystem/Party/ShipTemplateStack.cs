using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Party
{
	// Token: 0x02000300 RID: 768
	public struct ShipTemplateStack
	{
		// Token: 0x06002CAF RID: 11439 RVA: 0x000BBDF5 File Offset: 0x000B9FF5
		public ShipTemplateStack(ShipHull shipHull, int minValue, int maxValue)
		{
			this.ShipHull = shipHull;
			this.MinValue = minValue;
			this.MaxValue = maxValue;
		}

		// Token: 0x04000D0C RID: 3340
		public ShipHull ShipHull;

		// Token: 0x04000D0D RID: 3341
		public int MinValue;

		// Token: 0x04000D0E RID: 3342
		public int MaxValue;
	}
}
