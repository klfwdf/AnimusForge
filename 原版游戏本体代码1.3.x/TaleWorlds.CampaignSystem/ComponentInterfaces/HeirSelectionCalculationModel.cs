using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001D3 RID: 467
	public abstract class HeirSelectionCalculationModel : MBGameModel<HeirSelectionCalculationModel>
	{
		// Token: 0x17000789 RID: 1929
		// (get) Token: 0x06001E32 RID: 7730
		public abstract int HighestSkillPoint { get; }

		// Token: 0x06001E33 RID: 7731
		public abstract int CalculateHeirSelectionPoint(Hero candidateHeir, Hero deadHero, ref Hero maxSkillHero);
	}
}
