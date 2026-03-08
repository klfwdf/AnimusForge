using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200019A RID: 410
	public abstract class BribeCalculationModel : MBGameModel<BribeCalculationModel>
	{
		// Token: 0x06001C3C RID: 7228
		public abstract int GetBribeToEnterLordsHall(Settlement settlement);

		// Token: 0x06001C3D RID: 7229
		public abstract int GetBribeToEnterDungeon(Settlement settlement);

		// Token: 0x06001C3E RID: 7230
		public abstract bool IsBribeNotNeededToEnterKeep(Settlement settlement);

		// Token: 0x06001C3F RID: 7231
		public abstract bool IsBribeNotNeededToEnterDungeon(Settlement settlement);
	}
}
