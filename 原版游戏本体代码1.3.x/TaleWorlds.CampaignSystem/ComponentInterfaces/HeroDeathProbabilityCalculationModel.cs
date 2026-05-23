using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D4 RID: 468
	public abstract class HeroDeathProbabilityCalculationModel : MBGameModel<HeroDeathProbabilityCalculationModel>
	{
		// Token: 0x06001E35 RID: 7733
		public abstract float CalculateHeroDeathProbability(Hero hero);
	}
}
