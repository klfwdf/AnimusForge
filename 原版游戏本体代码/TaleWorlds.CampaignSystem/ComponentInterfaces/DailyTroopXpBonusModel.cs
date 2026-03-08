using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D8 RID: 472
	public abstract class DailyTroopXpBonusModel : MBGameModel<DailyTroopXpBonusModel>
	{
		// Token: 0x06001E44 RID: 7748
		public abstract int CalculateDailyTroopXpBonus(Town town);

		// Token: 0x06001E45 RID: 7749
		public abstract float CalculateGarrisonXpBonusMultiplier(Town town);
	}
}
