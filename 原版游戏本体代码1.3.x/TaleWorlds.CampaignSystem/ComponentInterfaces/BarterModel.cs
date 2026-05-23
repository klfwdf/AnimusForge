using System;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000196 RID: 406
	public abstract class BarterModel : MBGameModel<BarterModel>
	{
		// Token: 0x17000708 RID: 1800
		// (get) Token: 0x06001C29 RID: 7209
		public abstract int BarterCooldownWithHeroInDays { get; }

		// Token: 0x17000709 RID: 1801
		// (get) Token: 0x06001C2A RID: 7210
		public abstract float MaximumPercentageOfNpcGoldToSpendAtBarter { get; }

		// Token: 0x06001C2B RID: 7211
		public abstract int CalculateOverpayRelationIncreaseCosts(Hero hero, float overpayAmount);

		// Token: 0x06001C2C RID: 7212
		public abstract ExplainedNumber GetBarterPenalty(IFaction faction, ItemBarterable itemBarterable, Hero otherHero, PartyBase otherParty);
	}
}
