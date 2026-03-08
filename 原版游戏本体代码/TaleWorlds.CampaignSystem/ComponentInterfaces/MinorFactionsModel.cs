using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A3 RID: 419
	public abstract class MinorFactionsModel : MBGameModel<MinorFactionsModel>
	{
		// Token: 0x17000713 RID: 1811
		// (get) Token: 0x06001C7F RID: 7295
		public abstract float DailyMinorFactionHeroSpawnChance { get; }

		// Token: 0x17000714 RID: 1812
		// (get) Token: 0x06001C80 RID: 7296
		public abstract int MinorFactionHeroLimit { get; }

		// Token: 0x06001C81 RID: 7297
		public abstract int GetMercenaryAwardFactorToJoinKingdom(Clan mercenaryClan, Kingdom kingdom, bool neededAmountForClanToJoinCalculation = false);
	}
}
