using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B9 RID: 441
	public abstract class ValuationModel : MBGameModel<ValuationModel>
	{
		// Token: 0x06001D70 RID: 7536
		public abstract float GetValueOfTroop(CharacterObject troop);

		// Token: 0x06001D71 RID: 7537
		public abstract float GetMilitaryValueOfParty(MobileParty party);

		// Token: 0x06001D72 RID: 7538
		public abstract float GetValueOfHero(Hero hero);
	}
}
