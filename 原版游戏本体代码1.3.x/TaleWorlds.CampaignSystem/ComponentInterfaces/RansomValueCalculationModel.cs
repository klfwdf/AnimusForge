using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B4 RID: 436
	public abstract class RansomValueCalculationModel : MBGameModel<RansomValueCalculationModel>
	{
		// Token: 0x06001D37 RID: 7479
		public abstract int PrisonerRansomValue(CharacterObject prisoner, Hero sellerHero = null);
	}
}
