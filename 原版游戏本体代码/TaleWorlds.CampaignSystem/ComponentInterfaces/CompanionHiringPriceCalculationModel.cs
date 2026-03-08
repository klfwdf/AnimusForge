using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001E6 RID: 486
	public abstract class CompanionHiringPriceCalculationModel : MBGameModel<CompanionHiringPriceCalculationModel>
	{
		// Token: 0x06001EB1 RID: 7857
		public abstract int GetCompanionHiringPrice(Hero companion);
	}
}
