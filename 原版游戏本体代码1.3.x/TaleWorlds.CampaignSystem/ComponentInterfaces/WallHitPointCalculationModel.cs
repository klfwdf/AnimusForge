using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D7 RID: 471
	public abstract class WallHitPointCalculationModel : MBGameModel<WallHitPointCalculationModel>
	{
		// Token: 0x06001E42 RID: 7746
		public abstract float CalculateMaximumWallHitPoint(Town town);
	}
}
